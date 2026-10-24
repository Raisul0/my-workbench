namespace MyWorkbench.Modules.Invoices.Contracts;

public sealed class TemplateFieldBinding
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Text";
    public decimal? X { get; set; }
    public decimal? Y { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? ContentSource { get; set; }
    public string? CustomText { get; set; }
    public int? FontSize { get; set; }
    public string? FontStyle { get; set; }
    public string? TextColor { get; set; }
    public string? BackgroundColor { get; set; }
    public int? BorderSize { get; set; }
    public List<LineItemColumnConfig>? LineItemColumns { get; set; }
}
