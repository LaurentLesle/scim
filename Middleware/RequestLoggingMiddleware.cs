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
            
            // Log incoming request with basic info
            _logger.LogInformation("🌐 [{RequestId}] {Method} {Path} from {RemoteIP} - User-Agent: {UserAgent}",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                context.Request.Headers.UserAgent.ToString());

            // Log query parameters if present
            if (context.Request.QueryString.HasValue)
            {
                var queryParams = new List<string>();
                foreach (var param in context.Request.Query)
                {
                    queryParams.Add($"{param.Key}={param.Value}");
                }
                _logger.LogInformation("📋 [{RequestId}] Query Parameters: {QueryParams}", 
                    requestId, string.Join("&", queryParams));
            }

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

            // DEBUG: Log request payload for POST/PUT/PATCH operations
            string? requestBody = null;
            if (context.Request.Method is "POST" or "PUT" or "PATCH")
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (!string.IsNullOrEmpty(requestBody))
                {
                    _logger.LogDebug("📝 [{RequestId}] Request Payload: {RequestBody}", requestId, requestBody);
                }
            }

            // Capture response body for TRACE logging
            var originalResponseBody = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

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

                // TRACE: Log response body
                string? responseBody = null;
                if (responseBodyStream.Length > 0)
                {
                    responseBodyStream.Position = 0;
                    using var reader = new StreamReader(responseBodyStream);
                    responseBody = await reader.ReadToEndAsync();
                    
                    if (!string.IsNullOrEmpty(responseBody))
                    {
                        _logger.LogTrace("📤 [{RequestId}] Response Body: {ResponseBody}", requestId, responseBody);
                    }
                    
                    // Copy response body back to original stream
                    responseBodyStream.Position = 0;
                    await responseBodyStream.CopyToAsync(originalResponseBody);
                }

                var logLevel = statusCode >= 500 ? LogLevel.Error : statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
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
                    _ => statusCode >= 500 ? "💥" : statusCode >= 400 ? "⚠️" : "✅"
                };

                // Enhanced response logging with query string for troubleshooting
                var pathWithQuery = context.Request.Path + context.Request.QueryString;
                
                // Add red color for internal server errors (500+)
                if (statusCode >= 500)
                {
                    _logger.Log(logLevel,
                        "\u001b[31m{Emoji} [{RequestId}] INTERNAL SERVER ERROR: {Method} {PathWithQuery} responded {StatusCode} in {ElapsedMs}ms\u001b[0m",
                        emoji, requestId, context.Request.Method, pathWithQuery, statusCode, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.Log(logLevel,
                        "{Emoji} [{RequestId}] {Method} {PathWithQuery} responded {StatusCode} in {ElapsedMs}ms",
                        emoji, requestId, context.Request.Method, pathWithQuery, statusCode, stopwatch.ElapsedMilliseconds);
                }

                // Log additional troubleshooting info for error responses
                if (statusCode >= 500)
                {
                    var contentType = context.Response.ContentType ?? "unknown";
                    _logger.LogError("\u001b[31m🚨 [{RequestId}] INTERNAL SERVER ERROR Details - Status: {StatusCode}, ContentType: {ContentType}, Path: {Path}\u001b[0m",
                        requestId, statusCode, contentType, pathWithQuery);
                }
                else if (statusCode >= 400)
                {
                    var contentType = context.Response.ContentType ?? "unknown";
                    _logger.LogWarning("🔍 [{RequestId}] Error Details - Status: {StatusCode}, ContentType: {ContentType}, Path: {Path}",
                        requestId, statusCode, contentType, pathWithQuery);
                }
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
