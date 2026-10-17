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

            var invoice = sampleData as Invoice;
            var html = BuildHtmlInvoice(version, invoice);
            var result = Encoding.UTF8.GetBytes(html);
            InvoiceServiceLog.Succeeded(_logger, nameof(RenderPreviewAsync), version.Id);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(RenderPreviewAsync), version.Id);
            throw;
        }
    }

    private static string BuildHtmlInvoice(InvoiceTemplateVersion version, Invoice? invoice)
    {
        var invoiceNumber = invoice?.InvoiceNumber ?? "PREVIEW-001";
        var issuerName = invoice?.IssuerName ?? "Your Business Name";
        var issuerAddress = invoice?.IssuerAddress ?? "123 Business Street";
        var issuerEmail = invoice?.IssuerEmail ?? "billing@example.com";
        var customerName = invoice?.CustomerName ?? "Customer Name";
        var customerAddress = invoice?.CustomerAddress ?? "123 Customer Street";
        var issueDate = invoice?.IssueDate.ToString("MMM d, yyyy") ?? System.DateTime.Now.ToString("MMM d, yyyy");
        var dueDate = invoice?.DueDate?.ToString("MMM d, yyyy") ?? "Upon receipt";
        var currency = invoice?.CurrencyCode ?? "USD";
        var subtotal = invoice?.Subtotal ?? 0m;
        var discountTotal = invoice?.DiscountTotal ?? 0m;
        var taxTotal = invoice?.TaxTotal ?? 0m;
        var grandTotal = invoice?.GrandTotal ?? 0m;
        var lineItems = invoice?.LineItems ?? new List<InvoiceLineItem>();
        var orderedColumns = InvoiceLineItemColumnCatalog.ParseOrDefault(invoice?.LineItemColumns);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\" />");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        sb.Append("  <title>Invoice ").Append(System.Net.WebUtility.HtmlEncode(invoiceNumber)).AppendLine("</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    * { margin: 0; padding: 0; box-sizing: border-box; }");
        sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #1e293b; background: #f8fafc; padding: 2rem; }");
        sb.AppendLine("    .container { max-width: 900px; margin: 0 auto; background: white; box-shadow: 0 20px 60px rgba(15, 23, 42, 0.1); border-radius: 1rem; overflow: hidden; }");
        sb.AppendLine("    .header { display: grid; grid-template-columns: 1fr 1fr; gap: 2rem; padding: 2rem; border-bottom: 2px solid #e2e8f0; }");
        sb.AppendLine("    .issuer-info h1 { font-size: 1.8rem; margin-bottom: 0.5rem; color: #0f172a; }");
        sb.AppendLine("    .issuer-info p { font-size: 0.9rem; color: #64748b; margin: 0.25rem 0; }");
        sb.AppendLine("    .invoice-meta { text-align: right; display: flex; flex-direction: column; justify-content: space-between; }");
        sb.AppendLine("    .invoice-number { font-size: 2rem; font-weight: 700; color: #0f172a; }");
        sb.AppendLine("    .meta-item { display: flex; justify-content: space-between; gap: 1rem; font-size: 0.9rem; margin: 0.5rem 0; }");
        sb.AppendLine("    .meta-label { color: #64748b; font-weight: 600; }");
        sb.AppendLine("    .meta-value { color: #0f172a; }");
        sb.AppendLine("    .parties { display: grid; grid-template-columns: 1fr 1fr; gap: 2rem; padding: 2rem; border-bottom: 1px solid #e2e8f0; }");
        sb.AppendLine("    .party-section h3 { font-size: 0.85rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.1em; color: #64748b; margin-bottom: 0.75rem; }");
        sb.AppendLine("    .party-details { font-size: 0.95rem; line-height: 1.8; }");
        sb.AppendLine("    .party-details p { margin: 0.25rem 0; color: #334155; }");
        sb.AppendLine("    .line-items { padding: 2rem; border-bottom: 1px solid #e2e8f0; }");
        sb.AppendLine("    .line-items h3 { font-size: 0.85rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.1em; color: #64748b; margin-bottom: 1rem; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }");
        sb.AppendLine("    th { background: #f1f5f9; border-bottom: 2px solid #cbd5e1; padding: 0.75rem; text-align: left; font-weight: 600; color: #475569; font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.05em; }");
        sb.AppendLine("    td { border-bottom: 1px solid #e2e8f0; padding: 0.75rem; }");
        sb.AppendLine("    tr:last-child td { border-bottom: none; }");
        sb.AppendLine("    .totals { padding: 2rem; display: flex; justify-content: flex-end; gap: 3rem; }");
        sb.AppendLine("    .totals-column { width: 200px; }");
        sb.AppendLine("    .total-row { display: flex; justify-content: space-between; margin-bottom: 0.5rem; font-size: 0.9rem; }");
        sb.AppendLine("    .total-row.grand-total { border-top: 2px solid #0f172a; border-bottom: 2px solid #0f172a; padding-top: 0.75rem; margin-top: 1rem; font-weight: 700; font-size: 1.1rem; color: #0f172a; }");
        sb.AppendLine("    .total-label { color: #64748b; }");
        sb.AppendLine("    .total-value { text-align: right; font-weight: 600; color: #0f172a; }");
        sb.AppendLine("    .footer { padding: 2rem; background: #f8fafc; border-top: 1px solid #e2e8f0; text-align: center; font-size: 0.85rem; color: #64748b; }");
        sb.AppendLine("    .download-button { position: fixed; top: 2rem; right: 2rem; background: #0f172a; color: white; border: none; border-radius: 0.65rem; padding: 0.7rem 1rem; font-weight: 600; cursor: pointer; box-shadow: 0 10px 25px rgba(15, 23, 42, 0.2); font-size: 0.9rem; }");
        sb.AppendLine("    .download-button:hover { background: #1e293b; }");
        sb.AppendLine("    @media print { body { background: white; padding: 0; } .download-button { display: none; } }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <button class=\"download-button\" onclick=\"window.print()\">Download PDF</button>");
        sb.AppendLine("  <div class=\"container\">");
        sb.AppendLine("    <div class=\"header\">");
        sb.AppendLine("      <div class=\"issuer-info\">");
        sb.AppendLine("        <h1>Invoice</h1>");
        sb.Append("        <p><strong>").Append(System.Net.WebUtility.HtmlEncode(issuerName)).AppendLine("</strong></p>");
        sb.Append("        <p>").Append(System.Net.WebUtility.HtmlEncode(issuerAddress)).AppendLine("</p>");
        sb.Append("        <p>").Append(System.Net.WebUtility.HtmlEncode(issuerEmail)).AppendLine("</p>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"invoice-meta\">");
        sb.Append("        <div class=\"invoice-number\">#").Append(System.Net.WebUtility.HtmlEncode(invoiceNumber)).AppendLine("</div>");
        sb.AppendLine("        <div>");
        sb.AppendLine("          <div class=\"meta-item\">");
        sb.AppendLine("            <span class=\"meta-label\">Issue Date:</span>");
        sb.Append("            <span class=\"meta-value\">").Append(issueDate).AppendLine("</span>");
        sb.AppendLine("          </div>");
        sb.AppendLine("          <div class=\"meta-item\">");
        sb.AppendLine("            <span class=\"meta-label\">Due Date:</span>");
        sb.Append("            <span class=\"meta-value\">").Append(dueDate).AppendLine("</span>");
        sb.AppendLine("          </div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class=\"parties\">");
        sb.AppendLine("      <div class=\"party-section\">");
        sb.AppendLine("        <h3>Bill To</h3>");
        sb.AppendLine("        <div class=\"party-details\">");
        sb.Append("          <p><strong>").Append(System.Net.WebUtility.HtmlEncode(customerName)).AppendLine("</strong></p>");
        sb.Append("          <p>").Append(System.Net.WebUtility.HtmlEncode(customerAddress)).AppendLine("</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"party-section\">");
        sb.AppendLine("        <h3>From</h3>");
        sb.AppendLine("        <div class=\"party-details\">");
        sb.Append("          <p><strong>").Append(System.Net.WebUtility.HtmlEncode(issuerName)).AppendLine("</strong></p>");
        sb.Append("          <p>").Append(System.Net.WebUtility.HtmlEncode(issuerAddress)).AppendLine("</p>");
        sb.Append("          <p>").Append(System.Net.WebUtility.HtmlEncode(issuerEmail)).AppendLine("</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class=\"line-items\">");
        sb.AppendLine("      <h3>Line Items</h3>");
        sb.AppendLine("      <table>");
        sb.AppendLine("        <thead>");
        sb.AppendLine("          <tr>");

        foreach (var column in orderedColumns)
        {
            var alignment = IsRightAlignedColumn(column) ? " style=\"text-align: right;\"" : string.Empty;
            var label = InvoiceLineItemColumnCatalog.Labels.TryGetValue(column, out var mappedLabel) ? mappedLabel : column;
            sb.Append("            <th").Append(alignment).Append(">")
                .Append(System.Net.WebUtility.HtmlEncode(label))
                .AppendLine("</th>");
        }

        sb.AppendLine("          </tr>");
        sb.AppendLine("        </thead>");
        sb.AppendLine("        <tbody>");

        foreach (var pair in lineItems.Select((li, index) => new { li, index }))
        {
            sb.AppendLine("          <tr>");

            foreach (var column in orderedColumns)
            {
                var alignment = IsRightAlignedColumn(column) ? " style=\"text-align: right;\"" : string.Empty;
                var cellValue = BuildLineItemCell(column, pair.li, pair.index, customerName, issueDate, currency);
                sb.Append("            <td").Append(alignment).Append(">")
                    .Append(System.Net.WebUtility.HtmlEncode(cellValue))
                    .AppendLine("</td>");
            }

            sb.AppendLine("          </tr>");
        }

        sb.AppendLine("        </tbody>");
        sb.AppendLine("      </table>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class=\"totals\">");
        sb.AppendLine("      <div class=\"totals-column\">");
        sb.AppendLine("        <div class=\"total-row\">");
        sb.AppendLine("          <span class=\"total-label\">Subtotal:</span>");
        sb.Append("          <span class=\"total-value\">").Append(currency).Append(" ").Append(subtotal.ToString("F2")).AppendLine("</span>");
        sb.AppendLine("        </div>");

        if (discountTotal > 0m)
        {
            sb.AppendLine("        <div class=\"total-row\">");
            sb.AppendLine("          <span class=\"total-label\">Discount:</span>");
            sb.Append("          <span class=\"total-value\">- ").Append(currency).Append(" ").Append(discountTotal.ToString("F2")).AppendLine("</span>");
            sb.AppendLine("        </div>");
        }

        if (taxTotal > 0m)
        {
            sb.AppendLine("        <div class=\"total-row\">");
            sb.AppendLine("          <span class=\"total-label\">Tax:</span>");
            sb.Append("          <span class=\"total-value\">").Append(currency).Append(" ").Append(taxTotal.ToString("F2")).AppendLine("</span>");
            sb.AppendLine("        </div>");
        }

        sb.AppendLine("        <div class=\"total-row grand-total\">");
        sb.AppendLine("          <span>Total:</span>");
        sb.Append("          <span>").Append(currency).Append(" ").Append(grandTotal.ToString("F2")).AppendLine("</span>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class=\"footer\">");
        sb.AppendLine("      <p>Thank you for your business.</p>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static bool IsRightAlignedColumn(string column)
    {
        return column is InvoiceLineItemColumnCatalog.Hours or InvoiceLineItemColumnCatalog.Rate or InvoiceLineItemColumnCatalog.Total;
    }

    private static string BuildLineItemCell(string column, InvoiceLineItem lineItem, int index, string customerName, string issueDate, string currency)
    {
        return column switch
        {
            var value when value == InvoiceLineItemColumnCatalog.ItemNo => (index + 1).ToString(),
            var value when value == InvoiceLineItemColumnCatalog.Date => issueDate,
            var value when value == InvoiceLineItemColumnCatalog.Client => customerName,
            var value when value == InvoiceLineItemColumnCatalog.Project => string.Empty,
            var value when value == InvoiceLineItemColumnCatalog.Description => lineItem.Description,
            var value when value == InvoiceLineItemColumnCatalog.Hours => lineItem.Quantity.ToString("F2"),
            var value when value == InvoiceLineItemColumnCatalog.Rate => $"{currency} {lineItem.UnitPrice:F2}",
            var value when value == InvoiceLineItemColumnCatalog.Total => $"{currency} {lineItem.LineTotal:F2}",
            _ => string.Empty
        };
    }
}
