namespace MyWorkbench.Modules.Invoices.Domain;

public class InvoiceTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ActiveVersionId { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public InvoiceTemplateVersion? ActiveVersion { get; set; }
    public ICollection<InvoiceTemplateVersion> Versions { get; set; } = new List<InvoiceTemplateVersion>();
}
