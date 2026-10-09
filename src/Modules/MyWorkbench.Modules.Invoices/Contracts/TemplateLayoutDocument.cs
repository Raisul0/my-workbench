namespace MyWorkbench.Modules.Invoices.Contracts;

public sealed class TemplateLayoutDocument
{
    public int SchemaVersion { get; set; } = 1;
    public string Name { get; set; } = string.Empty;
    public List<TemplateFieldBinding> Fields { get; set; } = new();
}
