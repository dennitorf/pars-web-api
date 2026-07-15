using KellyServices.PARS.Application.Common.Interfaces.Archive;
using KellyServices.PARS.Application.Common.Interfaces.Identity;
using KellyServices.PARS.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace KellyServices.PARS.Application.UnitTests;

internal static class TestDb
{
    internal static AppDbContext Create() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}

internal sealed class TestCurrentUser : ICurrentUserService
{
    public string UserId => "payroll.specialist@kelly.test";
    public string DisplayName => "Payroll Specialist";
    public string CorrelationId => "test-correlation";
}

internal sealed class TestArchiveFileStore : IArchiveFileStore
{
    public List<(string Container, string Blob, byte[] Content)> Uploads { get; } = [];
    public Task UploadAsync(string containerName, string blobName, Stream content, string contentType,
        IReadOnlyDictionary<string, string> metadata, CancellationToken cancellationToken)
    {
        content.Position = 0;
        using var copy = new MemoryStream();
        content.CopyTo(copy);
        Uploads.Add((containerName, blobName, copy.ToArray()));
        return Task.CompletedTask;
    }
    public Task<Stream> OpenReadAsync(string containerName, string blobName, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream(Uploads.Single(x => x.Container == containerName && x.Blob == blobName).Content));
}

internal sealed class TestSftpArchiveSource : ISftpArchiveSource
{
    public Dictionary<string, byte[]> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Moved { get; } = [];
    public Task<bool> ExistsAsync(string remotePath, CancellationToken cancellationToken) => Task.FromResult(Files.ContainsKey(remotePath));
    public async Task DownloadAsync(string remotePath, Stream destination, CancellationToken cancellationToken) =>
        await destination.WriteAsync(Files[remotePath], cancellationToken);
    public Task MoveToProcessedAsync(string remotePath, string processedDirectory, CancellationToken cancellationToken)
    {
        Moved.Add(remotePath);
        return Task.CompletedTask;
    }
}
