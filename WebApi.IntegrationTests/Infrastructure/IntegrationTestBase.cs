using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApi.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase
    : IClassFixture<CustomWebApplicationFactory>,
      IAsyncLifetime
{
    protected CustomWebApplicationFactory Factory { get; }

    protected HttpClient Client { get; }

    protected IntegrationTestBase(
        CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(
     new WebApplicationFactoryClientOptions
     {
         BaseAddress = new Uri("https://localhost"),
         HandleCookies = true
     });
    }

    public async ValueTask InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync()
    {
        Client.Dispose();

        return ValueTask.CompletedTask;
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        Client.Dispose();

        return Task.CompletedTask;
    }
}