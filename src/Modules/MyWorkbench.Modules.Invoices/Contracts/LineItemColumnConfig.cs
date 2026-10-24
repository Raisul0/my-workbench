namespace MyWorkbench.Modules.Invoices.Contracts;

public sealed class LineItemColumnConfig
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal WidthMm { get; set; }
    public string Type { get; set; } = "text";
}
