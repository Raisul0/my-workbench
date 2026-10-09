using Microsoft.Extensions.Logging;

namespace MyWorkbench.Modules.Invoices.Application;

internal static class InvoiceServiceLog
{
    public static IDisposable? BeginOperationScope(ILogger logger, string service, string operation, Guid? entityId = null)
    {
        return logger.BeginScope(new Dictionary<string, object?>
        {
            ["Service"] = service,
            ["Operation"] = operation,
            ["EntityId"] = entityId
        });
    }

    public static void Started(ILogger logger, string operation, Guid? entityId = null)
    {
        logger.LogInformation("Starting {Operation} for {EntityId}", operation, entityId);
    }

    public static void Succeeded(ILogger logger, string operation, Guid? entityId = null)
    {
        logger.LogInformation("Completed {Operation} for {EntityId}", operation, entityId);
    }

    public static void Failed(ILogger logger, Exception exception, string operation, Guid? entityId = null)
    {
        logger.LogError(exception, "Failed {Operation} for {EntityId}", operation, entityId);
    }
}