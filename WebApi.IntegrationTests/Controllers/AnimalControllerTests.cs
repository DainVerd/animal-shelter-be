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
        var response = await Client.GetAsync("/api/v1/animals");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllAnimals_WithAuthentication_ReturnsOk()
    {
        // Arrange
        await Client.AuthenticateAsync(UserRole.SuperAdmin);

        // Act
        var response = await Client.GetAsync("/api/v1/animals");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateAnimal_WithSuperAdminRole_ReturnsCreated()
    {
        // Arrange
        await Client.AuthenticateAsync(UserRole.SuperAdmin);

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
        var response = await Client.PostAsync(
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
        var response = await Client.PostAsync(
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
        await Client.AuthenticateAsync(UserRole.SuperAdmin);

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
        var createResponse = await Client.PostAsync(
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
        var getResponse = await Client.GetAsync(
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

    [Fact]
    public async Task CreateAnimal_ThenDeleteAnimal_GetReturnsNotFound()
    {
        // Arrange
        await Client.AuthenticateAsync(UserRole.SuperAdmin);

        using var content = new MultipartFormDataContent();

        content.Add(new StringContent("Delete Test Dog"), "Name");
        content.Add(new StringContent("Labrador"), "Breed");
        content.Add(new StringContent("Animal for delete integration test"), "Description");
        content.Add(new StringContent(Gender.Male.ToString()), "Gender");
        content.Add(new StringContent(AnimalSize.Medium.ToString()), "Size");
        content.Add(new StringContent(Temperament.Friendly.ToString()), "Temperament");
        content.Add(new StringContent("2022-05-15"), "DateOfBirth");
        content.Add(new StringContent("false"), "IsSterilized");
        content.Add(new StringContent("true"), "IsVaccinated");

        using var photoContent =
            new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });

        photoContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

        content.Add(
            photoContent,
            "NewPhotos",
            "animal.jpg");

        var createResponse = await Client.PostAsync(
            "/api/v1/animals",
            content);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var animalId =
            await createResponse.Content.ReadFromJsonAsync<int>();

        Assert.True(animalId > 0);

        // Act - Delete
        var deleteResponse = await Client.DeleteAsync(
            $"/api/v1/animals/{animalId}");

        // Assert - Delete
        Assert.Equal(
            HttpStatusCode.OK,
            deleteResponse.StatusCode);

        // Act - Get deleted animal
        var getResponse = await Client.GetAsync(
            $"/api/v1/animals/{animalId}");

        // Assert - Get
        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task CreateAnimal_WithUserRole_ReturnsForbidden()
    {
        // Arrange
        const string email = "integration-user@example.com";
        const string password = "TestUser123!";

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

        Assert.True(animalId > 0);

        // Get created animal so we know its image IDs.
        var initialGetResponse =
            await Client.GetAsync(
                $"/api/v1/animals/{animalId}");

        Assert.Equal(
            HttpStatusCode.OK,
            initialGetResponse.StatusCode);

        var initialAnimal =
            await initialGetResponse.Content
                .ReadFromJsonAsync<AnimalDto>();

        Assert.NotNull(initialAnimal);
        Assert.NotEmpty(initialAnimal.Images);

        var existingPhotoIds =
            initialAnimal.Images
                .Select(x => x.Id)
                .ToList();

        // Update
        using var updateContent =
            AnimalRequestFactory
                .CreateValidUpdateRequest(
                    animalId,
                    existingPhotoIds);

        var updateResponse =
            await Client.PutAsync(
                "/api/v1/animals",
                updateContent);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        // Get again
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

        Assert.True(
            updatedAnimal.IsSterilized);

        Assert.True(
            updatedAnimal.IsVaccinated);

        Assert.Equal(
            new DateOnly(2021, 3, 10),
            updatedAnimal.DateOfBirth);

        Assert.NotEmpty(
            updatedAnimal.Images);
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

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var animalId =
            await createResponse.Content
                .ReadFromJsonAsync<int>();

        Assert.True(animalId > 0);

        // Verify that the animal initially has an image
        var initialGetResponse =
            await Client.GetAsync(
                $"/api/v1/animals/{animalId}");

        Assert.Equal(
            HttpStatusCode.OK,
            initialGetResponse.StatusCode);

        var initialAnimal =
            await initialGetResponse.Content
                .ReadFromJsonAsync<AnimalDto>();

        Assert.NotNull(initialAnimal);
        Assert.NotEmpty(initialAnimal.Images);

        // Act
        // Do NOT pass ExistingPhotoIds.
        // That means the client wants to remove all old images.
        using var updateContent =
            AnimalRequestFactory
                .CreateValidUpdateRequest(
                    animalId,
                    existingPhotoIds: null);

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

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var animalId =
            await createResponse.Content
                .ReadFromJsonAsync<int>();

        Assert.True(animalId > 0);

        // Get original animal
        var initialGetResponse =
            await Client.GetAsync(
                $"/api/v1/animals/{animalId}");

        Assert.Equal(
            HttpStatusCode.OK,
            initialGetResponse.StatusCode);

        var initialAnimal =
            await initialGetResponse.Content
                .ReadFromJsonAsync<AnimalDto>();

        Assert.NotNull(initialAnimal);

        var initialImage =
            Assert.Single(initialAnimal.Images);

        var oldImageId = initialImage.Id;

        // Act
        // existingPhotoIds is null:
        // old photo should be removed.
        //
        // addNewPhoto is true:
        // new photo should be created.
        using var updateContent =
            AnimalRequestFactory
                .CreateValidUpdateRequest(
                    animalId,
                    existingPhotoIds: null,
                    addNewPhoto: true);

        var updateResponse =
            await Client.PutAsync(
                "/api/v1/animals",
                updateContent);

        // Assert - Update
        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        // Get updated animal
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

        var newImage =
            Assert.Single(updatedAnimal.Images);

        Assert.NotEqual(
            oldImageId,
            newImage.Id);
    }
}