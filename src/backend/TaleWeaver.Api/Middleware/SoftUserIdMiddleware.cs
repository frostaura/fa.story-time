namespace TaleWeaver.Api.Middleware;

/// <summary>
/// Extracts X-Soft-User-Id header and makes it available via HttpContext.Items.
/// Returns 400 for non-config endpoints when header is missing.
/// </summary>
public class SoftUserIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SoftUserIdMiddleware> _logger;

    /// <summary>
    /// Paths that do not require a Soft User ID header.
    /// </summary>
    private static readonly string[] ExemptPaths =
    [
        "/api/config",
        "/api/webhooks",
        "/swagger",
        "/health"
    ];

    public SoftUserIdMiddleware(RequestDelegate next, ILogger<SoftUserIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip exempt paths
        if (ExemptPaths.Any(exempt => path.StartsWith(exempt, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        if (context.Request.Headers.TryGetValue("X-Soft-User-Id", out var softUserId)
            && !string.IsNullOrWhiteSpace(softUserId))
        {
            context.Items["SoftUserId"] = softUserId.ToString();
            await _next(context);
            return;
        }

        _logger.LogWarning("Missing X-Soft-User-Id header for {Path}", path);

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "X-Soft-User-Id header is required"
        });
    }
}
