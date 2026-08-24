using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace VpnPlatform.Infrastructure.Persistence;

public sealed record PostgresMigrationResult(
    string BackupPath,
    IReadOnlyList<string> AppliedMigrations);

public sealed class PostgresMigrationRunner
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(10);
    private readonly ApplicationDbContext _db;

    public PostgresMigrationRunner(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PostgresMigrationResult> RunAsync(string backupDirectory, CancellationToken cancellationToken)
    {
        if (!_db.Database.IsNpgsql())
        {
            throw new InvalidOperationException("The database-migrate command supports PostgreSQL only.");
        }

        if (string.IsNullOrWhiteSpace(backupDirectory) || !Path.IsPathRooted(backupDirectory))
        {
            throw new InvalidOperationException("DatabaseMaintenance:BackupDirectory must be an absolute path.");
        }

        var pendingMigrations = (await _db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        if (pendingMigrations.Length == 0)
        {
            return new PostgresMigrationResult(string.Empty, pendingMigrations);
        }

        var connectionString = _db.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
        }

        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        ValidateConnection(connection);

        Directory.CreateDirectory(backupDirectory);
        SetUnixMode(backupDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
        var backupPath = Path.Combine(backupDirectory, $"vpnplatform-{stamp}.dump");
        var listPath = $"{backupPath}.list";

        try
        {
            await RunProcessAsync(BuildPgDumpStartInfo(connection, backupPath), "pg_dump", cancellationToken);
            if (!File.Exists(backupPath) || new FileInfo(backupPath).Length == 0)
            {
                throw new InvalidOperationException("pg_dump completed without creating a non-empty backup.");
            }

            SetUnixMode(backupPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            var restoreList = await RunProcessAsync(BuildPgRestoreStartInfo(backupPath), "pg_restore --list", cancellationToken);
            if (string.IsNullOrWhiteSpace(restoreList))
            {
                throw new InvalidOperationException("pg_restore returned an empty backup manifest.");
            }

            await File.WriteAllTextAsync(listPath, restoreList, cancellationToken);
            SetUnixMode(listPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch
        {
            TryDelete(backupPath);
            TryDelete(listPath);
            throw;
        }

        await _db.Database.MigrateAsync(cancellationToken);
        DeleteExpiredBackups(backupDirectory, DateTimeOffset.UtcNow.AddDays(-14));

        return new PostgresMigrationResult(backupPath, pendingMigrations);
    }

    internal static ProcessStartInfo BuildPgDumpStartInfo(NpgsqlConnectionStringBuilder connection, string backupPath)
    {
        var startInfo = CreateProcessStartInfo("pg_dump");
        startInfo.ArgumentList.Add($"--host={connection.Host}");
        startInfo.ArgumentList.Add($"--port={connection.Port}");
        startInfo.ArgumentList.Add($"--username={connection.Username}");
        startInfo.ArgumentList.Add($"--dbname={connection.Database}");
        startInfo.ArgumentList.Add("--no-password");
        startInfo.ArgumentList.Add("--format=custom");
        startInfo.ArgumentList.Add("--no-owner");
        startInfo.ArgumentList.Add("--no-privileges");
        startInfo.ArgumentList.Add($"--file={backupPath}");
        startInfo.Environment["PGPASSWORD"] = connection.Password;
        startInfo.Environment["PGCONNECT_TIMEOUT"] = "15";
        startInfo.Environment["PGSSLMODE"] = ToPgSslMode(connection.SslMode);
        return startInfo;
    }

    internal static ProcessStartInfo BuildPgRestoreStartInfo(string backupPath)
    {
        var startInfo = CreateProcessStartInfo("pg_restore");
        startInfo.ArgumentList.Add("--list");
        startInfo.ArgumentList.Add(backupPath);
        return startInfo;
    }

    private static ProcessStartInfo CreateProcessStartInfo(string fileName) => new()
    {
        FileName = fileName,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    private static async Task<string> RunProcessAsync(ProcessStartInfo startInfo, string operation, CancellationToken cancellationToken)
    {
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to start {operation}.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeoutCts = new CancellationTokenSource(ProcessTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            if (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"{operation} exceeded the {ProcessTimeout.TotalMinutes:0}-minute timeout.");
            }

            throw;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{operation} failed with exit code {process.ExitCode}: {stderr.Trim()}");
        }

        return stdout;
    }

    private static void ValidateConnection(NpgsqlConnectionStringBuilder connection)
    {
        if (string.IsNullOrWhiteSpace(connection.Host)
            || string.IsNullOrWhiteSpace(connection.Database)
            || string.IsNullOrWhiteSpace(connection.Username))
        {
            throw new InvalidOperationException("PostgreSQL host, database and username are required for backup.");
        }
    }

    private static string ToPgSslMode(SslMode sslMode) => sslMode switch
    {
        SslMode.Disable => "disable",
        SslMode.Allow => "allow",
        SslMode.Prefer => "prefer",
        SslMode.Require => "require",
        SslMode.VerifyCA => "verify-ca",
        SslMode.VerifyFull => "verify-full",
        _ => throw new InvalidOperationException($"Unsupported PostgreSQL SSL mode: {sslMode}.")
    };

    private static void DeleteExpiredBackups(string backupDirectory, DateTimeOffset cutoff)
    {
        foreach (var path in Directory.EnumerateFiles(backupDirectory, "vpnplatform-*.dump*"))
        {
            if (File.GetLastWriteTimeUtc(path) < cutoff.UtcDateTime)
            {
                TryDelete(path);
            }
        }
    }

    private static void SetUnixMode(string path, UnixFileMode mode)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, mode);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
    }
}
