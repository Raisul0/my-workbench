namespace MyWorkbench.Modules.Invoices.Domain;

public static class InvoiceLineItemColumnCatalog
{
    public const string ItemNo = "itemNo";
    public const string Date = "date";
    public const string Client = "client";
    public const string Project = "project";
    public const string Description = "description";
    public const string Hours = "hours";
    public const string Rate = "rate";
    public const string Total = "total";

    public static readonly IReadOnlyList<string> DefaultOrderedColumns =
    [
        ItemNo,
        Date,
        Client,
        Project,
        Description,
        Hours,
        Rate,
        Total
    ];

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ItemNo] = "Item No",
        [Date] = "Date",
        [Client] = "Client",
        [Project] = "Project",
        [Description] = "Description",
        [Hours] = "Hours",
        [Rate] = "Rate",
        [Total] = "Total"
    };

    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var key = NormalizeKey(value);
        normalized = key switch
        {
            "itemno" or "itemnumber" or "temno" => ItemNo,
            "date" => Date,
            "client" => Client,
            "project" => Project,
            "description" => Description,
            "hours" => Hours,
            "rate" => Rate,
            "total" => Total,
            _ => string.Empty
        };

        return normalized.Length > 0;
    }

    public static IReadOnlyList<string> ParseOrDefault(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return DefaultOrderedColumns;
        }

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var segment in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryNormalize(segment, out var normalized) || !seen.Add(normalized))
            {
                continue;
            }

            result.Add(normalized);
        }

        return result.Count == 0 ? DefaultOrderedColumns : result;
    }

    public static string Serialize(IReadOnlyCollection<string> columns)
    {
        return columns.Count == 0
            ? string.Join(',', DefaultOrderedColumns)
            : string.Join(',', columns);
    }

    private static string NormalizeKey(string value)
    {
        var chars = value.Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();

        return new string(chars);
    }
}
