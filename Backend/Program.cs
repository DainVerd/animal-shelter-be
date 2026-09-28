namespace WebApi;

using Application;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentValidation;
using Infrastructure;
using Infrastructure.Persistance.Data;
using Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Reflection;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using WebApi.Behaviors;
using WebApi.Configuration;
using WebApi.Middleware;

public class Program
{
    public static async Task Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        var loggerConfiguration = new LoggerConfiguration();
        if (builder.Environment.IsDevelopment())
        {
            loggerConfiguration = loggerConfiguration
                .MinimumLevel.Information()
                .WriteTo.Console();
        }
        else
        {
            loggerConfiguration = loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext();
        }
        var logger = loggerConfiguration.CreateLogger();
        try
        {
            Log.Information("Starting Backend App");
            // Add services to the container.
            // https://www.claudiobernasconi.ch/2022/01/28/how-to-use-serilog-in-asp-net-core-web-api/ for precise logging
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(logger);

            // project dependencies in Clean Architecture pattern
            builder.Services
                .AddApplication()
                .AddInfrastructure(builder.Configuration);

            // add swagger
            builder.Services.AddSwaggerGen();
            builder.Services.AddEndpointsApiExplorer();
            // configure swagger
            builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

            // API versioning
            builder.Services.AddApiVersioning(opt =>
            {
                opt.DefaultApiVersion = new ApiVersion(1, 0);
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.ReportApiVersions = true;
                opt.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(),
                                                                new HeaderApiVersionReader("x-api-version"),
                                                                new MediaTypeApiVersionReader("x-api-version"));
            })
                .AddApiExplorer(setup =>
                {
                    setup.GroupNameFormat = "'v'VVV";
                    setup.SubstituteApiVersionInUrl = true;
                });

            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            builder.Services.AddCors(options =>
            {
                var allowedCordsOrigins = builder.Configuration.GetSection("AllowedCorsOrigins").Get<string[]>();
                allowedCordsOrigins ??= ["http://localhost:5000"];
                options.AddPolicy("FrontendPolicy", builder => builder
                                   .WithOrigins(allowedCordsOrigins)
                                   .AllowAnyHeader()
                                   .AllowAnyMethod()
                                   .AllowCredentials());
            });

            builder.Services.AddAuthorization();
            var authOptions = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();

            if (authOptions is null)
                throw new Exception("Jwt setting was not provided!");

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = authOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = authOptions.Audience,
                        ValidateLifetime = true,
                        IssuerSigningKey = authOptions.GetSymmetricSecurityKey(),
                        ValidateIssuerSigningKey = true,
                    };
                });

            builder.Services.AddControllers();
            builder.Services.AddRateLimiter(options =>
            {
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    var errorMessage = "Too many requests. Please try again later.";
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        errorMessage = $"Too many requests. Please try again after {retryAfter.TotalMinutes} minute(s).";

                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Detail = errorMessage,
                        Type = "https://httpstatuses.com/429"
                    };

                    await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
                };
                options.AddFixedWindowLimiter("FixedPolicy", opt =>
                {
                    opt.Window = TimeSpan.FromMinutes(1);    // Time window of 1 minute
                    opt.PermitLimit = 100;                   // Allow 100 requests per minute
                    opt.QueueLimit = 2;                      // Queue limit of 2
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });
            });

            // add fluent validators
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // custom middleware
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            var app = builder.Build();


            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();

                if (app.Environment.IsEnvironment("Testing"))
                {
                    await context.Database.MigrateAsync();
                }

                var initializer = scope.ServiceProvider
                    .GetRequiredService<DatabaseInitializer>();

                await initializer.InitializeAsync();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger(u =>
                {
                    u.RouteTemplate = "swagger/{documentName}/swagger.json";
                });
                var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
                app.UseSwaggerUI(options =>
                {
                    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                    {
                        options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                            description.GroupName.ToUpperInvariant());
                    }
                    options.RoutePrefix = string.Empty;
                });
            }


            app.UseHttpsRedirection();
            app.UseHsts();
            // security headers
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Referrer-Policy", "no-referrer");
                await next();
            });
            app.UseExceptionHandler();
            app.UseMiddleware<ForcePasswordChangeMiddleware>();
            app.UseCors("FrontendPolicy");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.Information("Stopping Backend application");
            Log.CloseAndFlush();
        }
    }
}
