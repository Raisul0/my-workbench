namespace MyWorkbench.Infrastructure;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage()
    {
        _rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyWorkbench", "private-storage");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> StoreAsync(Stream content, string contentType, string? originalFileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("A content type is required.", nameof(contentType));
        }

        var extension = ".bin";
        if (!string.IsNullOrWhiteSpace(originalFileName))
        {
            extension = Path.GetExtension(originalFileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".bin";
            }
        }

        var storageKey = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_rootPath, storageKey);

        await using var destination = File.Create(fullPath);
        await content.CopyToAsync(destination, cancellationToken);

        return storageKey;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("A storage key is required.", nameof(storageKey));
        }

        var fullPath = Path.Combine(_rootPath, storageKey);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The requested file could not be found.", fullPath);
        }

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream>(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("A storage key is required.", nameof(storageKey));
        }

        var fullPath = Path.Combine(_rootPath, storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
