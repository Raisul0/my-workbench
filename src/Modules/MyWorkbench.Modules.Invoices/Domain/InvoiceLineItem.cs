namespace MyWorkbench.Modules.Invoices.Domain;

public class InvoiceLineItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public int SortOrder { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string OtherColumns { get; set; } = string.Empty;

    public Invoice? Invoice { get; set; }
}
