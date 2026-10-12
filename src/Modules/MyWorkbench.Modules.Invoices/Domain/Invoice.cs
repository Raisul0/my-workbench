namespace MyWorkbench.Modules.Invoices.Domain;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessProfileId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public Guid TemplateVersionId { get; set; }
    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? DueDate { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string IssuerName { get; set; } = string.Empty;
    public string IssuerAddress { get; set; } = string.Empty;
    public string IssuerEmail { get; set; } = string.Empty;
    public string IssuerPhone { get; set; } = string.Empty;
    public string IssuerRegistrationNumber { get; set; } = string.Empty;
    public string IssuerTaxNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerRegistrationNumber { get; set; } = string.Empty;
    public string CustomerTaxNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Terms { get; set; } = string.Empty;
    public string LineItemColumnsCsv { get; set; } = string.Join(',', InvoiceLineItemColumnCatalog.DefaultOrderedColumns);
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string? PdfStorageKey { get; set; }
    public string? PdfSha256 { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? GeneratedAtUtc { get; set; }
    public byte[]? RowVersion { get; set; }

    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
}
