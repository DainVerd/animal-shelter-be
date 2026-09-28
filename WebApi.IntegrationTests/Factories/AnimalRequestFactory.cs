using Domain.Enums;
using System.Net.Http.Headers;

namespace WebApi.IntegrationTests.Factories;

public static class AnimalRequestFactory
{
    public static MultipartFormDataContent CreateValidCreateRequest(
        string name = "Integration Dog",
        string breed = "Labrador",
        string description = "Created by integration test")
    {
        var content = new MultipartFormDataContent();

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

        AddPhoto(content);

        return content;
    }

    public static MultipartFormDataContent CreateValidUpdateRequest(
        int id,
        IEnumerable<int>? existingPhotoIds = null,
        string name = "Updated Integration Dog",
        string breed = "Golden Retriever",
        string description = "Updated by integration test",
        string healthNote = "Healthy",
        bool addNewPhoto = false)
    {
        var content = new MultipartFormDataContent();

        content.Add(
            new StringContent(id.ToString()),
            "Id");

        content.Add(
            new StringContent(name),
            "Name");

        content.Add(
            new StringContent(breed),
            "Breed");

        content.Add(
            new StringContent(description),
            "Description");

        content.Add(
            new StringContent(healthNote),
            "HealthNote");

        content.Add(
            new StringContent(Gender.Female.ToString()),
            "Gender");

        content.Add(
            new StringContent(AnimalSize.Large.ToString()),
            "Size");

        content.Add(
            new StringContent(Temperament.Calm.ToString()),
            "Temperament");

        content.Add(
            new StringContent("2021-03-10"),
            "DateOfBirth");

        content.Add(
            new StringContent("true"),
            "IsSterilized");

        content.Add(
            new StringContent("true"),
            "IsVaccinated");

        if (existingPhotoIds is not null)
        {
            foreach (var photoId in existingPhotoIds)
            {
                content.Add(
                    new StringContent(photoId.ToString()),
                    "ExistingPhotoIds");
            }
        }

        if (addNewPhoto)
        {
            AddPhoto(
                content,
                fileName: "updated-animal.jpg");
        }

        return content;
    }

    private static void AddPhoto(
        MultipartFormDataContent content,
        string fileName = "animal.jpg")
    {
        var photoContent =
            new ByteArrayContent(
                new byte[] { 1, 2, 3, 4, 5 });

        photoContent.Headers.ContentType =
            new MediaTypeHeaderValue("image/jpeg");

        content.Add(
            photoContent,
            "NewPhotos",
            fileName);
    }
}