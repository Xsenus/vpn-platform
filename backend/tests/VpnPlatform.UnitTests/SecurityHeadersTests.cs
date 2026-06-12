using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using VpnPlatform.Api.Middleware;
using Xunit;

namespace VpnPlatform.UnitTests;

public class SecurityHeadersTests
{
    [Fact]
    public async Task Api_Responses_Should_Include_Security_Headers()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new SecurityHeadersMiddleware(WriteOkAsync, new TestHostEnvironment(Environments.Staging));

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
        Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"].ToString());
        Assert.Equal("camera=(), microphone=(), geolocation=(), payment=()", context.Response.Headers["Permissions-Policy"].ToString());
        Assert.Equal("default-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'none'", context.Response.Headers["Content-Security-Policy"].ToString());
        Assert.False(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Production_Api_Responses_Should_Include_Hsts()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new SecurityHeadersMiddleware(WriteOkAsync, new TestHostEnvironment(Environments.Production));

        await middleware.InvokeAsync(context);

        Assert.Equal("max-age=31536000; includeSubDomains", context.Response.Headers["Strict-Transport-Security"].ToString());
    }

    [Fact]
    public async Task Development_Swagger_Should_Not_Receive_Api_Csp()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/swagger/index.html";
        context.Response.Body = new MemoryStream();
        var middleware = new SecurityHeadersMiddleware(WriteOkAsync, new TestHostEnvironment(Environments.Development));

        await middleware.InvokeAsync(context);

        Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
    }

    [Fact]
    public void Api_Program_Should_Register_Security_Headers_Before_Exception_Handler()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "Program.cs"));

        Assert.Contains("app.UseMiddleware<SecurityHeadersMiddleware>();", program, StringComparison.Ordinal);
        Assert.True(program.IndexOf("app.UseMiddleware<RequestObservabilityMiddleware>();", StringComparison.Ordinal) < program.IndexOf("app.UseMiddleware<SecurityHeadersMiddleware>();", StringComparison.Ordinal));
        Assert.True(program.IndexOf("app.UseMiddleware<SecurityHeadersMiddleware>();", StringComparison.Ordinal) < program.IndexOf("app.UseMiddleware<ExceptionHandlingMiddleware>();", StringComparison.Ordinal));
    }

    [Fact]
    public void Frontend_Dockerfiles_Should_Use_Security_Nginx_Config()
    {
        var root = FindRepositoryRoot();
        var nginx = File.ReadAllText(Path.Combine(root, "frontend", "nginx.security.conf"));

        Assert.Contains("Content-Security-Policy", nginx, StringComparison.Ordinal);
        Assert.Contains("Strict-Transport-Security", nginx, StringComparison.Ordinal);
        Assert.Contains("X-Frame-Options", nginx, StringComparison.Ordinal);
        Assert.Contains("try_files $uri $uri/ /index.html;", nginx, StringComparison.Ordinal);

        foreach (var dockerfile in new[] { "Dockerfile.public-web", "Dockerfile.cabinet", "Dockerfile.admin-panel" })
        {
            var text = File.ReadAllText(Path.Combine(root, "frontend", dockerfile));
            Assert.Contains("COPY nginx.security.conf /etc/nginx/conf.d/default.conf", text, StringComparison.Ordinal);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "README.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }

    private static Task WriteOkAsync(HttpContext context)
        => context.Response.WriteAsync("ok");

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "VpnPlatform.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
