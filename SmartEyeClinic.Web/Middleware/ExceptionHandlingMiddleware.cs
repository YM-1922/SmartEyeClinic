using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SmartEyeClinic.Web.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred during request execution on path: {Path}", context.Request.Path);
                
                try
                {
                    var logFilePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "error_log.txt");
                    var logText = $"[{DateTime.Now}] Path: {context.Request.Path}\nException: {ex.Message}\nStack Trace:\n{ex.StackTrace}\n\n";
                    System.IO.File.AppendAllText(logFilePath, logText);
                }
                catch { }

                // Redirect to user-friendly error page
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}
