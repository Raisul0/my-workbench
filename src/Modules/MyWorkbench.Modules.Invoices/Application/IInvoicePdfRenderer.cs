using MyWorkbench.Modules.Invoices.Domain;

namespace MyWorkbench.Modules.Invoices.Application;

public interface IInvoicePdfRenderer
{
    Task<byte[]> RenderPreviewAsync(InvoiceTemplateVersion version, object? sampleData = null, CancellationToken cancellationToken = default);
}
