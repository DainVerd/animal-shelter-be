using Application.Entities;
using Application.Interfaces.Services;

namespace WebApi.IntegrationTests.Fakes;

public class FakeFileStorageService : IFileStorageService
{
    public Task<string> UploadFileAsync(
        FileRequest request,
        CancellationToken token = default)
    {
        return Task.FromResult(
            $"https://test-storage.local/{request.Key}");
    }

    public Task DeleteFileAsync(
        string key,
        CancellationToken token = default)
    {
        return Task.CompletedTask;
    }
}