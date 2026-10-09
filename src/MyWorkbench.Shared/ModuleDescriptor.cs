namespace MyWorkbench.Shared;

public sealed record ModuleDescriptor(
    string Id,
    string DisplayName,
    string Description,
    string Icon,
    string RoutePath,
    string ApiBasePath,
    bool Enabled,
    int SortOrder
    );
