using Application.Interfaces.Services;
using Domain.Models;
using Infrastructure.Persistance.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;
using WebApi.IntegrationTests.Fakes;

namespace WebApi.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=petspets_test;Username=petspets;Password=petspets_dev_password";

    private Respawner? _respawner;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        TestConnectionString,

                    ["Jwt:Key"] =
                        "IntegrationTestJwtKey_12345678901234567890",

                    ["Jwt:Issuer"] =
                        "IntegrationTests",

                    ["Jwt:Audience"] =
                        "IntegrationTests",

                    ["InitialUserCredentials:UserName"] =
                        "integration-admin",

                    ["InitialUserCredentials:Password"] =
                        "TestAdmin123!",

                    ["InitialUserCredentials:Email"] =
                        "integration-admin@example.com",

                    ["SendGridSettings:ApiKey"] =
                        "integration-test-api-key",

                    ["S3Settings:AccessKey"] =
                        "integration-test-access-key",

                    ["S3Settings:SecretKey"] =
                        "integration-test-secret-key",

                    ["S3Settings:Region"] =
                        "eu-north-1"
                });
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFileStorageService>();

            services.AddSingleton<
                IFileStorageService,
                FakeFileStorageService>();
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection =
            new NpgsqlConnection(TestConnectionString);

        await connection.OpenAsync();

        _respawner ??=
            await Respawner.CreateAsync(
                connection,
                new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Postgres,

                    SchemasToInclude =
                    [
                        "public"
                    ],

                    TablesToIgnore =
                    [
                        "__EFMigrationsHistory"
                    ]
                });

        await _respawner.ResetAsync(connection);

        await SeedDatabaseAsync();
    }

    private async Task SeedDatabaseAsync()
    {
        using var scope = Services.CreateScope();

        var initializer =
            scope.ServiceProvider
                .GetRequiredService<DatabaseInitializer>();

        await initializer.InitializeAsync();
    }

    public async Task CreateUserAsync(
        string email,
        string password,
        string role)
    {
        using var scope = Services.CreateScope();

        var userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new User
            {
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                RequirePasswordChange = false
            };

            var createResult =
                await userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    createResult.Errors.Select(x => x.Description));

                throw new InvalidOperationException(
                    $"Failed to create integration test user: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult =
                await userManager.AddToRoleAsync(user, role);

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    roleResult.Errors.Select(x => x.Description));

                throw new InvalidOperationException(
                    $"Failed to add integration test role: {errors}");
            }
        }
    }
}