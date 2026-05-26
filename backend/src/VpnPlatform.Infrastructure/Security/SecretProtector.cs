using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using VpnPlatform.Application.Abstractions;

namespace VpnPlatform.Infrastructure.Security;

public sealed class SecretProtector : ISecretProtector
{
    private const string Prefix = "v1";
    private readonly byte[] _key;

    public SecretProtector(IConfiguration configuration, IHostEnvironment? environment = null)
    {
        var configured = configuration["Security:SecretEncryptionKey"];
        if (string.IsNullOrWhiteSpace(configured) && environment?.IsDevelopment() == true)
        {
            // Backward-compatible local/dev fallback only. Production/staging must set an explicit encryption key.
            configured = configuration["Jwt:SigningKey"];
        }

        if (string.IsNullOrWhiteSpace(configured) || configured.Length < 32)
        {
            throw new InvalidOperationException("Security:SecretEncryptionKey with at least 32 characters is required to protect secrets. JWT signing key fallback is allowed only in Development.");
        }

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
    }

    public string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return string.Empty;
        }

        var iv = RandomNumberGenerator.GetBytes(16);
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var macInput = new byte[iv.Length + cipher.Length];
        Buffer.BlockCopy(iv, 0, macInput, 0, iv.Length);
        Buffer.BlockCopy(cipher, 0, macInput, iv.Length, cipher.Length);
        using var hmac = new HMACSHA256(_key);
        var mac = hmac.ComputeHash(macInput);

        var payload = new byte[macInput.Length + mac.Length];
        Buffer.BlockCopy(macInput, 0, payload, 0, macInput.Length);
        Buffer.BlockCopy(mac, 0, payload, macInput.Length, mac.Length);

        return $"{Prefix}:{Convert.ToBase64String(payload)}";
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return string.Empty;
        }

        if (!protectedValue.StartsWith($"{Prefix}:", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Secret value has unsupported protection format.");
        }

        var payload = Convert.FromBase64String(protectedValue[(Prefix.Length + 1)..]);
        if (payload.Length < 16 + 32 + 1)
        {
            throw new InvalidOperationException("Secret value is malformed.");
        }

        var iv = payload[..16];
        var mac = payload[^32..];
        var cipher = payload[16..^32];

        var macInput = payload[..^32];
        using var hmac = new HMACSHA256(_key);
        var expectedMac = hmac.ComputeHash(macInput);
        if (!CryptographicOperations.FixedTimeEquals(mac, expectedMac))
        {
            throw new InvalidOperationException("Secret value MAC validation failed.");
        }

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public string Mask(string? value, int visibleTail = 4)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Length <= visibleTail)
        {
            return new string('*', value.Length);
        }

        return new string('*', Math.Max(4, value.Length - visibleTail)) + value[^visibleTail..];
    }
}
