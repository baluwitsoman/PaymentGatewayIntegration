using System.Diagnostics;
using System.Text;

namespace PaymentGateway.Web.Infrastructure.Logging;

/// Logs every request and response for public-facing endpoints (paths that
/// start with /api or /webhooks or /pay). Captures body, key headers, status
/// code, duration. Redacts X-Api-Key. Razor admin pages are excluded to keep
/// the log readable.
public class HttpLoggingMiddleware
{
    private static readonly HashSet<string> LoggedPathPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/", "/webhooks/", "/pay/", "/pay"
    };

    // Don't log the body for anything bigger than this — protects log volume.
    private const int MaxBodyBytes = 32 * 1024;

    private readonly RequestDelegate _next;
    private readonly ILogger<HttpLoggingMiddleware> _logger;

    public HttpLoggingMiddleware(RequestDelegate next, ILogger<HttpLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "";
        var shouldLog = LoggedPathPrefixes.Any(p =>
            path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!shouldLog)
        {
            await _next(ctx);
            return;
        }

        var correlationId = Guid.NewGuid().ToString("n")[..12];
        ctx.Response.Headers["X-Correlation-Id"] = correlationId;
        var sw = Stopwatch.StartNew();

        // ---------- request ----------
        var reqBody = await ReadRequestBodyAsync(ctx.Request);
        _logger.LogInformation(
            "→ {Method} {Path}{Query} | corr={Cid} | from {Ip} | api-key={KeyTag} | body={Body}",
            ctx.Request.Method,
            ctx.Request.Path.Value,
            ctx.Request.QueryString.Value,
            correlationId,
            ctx.Connection.RemoteIpAddress,
            DescribeApiKey(ctx),
            Truncate(reqBody));

        // ---------- swap response stream so we can capture body ----------
        var originalBody = ctx.Response.Body;
        await using var buffer = new MemoryStream();
        ctx.Response.Body = buffer;

        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "✗ {Method} {Path} | corr={Cid} | unhandled exception",
                ctx.Request.Method, ctx.Request.Path.Value, correlationId);
            throw;
        }
        finally
        {
            sw.Stop();
            buffer.Position = 0;
            var respBody = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
            ctx.Response.Body = originalBody;

            _logger.LogInformation(
                "← {Method} {Path} | corr={Cid} | {Status} in {Elapsed}ms | body={Body}",
                ctx.Request.Method,
                ctx.Request.Path.Value,
                correlationId,
                ctx.Response.StatusCode,
                sw.ElapsedMilliseconds,
                Truncate(respBody));
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanSeek) request.EnableBuffering();
        request.Body.Position = 0;
        using var sr = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await sr.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private static string Truncate(string s)
    {
        if (string.IsNullOrEmpty(s)) return "<empty>";
        if (s.Length <= MaxBodyBytes) return s;
        return s[..MaxBodyBytes] + $"…[truncated, {s.Length} bytes total]";
    }

    private static string DescribeApiKey(HttpContext ctx)
    {
        var key = ctx.Request.Headers["X-Api-Key"].ToString();
        if (string.IsNullOrEmpty(key)) return "<none>";
        var idx = key.IndexOf('_', key.IndexOf('_') + 1);
        return idx > 0 ? key[..(idx + 1)] + "…" : "[redacted]";
    }
}

public static class HttpLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UsePublicApiLogging(this IApplicationBuilder app)
        => app.UseMiddleware<HttpLoggingMiddleware>();
}
