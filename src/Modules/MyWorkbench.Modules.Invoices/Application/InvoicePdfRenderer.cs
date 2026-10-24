using Microsoft.Extensions.Logging;
using MyWorkbench.Modules.Invoices.Domain;
using System.Text;
using System.Text.Json;

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

    public Task<string> RenderPreviewHtmlAsync(InvoiceTemplateVersion version, object? sampleData = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoicePdfRenderer), nameof(RenderPreviewHtmlAsync), version.Id);
        InvoiceServiceLog.Started(_logger, nameof(RenderPreviewHtmlAsync), version.Id);

        try
        {
            if (version.LayoutSchemaVersion <= 0)
            {
                throw new InvalidOperationException("The template layout schema version must be greater than zero.");
            }

            var invoice = sampleData as Invoice;
            var html = BuildHtmlInvoice(version, invoice);
            InvoiceServiceLog.Succeeded(_logger, nameof(RenderPreviewHtmlAsync), version.Id);
            return Task.FromResult(html);
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(RenderPreviewHtmlAsync), version.Id);
            throw;
        }
    }

    private static string GetFieldClass(string fieldType)
    {
        return fieldType.ToLowerInvariant() switch
        {
            "currency" => "field-currency",
            "number" => "field-number",
            "address" => "field-address",
            "table" => "field-table",
            _ => "field-text"
        };
    }
    private static TemplateLayout ParseTemplateLayout(string layoutJson)
    {
        var layout = new TemplateLayout { Fields = new List<TemplateField>(), BackgroundImagePath = string.Empty };

        if (string.IsNullOrWhiteSpace(layoutJson))
            return layout;

        try
        {
            using var doc = JsonDocument.Parse(layoutJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("BackgroundImagePath", out var bgProp))
                layout.BackgroundImagePath = bgProp.GetString() ?? string.Empty;

            if (root.TryGetProperty("Fields", out var fieldsArray))
            {
                foreach (var fieldElement in fieldsArray.EnumerateArray())
                {
                    var field = new TemplateField();

                    if (fieldElement.TryGetProperty("Name", out var nameProp))
                        field.Name = nameProp.GetString() ?? string.Empty;

                    if (fieldElement.TryGetProperty("Type", out var typeProp))
                        field.Type = typeProp.GetString() ?? "Text";

                    if (fieldElement.TryGetProperty("X", out var xProp))
                        field.X = xProp.GetDecimal();

                    if (fieldElement.TryGetProperty("Y", out var yProp))
                        field.Y = yProp.GetDecimal();

                    if (fieldElement.TryGetProperty("Width", out var wProp))
                        field.Width = wProp.GetDecimal();

                    if (fieldElement.TryGetProperty("Height", out var hProp))
                        field.Height = hProp.GetDecimal();

                    if (fieldElement.TryGetProperty("ContentSource", out var csProp))
                        field.ContentSource = csProp.GetString() ?? string.Empty;

                    if (fieldElement.TryGetProperty("CustomText", out var ctProp))
                        field.CustomText = ctProp.GetString() ?? string.Empty;

                    if (fieldElement.TryGetProperty("FontSize", out var fsProp) && fsProp.ValueKind == JsonValueKind.Number)
                        field.FontSize = fsProp.GetInt32();

                    if (fieldElement.TryGetProperty("FontStyle", out var fstProp))
                        field.FontStyle = fstProp.GetString() ?? string.Empty;

                    if (fieldElement.TryGetProperty("TextColor", out var tcProp))
                        field.TextColor = tcProp.GetString() ?? string.Empty;

                    if (fieldElement.TryGetProperty("BackgroundColor", out var bcProp))
                        field.BackgroundColor = bcProp.GetString() ?? string.Empty;

                    if (fieldElement.TryGetProperty("BorderSize", out var bsProp) && bsProp.ValueKind == JsonValueKind.Number)
                        field.BorderSize = bsProp.GetInt32();

                    if (fieldElement.TryGetProperty("LineItemColumns", out var colsArray) && colsArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var colEl in colsArray.EnumerateArray())
                        {
                            var col = new TemplateLineItemColumn();
                            if (colEl.TryGetProperty("Key", out var ckProp)) col.Key = ckProp.GetString() ?? string.Empty;
                            if (colEl.TryGetProperty("Label", out var clProp)) col.Label = clProp.GetString() ?? string.Empty;
                            if (colEl.TryGetProperty("WidthMm", out var cwProp) && cwProp.ValueKind == JsonValueKind.Number) col.WidthMm = cwProp.GetDecimal();
                            if (colEl.TryGetProperty("Type", out var ctypeProp)) col.Type = ctypeProp.GetString() ?? "text";
                            field.LineItemColumns.Add(col);
                        }
                    }

                    layout.Fields.Add(field);
                }
            }
        }
        catch (JsonException)
        {
            // Layout JSON is invalid; return default empty layout
        }

        return layout;
    }
    private static decimal PointsToPixels(decimal points)
    {
        // Standard conversion: 1 point = 1.333... pixels (96 DPI / 72 points per inch)
        return Math.Round(points * 96m / 72m, 2);
    }

    private class TemplateLayout
    {
        public List<TemplateField> Fields { get; set; } = new();
        public string BackgroundImagePath { get; set; } = string.Empty;
    }

    private class TemplateField
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Text";
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public string ContentSource { get; set; } = string.Empty;
        public string CustomText { get; set; } = string.Empty;
        public int FontSize { get; set; } = 12;
        public string FontStyle { get; set; } = "normal";
        public string TextColor { get; set; } = "#000000";
        public string BackgroundColor { get; set; } = string.Empty;
        public int BorderSize { get; set; }
        public List<TemplateLineItemColumn> LineItemColumns { get; set; } = new();
    }

    private class TemplateLineItemColumn
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public decimal WidthMm { get; set; }
        public string Type { get; set; } = "text";
    }


    private static string BuildHtmlInvoice(InvoiceTemplateVersion version, Invoice? invoice)
    {
        var invoiceNumber = invoice?.InvoiceNumber ?? "PREVIEW-001";
        var profileName = invoice?.CustomerProfile?.CustomerName ?? "Customer Name";
        var profileAddress = invoice?.CustomerProfile?.CustomerAddress ?? string.Empty;
        var profileEmail = invoice?.CustomerProfile?.CustomerEmail ?? string.Empty;
        var issueDate = invoice?.IssueDate.ToString("MMM d, yyyy") ?? System.DateTime.Now.ToString("MMM d, yyyy");
        var dueDate = invoice?.DueDate?.ToString("MMM d, yyyy") ?? "Upon receipt";
        var currency = invoice?.CurrencyCode ?? "USD";
        var subtotal = invoice?.Subtotal ?? 0m;
        var discountTotal = invoice?.DiscountTotal ?? 0m;
        var taxTotal = invoice?.TaxTotal ?? 0m;
        var grandTotal = invoice?.GrandTotal ?? 0m;
        var lineItems = invoice?.LineItems ?? new List<InvoiceLineItem>();

        // Parse layout to extract column configs and field styles
        var layout = ParseTemplateLayout(version.LayoutJson);
        var tableField = layout.Fields.FirstOrDefault(f =>
            f.Type.Equals("Table", StringComparison.OrdinalIgnoreCase) ||
            f.ContentSource.Equals("lineItems", StringComparison.OrdinalIgnoreCase));

        // Use layout column configs when available; otherwise fall back to invoice columns
        IReadOnlyList<(string Name, string Type, string Label, decimal WidthPct)> orderedColumnsWithMeta;
        if (tableField is not null && tableField.LineItemColumns.Count > 0)
        {
            var totalWidth = tableField.LineItemColumns.Sum(c => c.WidthMm);
            if (totalWidth <= 0) totalWidth = 1;
            orderedColumnsWithMeta = tableField.LineItemColumns
                .Select(c => (c.Key, c.Type, c.Label.Length > 0 ? c.Label : ToDisplayLabel(c.Key), Math.Round(c.WidthMm / totalWidth * 100m, 1)))
                .ToList();
        }
        else
        {
            var fallback = ParseLineItemColumns(invoice?.LineItemColumns);
            orderedColumnsWithMeta = fallback.Select(c => (c.Name, c.Type, ToDisplayLabel(c.Name), 0m)).ToList();
        }

        // Extract styles from layout Table field
        var tableFontSize = (tableField?.FontSize ?? 0) > 0 ? tableField!.FontSize : 13;
        var tableTextColor = !string.IsNullOrWhiteSpace(tableField?.TextColor) ? tableField!.TextColor : "#475569";
        var tableHeaderBg = !string.IsNullOrWhiteSpace(tableField?.BackgroundColor) ? tableField!.BackgroundColor : "#f1f5f9";

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
        sb.Append("    th { background: ").Append(tableHeaderBg).Append("; border-bottom: 2px solid #cbd5e1; padding: 0.75rem; text-align: left; font-weight: 600; color: ").Append(tableTextColor).AppendLine("; font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.05em; }");
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
        sb.Append("        <p><strong>").Append(System.Net.WebUtility.HtmlEncode(profileName)).AppendLine("</strong></p>");
        sb.Append("        <p>").Append(System.Net.WebUtility.HtmlEncode(profileAddress)).AppendLine("</p>");
        sb.Append("        <p>").Append(System.Net.WebUtility.HtmlEncode(profileEmail)).AppendLine("</p>");
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
        sb.Append("          <p><strong>").Append(System.Net.WebUtility.HtmlEncode(profileName)).AppendLine("</strong></p>");
        sb.Append("          <p>").Append(System.Net.WebUtility.HtmlEncode(profileAddress)).AppendLine("</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"party-section\">");
        sb.AppendLine("        <h3>From</h3>");
        sb.AppendLine("        <div class=\"party-details\">");
        sb.AppendLine("          <p><strong>My Workbench</strong></p>");
        sb.AppendLine("          <p>Generated invoice document</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class=\"line-items\">");
        sb.AppendLine("      <h3>Line Items</h3>");
        sb.AppendLine("      <table>");
        sb.AppendLine("        <thead>");
        sb.AppendLine("          <tr>");

        foreach (var column in orderedColumnsWithMeta)
        {
            var isRight = IsRightAlignedColumn(column.Name, column.Type);
            var alignment = isRight ? " style=\"text-align: right;\"" : string.Empty;
            var widthStyle = column.WidthPct > 0 ? $" style=\"width: {column.WidthPct}%;{(isRight ? " text-align: right;" : "")}\"" : alignment;
            sb.Append("            <th").Append(widthStyle).Append(">")
                .Append(System.Net.WebUtility.HtmlEncode(column.Label))
                .AppendLine("</th>");
        }

        sb.AppendLine("          </tr>");
        sb.AppendLine("        </thead>");
        sb.AppendLine("        <tbody>");

        foreach (var pair in lineItems.Select((li, index) => new { li, index }))
        {
            sb.AppendLine("          <tr>");

            foreach (var column in orderedColumnsWithMeta)
            {
                var isRight = IsRightAlignedColumn(column.Name, column.Type);
                var alignment = isRight ? " style=\"text-align: right;\"" : string.Empty;
                var cellValue = BuildLineItemCell((column.Name, column.Type), pair.li, pair.index, issueDate, currency);
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

    private static bool IsRightAlignedColumn(string columnName, string columnType)
    {
        return columnType.Equals("number", StringComparison.OrdinalIgnoreCase)
               || columnName.Equals("total", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildLineItemCell((string Name, string Type) column, InvoiceLineItem lineItem, int index, string issueDate, string currency)
    {
        if (column.Name.Equals("description", StringComparison.OrdinalIgnoreCase))
        {
            return lineItem.Description;
        }

        if (column.Name.Equals("total", StringComparison.OrdinalIgnoreCase))
        {
            return $"{currency} {lineItem.Total:F2}";
        }

        if (column.Name.Equals("itemNo", StringComparison.OrdinalIgnoreCase))
        {
            return (index + 1).ToString();
        }

        if (column.Name.Equals("date", StringComparison.OrdinalIgnoreCase))
        {
            return issueDate;
        }

        var otherColumns = ParseOtherColumns(lineItem.OtherColumns);
        return otherColumns.TryGetValue(column.Name, out var value) ? value : string.Empty;
    }

    private static IReadOnlyList<(string Name, string Type)> ParseLineItemColumns(string? columnsCsv)
    {
        var columns = new List<(string Name, string Type)>();
        if (string.IsNullOrWhiteSpace(columnsCsv))
        {
            columns.Add(("description", "text"));
            columns.Add(("total", "number"));
            return columns;
        }

        foreach (var segment in columnsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = segment.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            {
                continue;
            }

            var name = parts[0].Trim();
            var type = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : "text";
            columns.Add((name, type));
        }

        if (!columns.Any(c => c.Name.Equals("description", StringComparison.OrdinalIgnoreCase)))
        {
            columns.Insert(0, ("description", "text"));
        }

        if (!columns.Any(c => c.Name.Equals("total", StringComparison.OrdinalIgnoreCase)))
        {
            columns.Add(("total", "number"));
        }

        return columns;
    }

    private static Dictionary<string, string> ParseOtherColumns(string? otherColumns)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(otherColumns))
        {
            return result;
        }

        foreach (var segment in otherColumns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = segment.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[0]))
            {
                continue;
            }

            result[parts[0].Trim()] = parts[1].Trim();
        }

        return result;
    }

    private static string ToDisplayLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var spaced = value
            .Replace("_", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal);
        spaced = System.Text.RegularExpressions.Regex.Replace(spaced, "([a-z])([A-Z])", "$1 $2");
        return char.ToUpperInvariant(spaced[0]) + spaced[1..];
    }
}
