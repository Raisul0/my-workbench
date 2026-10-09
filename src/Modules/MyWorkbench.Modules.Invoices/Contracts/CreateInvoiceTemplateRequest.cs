namespace MyWorkbench.Modules.Invoices.Contracts;

using MyWorkbench.Modules.Invoices.Domain;

public sealed class CreateInvoiceTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TemplateSourceType SourceType { get; set; }
    public string SourceStorageKey { get; set; } = string.Empty;
    public int? SelectedPdfPageNumber { get; set; }
    public decimal PageWidthPoints { get; set; }
    public decimal PageHeightPoints { get; set; }
    public int LayoutSchemaVersion { get; set; } = 1;
    public string LayoutJson { get; set; } = "{}";
}
