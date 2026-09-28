using System.Net;
using System.Net.Http.Json;
using WebApi.IntegrationTests.Extensions;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Controllers;

public class AnimalControllerTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AnimalControllerTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllAnimals_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/animals");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllAnimals_WithAuthentication_ReturnsOk()
    {
        // Arrange
        await _client.AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/animals");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateAnimal_WithSuperAdminRole_ReturnsCreated()
    {
        // Arrange
        await _client.AuthenticateAsync();

        using var content = new MultipartFormDataContent();

        content.Add(new StringContent("Integration Dog"), "Name");
        content.Add(new StringContent("Labrador"), "Breed");
        content.Add(
            new StringContent("Animal created by integration test"),
            "Description");

        content.Add(new StringContent("Male"), "Gender");
        content.Add(new StringContent("Medium"), "Size");
        content.Add(new StringContent("Friendly"), "Temperament");

        content.Add(new StringContent("false"), "IsSterilized");
        content.Add(new StringContent("true"), "IsVaccinated");

        var photoBytes = new byte[]
        {
        1, 2, 3, 4, 5
        };

        var photoContent = new ByteArrayContent(photoBytes);

        photoContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
                "image/jpeg");

        content.Add(
            photoContent,
            "NewPhotos",
            "animal.jpg");

        // Act
        var response = await _client.PostAsync(
            "/api/v1/animals",
            content);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var animalId =
            await response.Content.ReadFromJsonAsync<int>();

        Assert.True(animalId > 0);
    }

    [Fact]
    public async Task CreateAnimal_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var content = new MultipartFormDataContent();

        // Act
        var response = await _client.PostAsync(
            "/api/v1/animals",
            content);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}