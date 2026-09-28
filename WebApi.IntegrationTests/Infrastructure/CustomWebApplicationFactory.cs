using Application.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using WebApi.IntegrationTests.Fakes;

namespace WebApi.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5432;Database=petspets_test;Username=petspets;Password=petspets_dev_password",

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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFileStorageService>();

            services.AddSingleton<IFileStorageService, FakeFileStorageService>();
        });
    }
}