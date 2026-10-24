using Microsoft.Extensions.Logging;
using MyWorkbench.Modules.Invoices.Domain;
using System.Text;
using System.Text.Json;

namespace MyWorkbench.Modules.Invoices.Application;

public sealed class InvoicePdfRenderer : IInvoicePdfRenderer
{
    private readonly ILogger<InvoicePdfRenderer> _logger;
    private readonly TemplateBackgroundImageService _backgroundImages;

    public InvoicePdfRenderer(ILogger<InvoicePdfRenderer> logger, TemplateBackgroundImageService backgroundImages)
    {
        _logger = logger;
        _backgroundImages = backgroundImages;
    }

    public async Task<byte[]> RenderPreviewAsync(InvoiceTemplateVersion version, object? sampleData = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoicePdfRenderer), nameof(RenderPreviewAsync), version.Id);
        InvoiceServiceLog.Started(_logger, nameof(RenderPreviewAsync), version.Id);

        try
        {
            if (version.LayoutSchemaVersion <= 0)
                throw new InvalidOperationException("The template layout schema version must be greater than zero.");

            var html = await BuildHtmlAsync(version, sampleData as Invoice, cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(RenderPreviewAsync), version.Id);
            return Encoding.UTF8.GetBytes(html);
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(RenderPreviewAsync), version.Id);
            throw;
        }
    }

    public async Task<string> RenderPreviewHtmlAsync(InvoiceTemplateVersion version, object? sampleData = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoicePdfRenderer), nameof(RenderPreviewHtmlAsync), version.Id);
        InvoiceServiceLog.Started(_logger, nameof(RenderPreviewHtmlAsync), version.Id);

        try
        {
            if (version.LayoutSchemaVersion <= 0)
                throw new InvalidOperationException("The template layout schema version must be greater than zero.");

            var html = await BuildHtmlAsync(version, sampleData as Invoice, cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(RenderPreviewHtmlAsync), version.Id);
            return html;
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(RenderPreviewHtmlAsync), version.Id);
            throw;
        }
    }

    // â”€â”€â”€ Layout parsing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static TemplateLayout ParseTemplateLayout(string layoutJson)
    {
        var layout = new TemplateLayout();
        if (string.IsNullOrWhiteSpace(layoutJson))
            return layout;

        try
        {
            using var doc = JsonDocument.Parse(layoutJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("BackgroundImagePath", out var bgProp))
                layout.BackgroundImagePath = bgProp.GetString() ?? string.Empty;

            if (!root.TryGetProperty("Fields", out var fieldsArray))
                return layout;

            foreach (var el in fieldsArray.EnumerateArray())
            {
                var f = new TemplateField();
                if (el.TryGetProperty("Name", out var p)) f.Name = p.GetString() ?? string.Empty;
                if (el.TryGetProperty("Type", out p)) f.Type = p.GetString() ?? "Text";
                if (el.TryGetProperty("X", out p) && p.ValueKind == JsonValueKind.Number) f.X = p.GetDecimal();
                if (el.TryGetProperty("Y", out p) && p.ValueKind == JsonValueKind.Number) f.Y = p.GetDecimal();
                if (el.TryGetProperty("Width", out p) && p.ValueKind == JsonValueKind.Number) f.Width = p.GetDecimal();
                if (el.TryGetProperty("Height", out p) && p.ValueKind == JsonValueKind.Number) f.Height = p.GetDecimal();
                if (el.TryGetProperty("ContentSource", out p)) f.ContentSource = p.GetString() ?? string.Empty;
                if (el.TryGetProperty("CustomText", out p)) f.CustomText = p.GetString() ?? string.Empty;
                if (el.TryGetProperty("FontSize", out p) && p.ValueKind == JsonValueKind.Number) f.FontSize = p.GetInt32();
                if (el.TryGetProperty("FontStyle", out p)) f.FontStyle = p.GetString() ?? string.Empty;
                if (el.TryGetProperty("TextColor", out p)) f.TextColor = p.GetString() ?? string.Empty;
                if (el.TryGetProperty("BackgroundColor", out p)) f.BackgroundColor = p.GetString() ?? string.Empty;
                if (el.TryGetProperty("BorderSize", out p) && p.ValueKind == JsonValueKind.Number) f.BorderSize = p.GetInt32();

                if (el.TryGetProperty("LineItemColumns", out var colsEl) && colsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ce in colsEl.EnumerateArray())
                    {
                        var c = new TemplateLineItemColumn();
                        if (ce.TryGetProperty("Key", out p)) c.Key = p.GetString() ?? string.Empty;
                        if (ce.TryGetProperty("Label", out p)) c.Label = p.GetString() ?? string.Empty;
                        if (ce.TryGetProperty("WidthMm", out p) && p.ValueKind == JsonValueKind.Number) c.WidthMm = p.GetDecimal();
                        if (ce.TryGetProperty("Type", out p)) c.Type = p.GetString() ?? "text";
                        f.LineItemColumns.Add(c);
                    }
                }

                layout.Fields.Add(f);
            }
        }
        catch (JsonException) { /* return whatever was parsed */ }

        return layout;
    }

    // â”€â”€â”€ HTML builder â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async Task<string> BuildHtmlAsync(InvoiceTemplateVersion version, Invoice? invoice, CancellationToken cancellationToken)
    {
        var layout = ParseTemplateLayout(version.LayoutJson);

        // Resolve invoice data (or preview placeholders)
        var invoiceNumber = invoice?.InvoiceNumber ?? "INV-PREVIEW";
        var customerName = invoice?.CustomerProfile?.CustomerName ?? "Sample Customer";
        var customerAddress = invoice?.CustomerProfile?.CustomerAddress ?? string.Empty;
        var customerEmail = invoice?.CustomerProfile?.CustomerEmail ?? string.Empty;
        var issueDate = invoice?.IssueDate.ToString("MMM d, yyyy") ?? DateTime.Now.ToString("MMM d, yyyy");
        var dueDate = invoice?.DueDate?.ToString("MMM d, yyyy") ?? string.Empty;
        var currency = invoice?.CurrencyCode ?? "USD";
        var subtotal = invoice?.Subtotal ?? 0m;
        var discountTotal = invoice?.DiscountTotal ?? 0m;
        var taxTotal = invoice?.TaxTotal ?? 0m;
        var grandTotal = invoice?.GrandTotal ?? 0m;
        var lineItems = invoice?.LineItems?.OrderBy(li => li.SortOrder).ToList() ?? new List<InvoiceLineItem>();

        // Load background image as embedded data URL
        var bgDataUrl = string.Empty;
        if (!string.IsNullOrWhiteSpace(layout.BackgroundImagePath))
        {
            var fileName = Path.GetFileName(layout.BackgroundImagePath);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                try { bgDataUrl = await _backgroundImages.GetBackgroundImageAsBase64Async(fileName, cancellationToken); }
                catch (Exception ex) { _logger.LogWarning(ex, "Could not embed background image {File}", fileName); }
            }
        }

        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\" />");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        sb.Append("  <title>Invoice ").Append(Enc(invoiceNumber)).AppendLine("</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    * { margin: 0; padding: 0; box-sizing: border-box; }");
        sb.AppendLine("    html, body { font-family: Arial, 'Helvetica Neue', Helvetica, sans-serif; }");
        sb.AppendLine("    body { background: #cbd5e1; display: flex; flex-direction: column; align-items: center; justify-content: flex-start; padding: 24px; min-height: 100vh; }");
        sb.AppendLine("    .print-btn { position: fixed; top: 16px; right: 16px; background: #0f172a; color: white; border: none; border-radius: 6px; padding: 8px 18px; font-size: 13px; font-weight: 600; cursor: pointer; z-index: 9999; box-shadow: 0 2px 8px rgba(0,0,0,.3); }");
        sb.AppendLine("    .print-btn:hover { background: #1e293b; }");

        // A4 page â€“ 210mm Ã— 297mm, with optional background image covering it fully
        sb.Append("    .page { position: relative; width: 210mm; height: 297mm; overflow: hidden; background-color: white;");
        if (!string.IsNullOrWhiteSpace(bgDataUrl))
        {
            sb.Append($" background-image: url('{bgDataUrl}'); background-size: 210mm 297mm; background-position: 0 0; background-repeat: no-repeat;");
        }
        sb.AppendLine(" box-shadow: 0 6px 32px rgba(0,0,0,.22); }");

        sb.AppendLine("    .field { position: absolute; overflow: hidden; line-height: 1.35; white-space: pre-wrap; word-wrap: break-word; }");
        sb.AppendLine("    .field-table { overflow: visible; white-space: normal; height: auto !important; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; }");
        sb.AppendLine("    thead th { padding: 3px 5px; font-weight: 600; border-bottom: 1.5px solid currentColor; vertical-align: bottom; }");
        sb.AppendLine("    tbody td { padding: 2px 5px; border-bottom: 1px solid rgba(0,0,0,.08); vertical-align: top; }");
        sb.AppendLine("    tbody tr:last-child td { border-bottom: none; }");
        sb.AppendLine("    @media print { body { background: none; padding: 0; margin: 0; } .print-btn { display: none; } .page { box-shadow: none; margin: 0; } @page { size: A4 portrait; margin: 0; } }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <button class=\"print-btn\" onclick=\"window.print()\">Print / Save PDF</button>");
        sb.AppendLine("  <div class=\"page\">");

        if (layout.Fields.Count > 0)
        {
            foreach (var field in layout.Fields)
            {
                var cssClass = field.Type.Equals("Table", StringComparison.OrdinalIgnoreCase)
                    ? "field field-table"
                    : "field";

                var style = BuildFieldStyle(field);
                var content = BuildFieldContent(field, invoiceNumber, customerName, customerAddress,
                    customerEmail, issueDate, dueDate, currency,
                    subtotal, discountTotal, taxTotal, grandTotal, lineItems);

                sb.Append("    <div class=\"").Append(cssClass).Append("\" style=\"").Append(style).AppendLine("\">");
                sb.AppendLine(content);
                sb.AppendLine("    </div>");
            }
        }
        else
        {
            // Fallback: no layout configured â€” render a basic A4 invoice
            AppendFallbackLayout(sb, invoiceNumber, customerName, customerAddress, customerEmail,
                issueDate, dueDate, currency, subtotal, discountTotal, taxTotal, grandTotal, lineItems,
                invoice?.LineItemColumns);
        }

        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    // â”€â”€â”€ Field rendering helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static string BuildFieldStyle(TemplateField f)
    {
        var isTable = f.Type.Equals("Table", StringComparison.OrdinalIgnoreCase)
                   || f.ContentSource.Equals("lineItems", StringComparison.OrdinalIgnoreCase);

        var sb = new StringBuilder();
        sb.Append($"left:{f.X:F2}mm;top:{f.Y:F2}mm;width:{f.Width:F2}mm");

        // Table fields expand to fit rows; all others clip at the designer height
        if (isTable)
            sb.Append($";min-height:{f.Height:F2}mm");
        else
            sb.Append($";height:{f.Height:F2}mm");

        if (f.FontSize > 0) sb.Append($";font-size:{f.FontSize}pt");
        if (!string.IsNullOrWhiteSpace(f.TextColor)) sb.Append($";color:{f.TextColor}");
        if (!string.IsNullOrWhiteSpace(f.BackgroundColor)) sb.Append($";background-color:{f.BackgroundColor}");
        if (f.BorderSize > 0) sb.Append($";border:{f.BorderSize}px solid {(string.IsNullOrWhiteSpace(f.TextColor) ? "#000" : f.TextColor)}");
        var fs = f.FontStyle?.ToLowerInvariant() ?? string.Empty;
        if (fs.Contains("bold")) sb.Append(";font-weight:bold");
        if (fs.Contains("italic")) sb.Append(";font-style:italic");
        return sb.ToString();
    }

    private static string BuildFieldContent(
        TemplateField field,
        string invoiceNumber, string customerName, string customerAddress, string customerEmail,
        string issueDate, string dueDate, string currency,
        decimal subtotal, decimal discountTotal, decimal taxTotal, decimal grandTotal,
        List<InvoiceLineItem> lineItems)
    {
        var src = string.IsNullOrWhiteSpace(field.ContentSource)
            ? field.Name?.ToLowerInvariant() ?? string.Empty
            : field.ContentSource.ToLowerInvariant();

        if (src is "invoicenumber" or "invoiceno" or "number")
            return $"<span>{Enc(invoiceNumber)}</span>";

        if (src is "issuedate" or "date")
            return $"<span>{Enc(issueDate)}</span>";

        if (src is "duedate")
            return $"<span>{Enc(dueDate)}</span>";

        if (src is "customername" or "name" or "companyname" or "billingname")
            return $"<span>{Enc(customerName)}</span>";

        if (src is "customeraddress" or "address" or "billingaddress")
            return $"<pre style=\"white-space:pre-wrap;font-family:inherit;font-size:inherit\">{Enc(customerAddress)}</pre>";

        if (src is "customeremail" or "email")
            return $"<span>{Enc(customerEmail)}</span>";

        if (src is "grandtotal" or "total")
            return $"<span>{Enc(currency)} {grandTotal:F2}</span>";

        if (src is "subtotal")
            return $"<span>{Enc(currency)} {subtotal:F2}</span>";

        if (src is "lineitems" || field.Type.Equals("Table", StringComparison.OrdinalIgnoreCase))
            return BuildTableHtml(field, lineItems, issueDate, currency);

        if (src is "custom" || !string.IsNullOrWhiteSpace(field.CustomText))
            return $"<span>{Enc(field.CustomText ?? string.Empty)}</span>";

        return string.Empty;
    }

    private static string BuildTableHtml(TemplateField field, List<InvoiceLineItem> lineItems, string issueDate, string currency)
    {
        // Resolve columns: prefer field's configured columns, fall back to defaults
        List<(string Key, string Type, string Label, decimal WidthMm)> columns;
        if (field.LineItemColumns.Count > 0)
        {
            columns = field.LineItemColumns
                .Select(c => (c.Key, c.Type, string.IsNullOrWhiteSpace(c.Label) ? ToDisplayLabel(c.Key) : c.Label, c.WidthMm))
                .ToList();
        }
        else
        {
            columns = ParseLineItemColumns(null)
                .Select(c => (c.Name, c.Type, ToDisplayLabel(c.Name), 0m))
                .ToList();
        }

        var totalWidth = columns.Sum(c => c.WidthMm);
        var hasWidths = totalWidth > 0;

        var fontSize = field.FontSize > 0 ? field.FontSize : 10;
        var textColor = !string.IsNullOrWhiteSpace(field.TextColor) ? field.TextColor : "inherit";
        var headerBg = !string.IsNullOrWhiteSpace(field.BackgroundColor) ? field.BackgroundColor : "rgba(0,0,0,.05)";

        var sb = new StringBuilder();
        sb.AppendLine($"<table style=\"font-size:{fontSize}pt;color:{textColor}\">");
        sb.AppendLine("<thead><tr>");

        foreach (var (key, type, label, widthMm) in columns)
        {
            var isRight = IsRightAlignedColumn(key, type);
            var wStyle = hasWidths ? $"width:{Math.Round(widthMm / totalWidth * 100m, 1)}%;" : string.Empty;
            var aStyle = isRight ? "text-align:right;" : string.Empty;
            sb.AppendLine($"<th style=\"background:{headerBg};{wStyle}{aStyle}\">{Enc(label)}</th>");
        }

        sb.AppendLine("</tr></thead><tbody>");

        foreach (var (li, idx) in lineItems.Select((li, i) => (li, i)))
        {
            sb.AppendLine("<tr>");
            foreach (var (key, type, _, _) in columns)
            {
                var isRight = IsRightAlignedColumn(key, type);
                var aStyle = isRight ? " style=\"text-align:right\"" : string.Empty;
                var value = BuildLineItemCell((key, type), li, idx, issueDate, currency);
                sb.AppendLine($"<td{aStyle}>{Enc(value)}</td>");
            }
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    // â”€â”€â”€ Fallback layout (no designer fields configured) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void AppendFallbackLayout(
        StringBuilder sb,
        string invoiceNumber, string customerName, string customerAddress, string customerEmail,
        string issueDate, string dueDate, string currency,
        decimal subtotal, decimal discountTotal, decimal taxTotal, decimal grandTotal,
        List<InvoiceLineItem> lineItems, string? lineItemColumnsCsv)
    {
        var columns = ParseLineItemColumns(lineItemColumnsCsv);
        var headerBg = "rgba(0,0,0,.06)";

        sb.AppendLine("    <div style=\"padding:12mm 14mm 0;\">");

        // Invoice number + date header row
        sb.AppendLine("      <div style=\"display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:8mm;\">");
        sb.AppendLine("        <div>");
        sb.AppendLine($"          <div style=\"font-size:22pt;font-weight:bold;color:#0f172a\">{Enc(invoiceNumber)}</div>");
        sb.AppendLine($"          <div style=\"font-size:9pt;color:#64748b;margin-top:2mm\">Invoice</div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div style=\"text-align:right;font-size:9pt;color:#475569\">");
        sb.AppendLine($"          <div>Issue Date: <strong>{Enc(issueDate)}</strong></div>");
        if (!string.IsNullOrWhiteSpace(dueDate))
            sb.AppendLine($"          <div>Due: <strong>{Enc(dueDate)}</strong></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");

        // Customer info
        sb.AppendLine("      <div style=\"margin-bottom:8mm;font-size:10pt;\">");
        sb.AppendLine($"        <div style=\"font-weight:bold\">{Enc(customerName)}</div>");
        if (!string.IsNullOrWhiteSpace(customerAddress))
            sb.AppendLine($"        <div style=\"white-space:pre-wrap;color:#475569\">{Enc(customerAddress)}</div>");
        if (!string.IsNullOrWhiteSpace(customerEmail))
            sb.AppendLine($"        <div style=\"color:#475569\">{Enc(customerEmail)}</div>");
        sb.AppendLine("      </div>");

        // Line items table
        sb.AppendLine("      <table style=\"width:100%;border-collapse:collapse;font-size:9pt;margin-bottom:6mm\">");
        sb.AppendLine($"      <thead><tr style=\"background:{headerBg}\">");
        foreach (var (name, type) in columns)
        {
            var isRight = IsRightAlignedColumn(name, type);
            var a = isRight ? ";text-align:right" : string.Empty;
            sb.AppendLine($"        <th style=\"padding:3px 6px;border-bottom:1.5px solid #cbd5e1{a}\">{Enc(ToDisplayLabel(name))}</th>");
        }
        sb.AppendLine("      </tr></thead><tbody>");

        foreach (var (li, idx) in lineItems.Select((li, i) => (li, i)))
        {
            sb.AppendLine("      <tr>");
            foreach (var (name, type) in columns)
            {
                var isRight = IsRightAlignedColumn(name, type);
                var a = isRight ? " style=\"text-align:right\"" : string.Empty;
                var val = BuildLineItemCell((name, type), li, idx, issueDate, currency);
                sb.AppendLine($"        <td{a} style=\"padding:2px 6px;border-bottom:1px solid #e2e8f0\">{Enc(val)}</td>");
            }
            sb.AppendLine("      </tr>");
        }
        sb.AppendLine("      </tbody></table>");

        // Totals
        sb.AppendLine("      <div style=\"display:flex;justify-content:flex-end\">");
        sb.AppendLine("        <div style=\"width:50mm;font-size:9pt\">");
        sb.AppendLine($"          <div style=\"display:flex;justify-content:space-between\"><span>Subtotal</span><span>{Enc(currency)} {subtotal:F2}</span></div>");
        if (discountTotal > 0)
            sb.AppendLine($"          <div style=\"display:flex;justify-content:space-between\"><span>Discount</span><span>- {Enc(currency)} {discountTotal:F2}</span></div>");
        if (taxTotal > 0)
            sb.AppendLine($"          <div style=\"display:flex;justify-content:space-between\"><span>Tax</span><span>{Enc(currency)} {taxTotal:F2}</span></div>");
        sb.AppendLine($"          <div style=\"display:flex;justify-content:space-between;font-weight:bold;border-top:1.5px solid #0f172a;margin-top:2mm;padding-top:2mm;font-size:11pt\"><span>Total</span><span>{Enc(currency)} {grandTotal:F2}</span></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");

        sb.AppendLine("    </div>");
    }

    // â”€â”€â”€ Column / cell helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static bool IsRightAlignedColumn(string columnName, string columnType) =>
        columnType.Equals("number", StringComparison.OrdinalIgnoreCase)
        || columnName.Equals("total", StringComparison.OrdinalIgnoreCase);

    private static string BuildLineItemCell((string Name, string Type) column, InvoiceLineItem lineItem, int index, string issueDate, string currency)
    {
        var name = column.Name;
        if (name.Equals("description", StringComparison.OrdinalIgnoreCase)) return lineItem.Description;
        if (name.Equals("total", StringComparison.OrdinalIgnoreCase)) return $"{currency} {lineItem.Total:F2}";
        if (name.Equals("itemNo", StringComparison.OrdinalIgnoreCase) || name.Equals("no", StringComparison.OrdinalIgnoreCase)) return (index + 1).ToString();
        if (name.Equals("date", StringComparison.OrdinalIgnoreCase)) return issueDate;

        var others = ParseOtherColumns(lineItem.OtherColumns);
        return others.TryGetValue(name, out var val) ? val : string.Empty;
    }

    private static IReadOnlyList<(string Name, string Type)> ParseLineItemColumns(string? columnsCsv)
    {
        var columns = new List<(string, string)>();
        if (!string.IsNullOrWhiteSpace(columnsCsv))
        {
            foreach (var segment in columnsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = segment.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                    columns.Add((parts[0].Trim(), parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : "text"));
            }
        }

        if (!columns.Any(c => c.Item1.Equals("description", StringComparison.OrdinalIgnoreCase)))
            columns.Insert(0, ("description", "text"));
        if (!columns.Any(c => c.Item1.Equals("total", StringComparison.OrdinalIgnoreCase)))
            columns.Add(("total", "number"));

        return columns;
    }

    private static Dictionary<string, string> ParseOtherColumns(string? otherColumns)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(otherColumns)) return result;

        foreach (var segment in otherColumns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = segment.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[0]))
                result[parts[0].Trim()] = parts[1].Trim();
        }
        return result;
    }

    private static string ToDisplayLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var s = System.Text.RegularExpressions.Regex.Replace(
            value.Replace("_", " ").Replace("-", " "), "([a-z])([A-Z])", "$1 $2");
        return char.ToUpperInvariant(s[0]) + s[1..];
    }

    private static string Enc(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    // â”€â”€â”€ Domain model classes â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
        public int FontSize { get; set; }
        public string FontStyle { get; set; } = string.Empty;
        public string TextColor { get; set; } = string.Empty;
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
}

