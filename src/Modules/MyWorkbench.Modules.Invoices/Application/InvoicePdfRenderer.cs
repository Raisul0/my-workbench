using System.Text;
using Microsoft.Extensions.Logging;
using MyWorkbench.Modules.Invoices.Domain;

namespace MyWorkbench.Modules.Invoices.Application;

public sealed class InvoicePdfRenderer : IInvoicePdfRenderer
{
    private readonly ILogger<InvoicePdfRenderer> _logger;

    public InvoicePdfRenderer(ILogger<InvoicePdfRenderer> logger)
    {
        _logger = logger;
    }

    public Task<byte[]> RenderPreviewAsync(InvoiceTemplateVersion version, object? sampleData = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoicePdfRenderer), nameof(RenderPreviewAsync), version.Id);
        InvoiceServiceLog.Started(_logger, nameof(RenderPreviewAsync), version.Id);

        try
        {
            if (version.LayoutSchemaVersion <= 0)
            {
                throw new InvalidOperationException("The template layout schema version must be greater than zero.");
            }

            var pdfText = BuildMinimalPdf(version);
            var result = Encoding.ASCII.GetBytes(pdfText);
            InvoiceServiceLog.Succeeded(_logger, nameof(RenderPreviewAsync), version.Id);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(RenderPreviewAsync), version.Id);
            throw;
        }
    }

    private static string BuildMinimalPdf(InvoiceTemplateVersion version)
    {
        var width = version.PageWidthPoints > 0 ? version.PageWidthPoints : 595m;
        var height = version.PageHeightPoints > 0 ? version.PageHeightPoints : 842m;

        var pageWidth = decimal.ToInt32(width);
        var pageHeight = decimal.ToInt32(height);

        var body = $@"BT
/F1 12 Tf
72 760 Td
(Preview: {version.LayoutSchemaVersion}) Tj
ET";

        var contentLength = Encoding.ASCII.GetByteCount(body);

        return $"%PDF-1.4\n" +
               "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n" +
               "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n" +
               $"3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageWidth} {pageHeight}] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n" +
               "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n" +
               $"5 0 obj\n<< /Length {contentLength} >>\nstream\n{body}\nendstream\nendobj\n" +
               "xref\n0 6\n0000000000 65535 f \n" +
               "0000000010 00000 n \n" +
               "0000000062 00000 n \n" +
               "0000000123 00000 n \n" +
               "0000000798 00000 n \n" +
               "0000000000 00000 n \n" +
               "trailer\n<< /Root 1 0 R /Size 6 >>\nstartxref\n0\n%%EOF\n";
    }
}
