namespace MyWorkbench.Modules.Invoices.Contracts;

public sealed class CreateInvoiceDraftRequest
{
    public Guid? BusinessProfileId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Terms { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? DueDate { get; set; }
    public List<string> LineItemColumns { get; set; } = new();
    public List<CreateInvoiceLineItemRequest> LineItems { get; set; } = new();
}

public sealed class CreateInvoiceLineItemRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string OtherColumns { get; set; } = string.Empty;
}
