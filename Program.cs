using APITest.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace APITest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            const string loginRateLimitPolicy = "LoginPolicy";
            const string taskReadRateLimitPolicy = "TasksReadPolicy";
            const string taskWriteRateLimitPolicy = "TasksWritePolicy";
            const string adminRateLimitPolicy = "AdminPolicy";

            var builder = WebApplication.CreateBuilder(args);

            var jwtKey = builder.Configuration["Security:JwtKey"]
                ?? throw new InvalidOperationException("Missing Security:JwtKey configuration.");

            builder.Services.AddScoped<JwtService>();

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        RoleClaimType = ClaimTypes.Role,
                        NameClaimType = ClaimTypes.Name
                    };
                });

            builder.Services.AddAuthorization();

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, token) =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(
                        "Rate limit exceeded for {Method} {Path} by {Identity}.",
                        context.HttpContext.Request.Method,
                        context.HttpContext.Request.Path,
                        GetRateLimitPartitionKey(context.HttpContext));

                    context.HttpContext.Response.ContentType = "application/json";

                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new
                        {
                            message = "Rate limit exceeded for this endpoint.",
                            path = context.HttpContext.Request.Path.Value,
                            statusCode = StatusCodes.Status429TooManyRequests
                        },
                        cancellationToken: token);
                };

                options.AddPolicy(loginRateLimitPolicy, context =>
                    CreateLimiter(context, 5, TimeSpan.FromMinutes(1), "login"));

                options.AddPolicy(taskReadRateLimitPolicy, context =>
                    CreateLimiter(context, 20, TimeSpan.FromMinutes(1), "tasks-read"));

                options.AddPolicy(taskWriteRateLimitPolicy, context =>
                    CreateLimiter(context, 10, TimeSpan.FromMinutes(1), "tasks-write"));

                options.AddPolicy(adminRateLimitPolicy, context =>
                    CreateLimiter(context, 3, TimeSpan.FromMinutes(1), "admin"));
            });

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }

        private static RateLimitPartition<string> CreateLimiter(
            HttpContext context,
            int permitLimit,
            TimeSpan window,
            string policyName)
        {
            var partitionKey = $"{policyName}:{GetRateLimitPartitionKey(context)}";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        }

        private static string GetRateLimitPartitionKey(HttpContext context)
        {
            var userName = context.User.Identity?.IsAuthenticated == true
                ? context.User.Identity?.Name
                : null;

            if (!string.IsNullOrWhiteSpace(userName))
            {
                return userName;
            }

            var apiKey = context.Request.Headers["X-API-KEY"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                return apiKey;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        }
    }
}
