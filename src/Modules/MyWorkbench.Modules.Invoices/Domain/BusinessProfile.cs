namespace MyWorkbench.Modules.Invoices.Domain;

public class BusinessProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IssuerName { get; set; } = "My Business";
    public string IssuerAddress { get; set; } = string.Empty;
    public string IssuerEmail { get; set; } = string.Empty;
    public string IssuerPhone { get; set; } = string.Empty;
    public string IssuerRegistrationNumber { get; set; } = string.Empty;
    public string IssuerTaxNumber { get; set; } = string.Empty;
    public string TaxLabel { get; set; } = "Tax";
    public string DefaultCurrencyCode { get; set; } = "USD";
    public string InvoicePrefix { get; set; } = "INV";
    public int NextSequence { get; set; } = 100;
    public decimal DefaultTaxRate { get; set; } = 0m;
    public string DefaultTerms { get; set; } = "Payment due within 14 days.";
    public string LineItemColumnsCsv { get; set; } = string.Join(',', InvoiceLineItemColumnCatalog.DefaultOrderedColumns);
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
