namespace MyWorkbench.Modules.Invoices.Contracts;

public sealed class TemplateLayoutValidator
{
    public bool IsValid(TemplateLayoutDocument? layout)
    {
        if (layout is null)
        {
            return false;
        }

        if (layout.SchemaVersion <= 0)
        {
            return false;
        }

        if (layout.Fields is null || layout.Fields.Count == 0)
        {
            return false;
        }

        foreach (var field in layout.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Name))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(field.Type))
            {
                return false;
            }
        }

        return true;
    }
}
