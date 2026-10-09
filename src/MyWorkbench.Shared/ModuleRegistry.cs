namespace MyWorkbench.Shared;

public static class ModuleRegistry
{
    public static IReadOnlyList<ModuleDescriptor> FilterEnabled(IEnumerable<ModuleDescriptor> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);

        return modules
            .Where(m => m.Enabled && !string.IsNullOrWhiteSpace(m.Id))
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
