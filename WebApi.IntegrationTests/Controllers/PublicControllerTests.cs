using System.Net;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Controllers;

public class PublicControllerTests
     : IntegrationTestBase
{

    public PublicControllerTests(
       CustomWebApplicationFactory factory)
       : base(factory)
    {
    }

    [Fact]
    public async Task GetAnimals_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/public/animals");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
