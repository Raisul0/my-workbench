namespace MyWorkbench.Infrastructure;

public interface IFileStorage
{
    Task<string> StoreAsync(Stream content, string contentType, string? originalFileName, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
