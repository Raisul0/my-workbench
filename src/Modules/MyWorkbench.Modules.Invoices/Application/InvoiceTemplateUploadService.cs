using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MyWorkbench.Infrastructure;
using MyWorkbench.Modules.Invoices.Contracts;
using MyWorkbench.Modules.Invoices.Domain;
using MyWorkbench.Modules.Invoices.Persistence;

namespace MyWorkbench.Modules.Invoices.Application;

public sealed class InvoiceTemplateUploadService
{
    private const long MaxTemplateUploadBytes = 20L * 1024 * 1024;

    private readonly InvoiceDbContext _dbContext;
    private readonly IFileStorage _fileStorage;
    private readonly InvoiceTemplateService _templateService;
    private readonly ILogger<InvoiceTemplateUploadService> _logger;

    public InvoiceTemplateUploadService(
        InvoiceDbContext dbContext,
        IFileStorage fileStorage,
        InvoiceTemplateService templateService,
        ILogger<InvoiceTemplateUploadService> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _templateService = templateService;
        _logger = logger;
    }

    public async Task<InvoiceTemplate> UploadAsync(IFormFile file, CreateInvoiceTemplateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(request);

        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceTemplateUploadService), nameof(UploadAsync));
        InvoiceServiceLog.Started(_logger, nameof(UploadAsync));

        try
        {
            if (file.Length <= 0)
            {
                throw new InvalidOperationException("The uploaded template file is empty.");
            }

            if (file.Length > MaxTemplateUploadBytes)
            {
                throw new InvalidOperationException("The uploaded template exceeds the 20 MB size limit.");
            }

            var fileName = Path.GetFileName(file.FileName);
            var uploadValidator = new TemplateUploadValidator();
            if (!uploadValidator.IsAllowedSafeFileName(fileName))
            {
                throw new InvalidOperationException("The uploaded file name is invalid or unsafe.");
            }

            var contentTypeHeader = file.Headers is not null && file.Headers.TryGetValue("Content-Type", out var headerValue)
                ? headerValue.ToString()
                : null;

            var detectedContentType = string.IsNullOrWhiteSpace(contentTypeHeader)
                ? InferContentType(fileName)
                : contentTypeHeader.Trim();

            if (!uploadValidator.IsSupportedContentType(detectedContentType))
            {
                throw new InvalidOperationException("Unsupported file type. Use PDF, PNG, or JPG images.");
            }

            await using var stream = file.OpenReadStream();
            await using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);

            var payload = buffer.ToArray();
            if (!HasSupportedFileSignature(fileName, payload))
            {
                throw new InvalidOperationException("The uploaded file signature does not match a supported PDF or image format.");
            }

            request.SourceType = InferSourceType(fileName, request.SourceType);
            request.SourceStorageKey = await _fileStorage.StoreAsync(new MemoryStream(payload), detectedContentType, fileName, cancellationToken);

            var created = await _templateService.CreateAsync(request, cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(UploadAsync), created.Id);
            return created;
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(UploadAsync));
            throw;
        }
    }

    private static TemplateSourceType InferSourceType(string fileName, TemplateSourceType requestedType)
    {
        var extension = Path.GetExtension(fileName);
        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return TemplateSourceType.Pdf;
        }

        return requestedType == TemplateSourceType.Pdf ? TemplateSourceType.Image : requestedType;
    }

    private static string InferContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => string.Empty
        };
    }

    private static bool HasSupportedFileSignature(string fileName, byte[] payload)
    {
        if (payload.Length == 0)
        {
            return false;
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => payload.Length >= 5 && payload[0] == 0x25 && payload[1] == 0x50 && payload[2] == 0x44 && payload[3] == 0x46 && payload[4] == 0x2D,
            ".png" => payload.Length >= 8 && payload[0] == 0x89 && payload[1] == 0x50 && payload[2] == 0x4E && payload[3] == 0x47 && payload[4] == 0x0D && payload[5] == 0x0A && payload[6] == 0x1A && payload[7] == 0x0A,
            ".jpg" or ".jpeg" => payload.Length >= 2 && payload[0] == 0xFF && payload[1] == 0xD8,
            _ => false
        };
    }
}
