using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MyWorkbench.Modules.Invoices.Contracts;

namespace MyWorkbench.Modules.Invoices.Application;

public sealed class TemplateBackgroundImageService
{
    private const long MaxImageUploadBytes = 10L * 1024 * 1024; // 10 MB
    private readonly string _backgroundsDirectory;
    private readonly ILogger<TemplateBackgroundImageService> _logger;

    public TemplateBackgroundImageService(ILogger<TemplateBackgroundImageService> logger, string? webRootPath = null)
    {
        _logger = logger;
        
        var basePath = webRootPath ?? Directory.GetCurrentDirectory();
        _backgroundsDirectory = Path.Combine(basePath, "resources", "template-backgrounds");
        
        // Ensure directory exists
        if (!Directory.Exists(_backgroundsDirectory))
        {
            Directory.CreateDirectory(_backgroundsDirectory);
        }
    }

    public async Task<string> UploadBackgroundImageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        try
        {
            if (file.Length <= 0)
            {
                throw new InvalidOperationException("The uploaded image file is empty.");
            }

            if (file.Length > MaxImageUploadBytes)
            {
                throw new InvalidOperationException("The uploaded image exceeds the 10 MB size limit.");
            }

            var fileName = Path.GetFileName(file.FileName);
            if (!IsAllowedImageFileName(fileName))
            {
                throw new InvalidOperationException("The uploaded file name is invalid or unsafe.");
            }

            var contentType = file.ContentType?.Trim() ?? InferContentType(fileName);
            if (!IsSupportedImageContentType(contentType))
            {
                throw new InvalidOperationException("Unsupported image type. Use PNG or JPG images.");
            }

            await using var stream = file.OpenReadStream();
            await using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);

            var payload = buffer.ToArray();
            if (!HasSupportedImageSignature(fileName, payload))
            {
                throw new InvalidOperationException("The uploaded file signature does not match a supported image format.");
            }

            // Generate a unique filename
            var uniqueFileName = $"{Guid.NewGuid():N}_{fileName}";
            var filePath = Path.Combine(_backgroundsDirectory, uniqueFileName);

            // Save the file
            await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.WriteAsync(payload, 0, payload.Length, cancellationToken);
            }

            // Return the relative path for use in layoutJson
            var relativePath = $"/resources/template-backgrounds/{uniqueFileName}";
            _logger.LogInformation("Background image uploaded successfully: {FilePath}", relativePath);

            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload background image");
            throw;
        }
    }

    public async Task<string> GetBackgroundImageAsBase64Async(string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(fileName);

        try
        {
            if (!IsAllowedImageFileName(fileName))
            {
                throw new InvalidOperationException("The file name is invalid or unsafe.");
            }

            var filePath = Path.Combine(_backgroundsDirectory, fileName);

            // Security check: ensure the file is within the backgrounds directory
            var fullBackgroundsPath = Path.GetFullPath(_backgroundsDirectory);
            var fullFilePath = Path.GetFullPath(filePath);
            if (!fullFilePath.StartsWith(fullBackgroundsPath))
            {
                throw new InvalidOperationException("Access to this file is not allowed.");
            }

            if (!File.Exists(fullFilePath))
            {
                _logger.LogWarning("Background image file not found: {FilePath}", fullFilePath);
                return string.Empty;
            }

            var imageBytes = await File.ReadAllBytesAsync(fullFilePath, cancellationToken);
            var base64String = Convert.ToBase64String(imageBytes);
            
            var contentType = InferContentType(fileName);
            var dataUrl = $"data:{contentType};base64,{base64String}";
            
            _logger.LogInformation("Background image converted to base64: {FileName}", fileName);
            return dataUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get background image as base64");
            throw;
        }
    }

    private static bool IsAllowedImageFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        // Check for path traversal attempts
        if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension is ".png" or ".jpg" or ".jpeg";
    }

    private static string InferContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }

    private static bool IsSupportedImageContentType(string contentType)
    {
        return contentType is "image/png" or "image/jpeg" or "image/jpg";
    }

    private static bool HasSupportedImageSignature(string fileName, byte[] payload)
    {
        if (payload.Length == 0)
        {
            return false;
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".png" => payload.Length >= 8 && payload[0] == 0x89 && payload[1] == 0x50 && payload[2] == 0x4E && payload[3] == 0x47 && 
                      payload[4] == 0x0D && payload[5] == 0x0A && payload[6] == 0x1A && payload[7] == 0x0A,
            ".jpg" or ".jpeg" => payload.Length >= 2 && payload[0] == 0xFF && payload[1] == 0xD8,
            _ => false
        };
    }
}
