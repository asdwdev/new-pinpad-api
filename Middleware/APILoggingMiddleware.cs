using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.Models;
using System.Text;
using System.Text.Json;

namespace NewPinpadApi.Middleware
{
    public class APILoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<APILoggingMiddleware> _logger;

        public APILoggingMiddleware(RequestDelegate next, ILogger<APILoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Capture original response body stream
            var originalResponseBody = context.Response.Body;
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            string requestBody = string.Empty;
            string responseBody = string.Empty;
            int statusCode = 0;

            try
            {
                // Capture request body for POST/PUT requests
                if (context.Request.Method == "POST" || context.Request.Method == "PUT")
                {
                    context.Request.EnableBuffering();
                    requestBody = await ReadRequestBody(context.Request);
                    context.Request.Body.Position = 0;
                }

                // Process the request
                await _next(context);

                stopwatch.Stop();
                statusCode = context.Response.StatusCode;

                // Capture response body
                memoryStream.Position = 0;
                responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
                memoryStream.Position = 0;

                // Copy response back to original stream
                await memoryStream.CopyToAsync(originalResponseBody);

                // Log to database
                await LogToDatabase(context, requestBody, responseBody, statusCode, stopwatch.ElapsedMilliseconds, dbContext);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                statusCode = 500;
                responseBody = JsonSerializer.Serialize(new { error = ex.Message });

                // Log error to database
                await LogToDatabase(context, requestBody, responseBody, statusCode, stopwatch.ElapsedMilliseconds, dbContext);

                throw;
            }
            finally
            {
                context.Response.Body = originalResponseBody;
            }
        }

        private async Task<string> ReadRequestBody(HttpRequest request)
        {
            try
            {
                var body = await new StreamReader(request.Body).ReadToEndAsync();
                return body.Length > 2000 ? body.Substring(0, 2000) + "..." : body; // Limit request body length
            }
            catch
            {
                return "Unable to read request body";
            }
        }

        private async Task LogToDatabase(HttpContext context, string requestBody, string responseBody, int statusCode, long responseTime, AppDbContext dbContext)
        {
            try
            {
                // Skip logging for certain endpoints to avoid infinite loops
                var path = context.Request.Path.Value?.ToLower();
                if (path?.Contains("/api/apireqlog") == true || path?.Contains("/swagger") == true)
                {
                    return;
                }

                // Get user info from session
                var username = context.Session.GetString("Username") ?? "Anonymous";
                if (string.IsNullOrEmpty(username))
                {
                    username = context.User?.Identity?.Name ?? "Anonymous";
                }

                // Determine process name from endpoint
                var process = DetermineProcessName(context.Request.Path.Value, context.Request.Method);

                // Create log entry
                var logEntry = new APIReqLog
                {
                    Proses = process,
                    Request = string.IsNullOrEmpty(requestBody) ? null : requestBody,
                    Result = responseBody.Length > 2000 ? responseBody.Substring(0, 2000) + "..." : responseBody, // Limit response body length
                    StatusCode = statusCode.ToString(),
                    Remark = statusCode >= 200 && statusCode < 300 ? "Success" : "Error",
                    ReqBy = username,
                    ReqDate = DateTime.Now,
                    Method = context.Request.Method,
                    Endpoint = context.Request.Path.Value,
                    IpAddress = GetClientIpAddress(context),
                    ResponseTime = (int)responseTime
                };

                // Save to database
                dbContext.APIReqLogs.Add(logEntry);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging API request to database");
            }
        }

        private string DetermineProcessName(string? path, string method)
        {
            if (string.IsNullOrEmpty(path))
                return "Unknown";

            // Extract process name from path
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
            {
                var controllerName = segments[1].Replace("controller", "", StringComparison.OrdinalIgnoreCase);
                
                // Map common operations
                if (segments.Length > 2)
                {
                    var action = segments[2].ToLower();
                    switch (action)
                    {
                        case "login":
                            return "Auth";
                        case "logout":
                            return "Logout";
                        case "create":
                            return $"Create{controllerName}";
                        case "update":
                            return $"Update{controllerName}";
                        case "delete":
                            return $"Delete{controllerName}";
                        case "get":
                            return $"Get{controllerName}";
                        default:
                            return action.ToUpper();
                    }
                }
                
                return controllerName;
            }

            return "Unknown";
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            
            // Check for forwarded headers
            var forwardedIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedIp))
            {
                ip = forwardedIp.Split(',')[0].Trim();
            }

            return ip ?? "Unknown";
        }
    }

    // Extension method for easy registration
    public static class APILoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseAPILogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<APILoggingMiddleware>();
        }
    }
}
