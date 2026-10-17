using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyWorkbench.Infrastructure;
using MyWorkbench.Modules.Invoices.Domain;
using MyWorkbench.Modules.Invoices.Persistence;

namespace MyWorkbench.Modules.Invoices.Application;

public sealed class InvoiceGenerationService
{
    private readonly InvoiceDbContext _dbContext;
    private readonly IFileStorage _fileStorage;
    private readonly IInvoicePdfRenderer _pdfRenderer;
    private readonly ILogger<InvoiceGenerationService> _logger;

    public InvoiceGenerationService(
        InvoiceDbContext dbContext,
        IFileStorage fileStorage,
        IInvoicePdfRenderer pdfRenderer,
        ILogger<InvoiceGenerationService> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _pdfRenderer = pdfRenderer;
        _logger = logger;
    }

    public async Task<byte[]> PreviewAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceGenerationService), nameof(PreviewAsync), invoiceId);
        InvoiceServiceLog.Started(_logger, nameof(PreviewAsync), invoiceId);

        try
        {
            var invoice = await LoadInvoiceAsync(invoiceId, cancellationToken);
            var templateVersion = await LoadTemplateVersionAsync(invoice.TemplateVersionId, cancellationToken);
            var bytes = await _pdfRenderer.RenderPreviewAsync(templateVersion, invoice, cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(PreviewAsync), invoiceId);
            return bytes;
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(PreviewAsync), invoiceId);
            throw;
        }
    }

    public async Task<Invoice> GenerateAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceGenerationService), nameof(GenerateAsync), invoiceId);
        InvoiceServiceLog.Started(_logger, nameof(GenerateAsync), invoiceId);

        try
        {
            var invoice = await LoadInvoiceAsync(invoiceId, cancellationToken);
            var templateVersion = await LoadTemplateVersionAsync(invoice.TemplateVersionId, cancellationToken);

            var bytes = await _pdfRenderer.RenderPreviewAsync(templateVersion, invoice, cancellationToken);
            var storageKey = await _fileStorage.StoreAsync(new MemoryStream(bytes), "application/pdf", $"{invoice.InvoiceNumber}.pdf", cancellationToken);
            var checksum = ComputeSha256(bytes);

            invoice.PdfStorageKey = storageKey;
            invoice.PdfSha256 = checksum;
            invoice.Status = InvoiceStatus.Generated;
            invoice.GeneratedAtUtc = DateTimeOffset.UtcNow;
            invoice.UpdatedAtUtc = DateTimeOffset.UtcNow;

            _dbContext.Invoices.Update(invoice);
            await _dbContext.SaveChangesAsync(cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(GenerateAsync), invoiceId);

            return invoice;
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(GenerateAsync), invoiceId);
            throw;
        }
    }

    private async Task<Invoice> LoadInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await _dbContext.Invoices
            .Include(x => x.BusinessProfile)
            .Include(x => x.LineItems)
            .SingleOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);

        if (invoice is null)
        {
            throw new InvalidOperationException("The requested invoice was not found.");
        }

        return invoice;
    }

    private async Task<InvoiceTemplateVersion> LoadTemplateVersionAsync(Guid templateVersionId, CancellationToken cancellationToken)
    {
        if (templateVersionId == Guid.Empty)
        {
            throw new InvalidOperationException("The invoice is not associated with a valid template version.");
        }

        var templateVersion = await _dbContext.InvoiceTemplateVersions
            .SingleOrDefaultAsync(x => x.Id == templateVersionId, cancellationToken);

        if (templateVersion is null)
        {
            throw new InvalidOperationException("The referenced template version could not be found.");
        }

        return templateVersion;
    }

    private static string ComputeSha256(byte[] payload)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(payload)).ToLowerInvariant();
    }
}
