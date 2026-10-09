namespace MyWorkbench.Modules.Invoices.Contracts;

public sealed class CreateInvoiceDraftRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerRegistrationNumber { get; set; } = string.Empty;
    public string CustomerTaxNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Terms { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? DueDate { get; set; }
    public List<CreateInvoiceLineItemRequest> LineItems { get; set; } = new();
}

public sealed class CreateInvoiceLineItemRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxRate { get; set; }
}
