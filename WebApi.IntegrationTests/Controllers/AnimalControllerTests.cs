using Application.Constants;
using Application.Dtos.Animal;
using Domain.Enums;
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
        await _client.AuthenticateAsync(UserRole.SuperAdmin);

        // Act
        var response = await _client.GetAsync("/api/v1/animals");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateAnimal_WithSuperAdminRole_ReturnsCreated()
    {
        // Arrange
        await _client.AuthenticateAsync(UserRole.SuperAdmin);

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

    [Fact]
    public async Task CreateAnimal_ThenGetAnimal_ReturnsCreatedAnimal()
    {
        // Arrange
        await _client.AuthenticateAsync(UserRole.SuperAdmin);

        const string name = "Integration Dog";
        const string breed = "Labrador";
        const string description = "Animal created by integration test";

        using var content = new MultipartFormDataContent();

        content.Add(new StringContent(name), "Name");
        content.Add(new StringContent(breed), "Breed");
        content.Add(new StringContent(description), "Description");

        content.Add(
            new StringContent(Gender.Male.ToString()),
            "Gender");

        content.Add(
            new StringContent(AnimalSize.Medium.ToString()),
            "Size");

        content.Add(
            new StringContent(Temperament.Friendly.ToString()),
            "Temperament");

        content.Add(
            new StringContent("2022-05-15"),
            "DateOfBirth");

        content.Add(
            new StringContent("false"),
            "IsSterilized");

        content.Add(
            new StringContent("true"),
            "IsVaccinated");

        var photoBytes = new byte[]
        {
        1, 2, 3, 4, 5
        };

        using var photoContent =
            new ByteArrayContent(photoBytes);

        photoContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
                "image/jpeg");

        content.Add(
            photoContent,
            "NewPhotos",
            "animal.jpg");

        // Act - Create
        var createResponse = await _client.PostAsync(
            "/api/v1/animals",
            content);

        // Assert - Create
        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var animalId =
            await createResponse.Content.ReadFromJsonAsync<int>();

        Assert.True(animalId > 0);

        // Act - Get
        var getResponse = await _client.GetAsync(
            $"/api/v1/animals/{animalId}");

        // Assert - Get
        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var animal =
            await getResponse.Content.ReadFromJsonAsync<AnimalDto>();

        Assert.NotNull(animal);

        Assert.Equal(animalId, animal.Id);
        Assert.Equal(name, animal.Name);
        Assert.Equal(breed, animal.Breed);
        Assert.Equal(description, animal.Description);

        Assert.Equal(Gender.Male, animal.Gender);
        Assert.Equal(AnimalSize.Medium, animal.Size);
        Assert.Equal(
            Temperament.Friendly,
            animal.Temperament);

        Assert.Equal(
            new DateOnly(2022, 5, 15),
            animal.DateOfBirth);

        Assert.False(animal.IsSterilized);
        Assert.True(animal.IsVaccinated);

        Assert.NotEmpty(animal.Images);
    }
}