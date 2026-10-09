namespace MyWorkbench.Modules.Invoices.Contracts;

public sealed class TemplateUploadValidator
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".png",
        ".jpg",
        ".jpeg"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg"
    };

    public bool IsAllowedSafeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var name = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(name) || !string.Equals(name, fileName, StringComparison.Ordinal))
        {
            return false;
        }

        if (name.StartsWith(".", StringComparison.Ordinal))
        {
            return false;
        }

        var extension = Path.GetExtension(name);
        if (!AllowedExtensions.Contains(extension))
        {
            return false;
        }

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            if (name.IndexOf(invalidCharacter, StringComparison.Ordinal) >= 0)
            {
                return false;
            }
        }

        return true;
    }

    public bool IsSupportedContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return AllowedContentTypes.Contains(contentType.Trim());
    }
}
