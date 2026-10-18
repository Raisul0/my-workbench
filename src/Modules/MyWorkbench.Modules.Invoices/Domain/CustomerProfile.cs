namespace MyWorkbench.Modules.Invoices.Domain;

public class CustomerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerName { get; set; } = "Customer";
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerRegistrationNumber { get; set; } = string.Empty;
    public string CustomerTaxNumber { get; set; } = string.Empty;
    public string TaxLabel { get; set; } = "Tax";
    public string DefaultCurrencyCode { get; set; } = "USD";
    public string InvoicePrefix { get; set; } = "INV";
    public int NextSequence { get; set; } = 100;
    public decimal DefaultTaxRate { get; set; } = 0m;
    public string DefaultTerms { get; set; } = "Payment due within 14 days.";
    public string LineItemColumns { get; set; } = "description:text,total:number";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
