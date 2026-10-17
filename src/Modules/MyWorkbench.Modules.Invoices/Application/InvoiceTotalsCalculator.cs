namespace MyWorkbench.Modules.Invoices.Application;

using MyWorkbench.Modules.Invoices.Domain;

public sealed record InvoiceTotals(decimal Subtotal, decimal DiscountTotal, decimal TaxTotal, decimal GrandTotal);

public static class InvoiceTotalsCalculator
{
    public static InvoiceTotals Calculate(IEnumerable<InvoiceLineItem> lineItems)
    {
        ArgumentNullException.ThrowIfNull(lineItems);

        decimal grandTotal = 0m;

        foreach (var item in lineItems)
        {
            item.Total = decimal.Round(item.Total, 4, MidpointRounding.AwayFromZero);
            grandTotal += item.Total;
        }

        grandTotal = decimal.Round(grandTotal, 4, MidpointRounding.AwayFromZero);

        return new InvoiceTotals(
            grandTotal,
            0m,
            0m,
            grandTotal);
    }
}
