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

    public static readonly IReadOnlyDictionary<string, string> ColumnTypes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ItemNo] = "number",
        [Date] = "date",
        [Client] = "text",
        [Project] = "text",
        [Description] = "text",
        [Hours] = "number",
        [Rate] = "number",
        [Total] = "number"
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
            // Extract column name from "columnName:type" format
            var columnName = ExtractColumnName(segment);

            if (!TryNormalize(columnName, out var normalized) || !seen.Add(normalized))
            {
                continue;
            }

            result.Add(normalized);
        }

        return result.Count == 0 ? DefaultOrderedColumns : result;
    }

    public static string Serialize(IReadOnlyCollection<string> columns)
    {
        if (columns.Count == 0)
        {
            return SerializeWithTypes(DefaultOrderedColumns);
        }

        return SerializeWithTypes(columns);
    }

    /// <summary>
    /// Serializes columns with their type information in the format: "columnName:columnType"
    /// </summary>
    public static string SerializeWithTypes(IReadOnlyCollection<string> columns)
    {
        var parts = new List<string>(columns.Count);
        foreach (var column in columns)
        {
            var type = ColumnTypes.TryGetValue(column, out var columnType) ? columnType : "text";
            parts.Add($"{column}:{type}");
        }
        return string.Join(',', parts);
    }

    /// <summary>
    /// Extracts the column name from a "columnName:columnType" format, or returns the input if no type is specified.
    /// </summary>
    public static string ExtractColumnName(string columnWithType)
    {
        if (string.IsNullOrWhiteSpace(columnWithType))
        {
            return string.Empty;
        }

        var colonIndex = columnWithType.IndexOf(':');
        return colonIndex > 0 ? columnWithType.Substring(0, colonIndex).Trim() : columnWithType.Trim();
    }

    /// <summary>
    /// Extracts the type from a "columnName:columnType" format, or returns the default type for the column if not specified.
    /// </summary>
    public static string ExtractColumnType(string columnWithType)
    {
        if (string.IsNullOrWhiteSpace(columnWithType))
        {
            return "text";
        }

        var colonIndex = columnWithType.IndexOf(':');
        if (colonIndex > 0 && colonIndex < columnWithType.Length - 1)
        {
            return columnWithType.Substring(colonIndex + 1).Trim();
        }

        // If no type specified, return the default for this column
        var columnName = colonIndex > 0 ? columnWithType.Substring(0, colonIndex).Trim() : columnWithType.Trim();
        return ColumnTypes.TryGetValue(columnName, out var defaultType) ? defaultType : "text";
    }

    private static string NormalizeKey(string value)
    {
        var chars = value.Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();

        return new string(chars);
    }
}
