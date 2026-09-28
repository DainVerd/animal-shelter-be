using System.Net;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Controllers;

public class PublicControllerTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PublicControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAnimals_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/public/animals");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
