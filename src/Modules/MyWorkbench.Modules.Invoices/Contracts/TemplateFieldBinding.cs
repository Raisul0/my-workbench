namespace MyWorkbench.Modules.Invoices.Contracts;

public sealed class TemplateFieldBinding
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Text";
    public decimal? X { get; set; }
    public decimal? Y { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
}
