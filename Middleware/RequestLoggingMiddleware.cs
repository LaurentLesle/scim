using System.Diagnostics;

namespace ScimServiceProvider.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N")[..8];
            
            // Log incoming request
            _logger.LogInformation("🌐 [{RequestId}] {Method} {Path} from {RemoteIP} - User-Agent: {UserAgent}",
                requestId,
                context.Request.Method,
                context.Request.Path + context.Request.QueryString,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                context.Request.Headers.UserAgent.ToString());

            // Log authentication header if present
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = context.Request.Headers.Authorization.ToString();
                var authType = authHeader.StartsWith("Bearer ") ? "Bearer" : authHeader.Split(' ').FirstOrDefault() ?? "Unknown";
                _logger.LogInformation("🔑 [{RequestId}] Authentication header present: {AuthType}", requestId, authType);
            }
            else
            {
                _logger.LogWarning("⚠️ [{RequestId}] No Authorization header found", requestId);
            }

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 [{RequestId}] Unhandled exception occurred", requestId);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                var statusCode = context.Response.StatusCode;
                var logLevel = statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
                var emoji = statusCode switch
                {
                    200 => "✅",
                    201 => "✅",
                    204 => "✅",
                    400 => "❌",
                    401 => "🔒",
                    403 => "🚫",
                    404 => "🔍❌",
                    500 => "💥",
                    _ => statusCode >= 400 ? "⚠️" : "✅"
                };

                _logger.Log(logLevel,
                    "{Emoji} [{RequestId}] {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                    emoji, requestId, context.Request.Method, context.Request.Path, statusCode, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
