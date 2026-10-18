namespace MyWorkbench.Modules.Invoices.Application;

using System.Threading;
using MyWorkbench.Modules.Invoices.Domain;

public static class InvoiceNumberAllocator
{
    public static string NextNumber(CustomerProfile settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var prefix = string.IsNullOrWhiteSpace(settings.InvoicePrefix) ? "INV" : settings.InvoicePrefix.Trim();
        var sequence = settings.NextSequence + 1;

        settings.NextSequence = sequence;
        return $"{prefix}-{sequence}";
    }
}
