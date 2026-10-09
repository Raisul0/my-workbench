namespace MyWorkbench.Modules.Invoices.Domain;

public class InvoiceTemplateVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TemplateId { get; set; }
    public int VersionNumber { get; set; } = 1;
    public TemplateSourceType SourceType { get; set; }
    public string SourceStorageKey { get; set; } = string.Empty;
    public int? SelectedPdfPageNumber { get; set; }
    public decimal PageWidthPoints { get; set; }
    public decimal PageHeightPoints { get; set; }
    public int LayoutSchemaVersion { get; set; } = 1;
    public string LayoutJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public InvoiceTemplate Template { get; set; } = null!;
}
