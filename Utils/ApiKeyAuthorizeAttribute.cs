using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace APITest.Utils
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var configuration = context.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<ApiKeyAuthorizeAttribute>)) as ILogger<ApiKeyAuthorizeAttribute>;

            if (!context.HttpContext.Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey))
            {
                logger?.LogWarning(
                    "Request missing API key header for {Method} {Path}.",
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path);

                context.Result = new ContentResult
                {
                    StatusCode = 401,
                    Content = "API key was not provided."
                };
                return;
            }

            if (configuration == null || configuration["Security:ApiKey"] == null)
            {
                context.Result = new ContentResult
                {
                    StatusCode = 500,
                    Content = "Configuration Error"
                };
                return;
            }

            var apiKey = configuration["Security:ApiKey"];

            if (!string.Equals(apiKey, extractedApiKey.ToString(), StringComparison.Ordinal))
            {
                logger?.LogWarning(
                    "Invalid API key attempt for {Method} {Path}.",
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path);

                context.Result = new ContentResult
                {
                    StatusCode = 401,
                    Content = "Unauthorized client."
                };
            }
        }
    }
}
