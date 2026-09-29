using Application.Constants;
using Application.Dtos.Animal;
using Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using WebApi.IntegrationTests.Extensions;
using WebApi.IntegrationTests.Factories;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Controllers;

public class AnimalControllerTests
    : IntegrationTestBase
{
    public AnimalControllerTests(
        CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetAllAnimals_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync(
            "/api/v1/animals");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAllAnimals_WithUserRole_ReturnsOk()
    {
        // Arrange
        const string email =
            "integration-user@example.com";

        const string password =
            "TestUser123!";

        await Factory.CreateUserAsync(
            email,
            password,
            UserRole.User);

        await Client.AuthenticateAsync(
            UserRole.User,
            email,
            password);

        // Act
        var response = await Client.GetAsync(
            "/api/v1/animals");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateAnimal_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var content =
            new MultipartFormDataContent();

        // Act
        var response = await Client.PostAsync(
            "/api/v1/animals",
            content);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateAnimal_WithUserRole_ReturnsForbidden()
    {
        // Arrange
        const string email =
            "integration-user@example.com";

        const string password =
            "TestUser123!";

        await Factory.CreateUserAsync(
            email,
            password,
            UserRole.User);

        await Client.AuthenticateAsync(
            UserRole.User,
            email,
            password);

        using var content =
            new MultipartFormDataContent();

        // Act
        var response = await Client.PostAsync(
            "/api/v1/animals",
            content);

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateAnimal_WithSuperAdminRole_ReturnsCreated()
    {
        // Arrange
        await Client.AuthenticateAsync(
            UserRole.SuperAdmin);

        using var content =
            AnimalRequestFactory
                .CreateValidCreateRequest();

        // Act
        var response = await Client.PostAsync(
            "/api/v1/animals",
            content);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var animalId =
            await response.Content
                .ReadFromJsonAsync<int>();

        Assert.True(animalId > 0);
    }

    [Fact]
    public async Task CreateAnimal_ThenGetAnimal_ReturnsCreatedAnimal()
    {
        // Arrange
        await Client.AuthenticateAsync(
            UserRole.SuperAdmin);

        const string name = "Integration Dog";
        const string breed = "Labrador";
        const string description =
            "Animal created by integration test";

        using var createContent =
            AnimalRequestFactory
                .CreateValidCreateRequest(
                    name,
                    breed,
                    description);

        var createResponse =
            await Client.PostAsync(
                "/api/v1/animals",
                createContent);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var animalId =
            await createResponse.Content
                .ReadFromJsonAsync<int>();

        Assert.True(animalId > 0);

        // Act
        var getResponse =
            await Client.GetAsync(
                $"/api/v1/animals/{animalId}");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var animal =
            await getResponse.Content
                .ReadFromJsonAsync<AnimalDto>();

        Assert.NotNull(animal);

        Assert.Equal(animalId, animal.Id);
        Assert.Equal(name, animal.Name);
        Assert.Equal(breed, animal.Breed);
        Assert.Equal(description, animal.Description);

        Assert.Equal(
            Gender.Male,
            animal.Gender);

        Assert.Equal(
            AnimalSize.Medium,
            animal.Size);

        Assert.Equal(
            Temperament.Friendly,
            animal.Temperament);

        Assert.False(animal.IsSterilized);
        Assert.True(animal.IsVaccinated);

        Assert.NotEmpty(animal.Images);
    }

    [Fact]
    public async Task CreateAnimal_ThenUpdateAnimal_ReturnsUpdatedAnimal()
    {
        // Arrange
        await Client.AuthenticateAsync(
            UserRole.SuperAdmin);

        using var createContent =
            AnimalRequestFactory
                .CreateValidCreateRequest();

        var createResponse =
            await Client.PostAsync(
                "/api/v1/animals",
                createContent);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var animalId =
            await createResponse.Content
                .ReadFromJsonAsync<int>();

        // Get existing photos
        var initialResponse =
            await Client.GetAsync(
                $"/api/v1/animals/{animalId}");

        var initialAnimal =
            await initialResponse.Content
                .ReadFromJsonAsync<AnimalDto>();

        Assert.NotNull(initialAnimal);
        Assert.NotEmpty(initialAnimal.Images);

        var existingPhotoIds =
            initialAnimal.Images
                .Select(x => x.Id)
                .ToList();

        using var updateContent =
            AnimalRequestFactory
                .CreateValidUpdateRequest(
                    animalId,
                    existingPhotoIds);

        // Act
        var updateResponse =
            await Client.PutAsync(
                "/api/v1/animals",
                updateContent);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var getResponse =
            await Client.GetAsync(
                $"/api/v1/animals/{animalId}");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var updatedAnimal =
            await getResponse.Content
                .ReadFromJsonAsync<AnimalDto>();

        Assert.NotNull(updatedAnimal);

        Assert.Equal(
            "Updated Integration Dog",
            updatedAnimal.Name);

        Assert.Equal(
            "Golden Retriever",
            updatedAnimal.Breed);

        Assert.Equal(
            "Updated by integration test",
            updatedAnimal.Description);

        Assert.Equal(
            Gender.Female,
            updatedAnimal.Gender);

        Assert.Equal(
            AnimalSize.Large,
            updatedAnimal.Size);

        Assert.True(updatedAnimal.IsSterilized);
        Assert.True(updatedAnimal.IsVaccinated);

        Assert.NotEmpty(updatedAnimal.Images);
    }

    [Fact]
    public async Task UpdateAnimal_WithoutExistingPhotoIds_RemovesOldImages()
    {
        // Arrange
        await Client.AuthenticateAsync(
            UserRole.SuperAdmin);

        using var createContent =
            AnimalRequestFactory
                .CreateValidCreateRequest();

        var createResponse =
            await Client.PostAsync(
                "/api/v1/animals",
                createContent);

        var animalId =
            await createResponse.Content
                .ReadFromJsonAsync<int>();

        var initialResponse =
            await Client.GetAsync(
                $"/api/v1/animals/{animalId}");

        var initialAnimal =
            await initialResponse.Content
                .ReadFromJsonAsync<AnimalDto>();

        Assert.NotNull(initialAnimal);
        Assert.NotEmpty(initialAnimal.Images);

        using var updateContent =
            AnimalRequestFactory
                .CreateValidUpdateRequest(
                    animalId,
                    existingPhotoIds: null);

        // Act
        var updateResponse =
            await Client.PutAsync(
                "/api/v1/animals",
                updateContent);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var getResponse =
            await Client.GetAsync(
                $"/api/v1/animals/{animalId}");

        var updatedAnimal =
            await getResponse.Content
                .ReadFromJsonAsync<AnimalDto>();

        Assert.NotNull(updatedAnimal);
        Assert.Empty(updatedAnimal.Images);
    }

    [Fact]
    public async Task UpdateAnimal_WithNewPhoto_ReplacesOldPhoto()
    {
        // Arrange
        await Client.AuthenticateAsync(
            UserRole.SuperAdmin);

        using var createContent =
            AnimalRequestFactory
                .CreateValidCreateRequest();

        var createResponse =
            await Client.PostAsync(
                "/api/v1/animals",
                createContent);

        var animalId =
            await createResponse.Content
                .ReadFromJsonAsync<int>();

        var initialResponse =
            await Client.GetAsync(
                $"/api/v1/animals/{animalId}");

        var initialAnimal =
            await initialResponse.Content
                .ReadFromJsonAsync<AnimalDto>();

        Assert.NotNull(initialAnimal);

        var oldImage =
            Assert.Single(initialAnimal.Images);

        using var updateContent =
            AnimalRequestFactory
                .CreateValidUpdateRequest(
                    animalId,
                    existingPhotoIds: null,
                    addNewPhoto: true);

        // Act
        var updateResponse =
            await Client.PutAsync(
                "/api/v1/animals",
                updateContent);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var getResponse =
            await Client.GetAsync(
                $"/api/v1/animals/{animalId}");

        var updatedAnimal =
            await getResponse.Content
                .ReadFromJsonAsync<AnimalDto>();

        Assert.NotNull(updatedAnimal);

        var newImage =
            Assert.Single(updatedAnimal.Images);

        Assert.NotEqual(
            oldImage.Id,
            newImage.Id);
    }

    [Fact]
    public async Task CreateAnimal_ThenDeleteAnimal_GetReturnsNotFound()
    {
        // Arrange
        await Client.AuthenticateAsync(
            UserRole.SuperAdmin);

        using var createContent =
            AnimalRequestFactory
                .CreateValidCreateRequest();

        var createResponse =
            await Client.PostAsync(
                "/api/v1/animals",
                createContent);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var animalId =
            await createResponse.Content
                .ReadFromJsonAsync<int>();

        // Act
        var deleteResponse =
            await Client.DeleteAsync(
                $"/api/v1/animals/{animalId}");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            deleteResponse.StatusCode);

        var getResponse =
            await Client.GetAsync(
                $"/api/v1/animals/{animalId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }
    [Fact]
    public async Task DeleteAnimal_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await Client.DeleteAsync(
                "/api/v1/animals/999");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAnimal_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        await Client.AuthenticateAsync(UserRole.SuperAdmin);

        // Act
        var response =
            await Client.GetAsync("/api/v1/animals/999999");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
    [Fact]
    public async Task GetAnimal_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange
        await Client.AuthenticateAsync(UserRole.SuperAdmin);

        // Act
        var response =
            await Client.GetAsync("/api/v1/animals/0");

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
    
}