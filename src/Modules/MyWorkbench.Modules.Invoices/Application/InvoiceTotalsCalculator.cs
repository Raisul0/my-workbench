namespace MyWorkbench.Modules.Invoices.Application;

using MyWorkbench.Modules.Invoices.Domain;

public sealed record InvoiceTotals(decimal Subtotal, decimal DiscountTotal, decimal TaxTotal, decimal GrandTotal);

public static class InvoiceTotalsCalculator
{
    public static InvoiceTotals Calculate(IEnumerable<InvoiceLineItem> lineItems)
    {
        ArgumentNullException.ThrowIfNull(lineItems);

        decimal subtotal = 0m;
        decimal discountTotal = 0m;
        decimal taxTotal = 0m;

        foreach (var item in lineItems)
        {
            var lineSubtotal = decimal.Round(item.Quantity * item.UnitPrice, 4, MidpointRounding.AwayFromZero);
            var discountAmount = decimal.Round(lineSubtotal * (item.DiscountPercent / 100m), 4, MidpointRounding.AwayFromZero);
            var taxableAmount = lineSubtotal - discountAmount;
            var taxAmount = decimal.Round(taxableAmount * (item.TaxRate / 100m), 4, MidpointRounding.AwayFromZero);
            var lineTotal = taxableAmount + taxAmount;

            item.LineSubtotal = lineSubtotal;
            item.DiscountAmount = discountAmount;
            item.TaxAmount = taxAmount;
            item.LineTotal = lineTotal;

            subtotal += lineSubtotal;
            discountTotal += discountAmount;
            taxTotal += taxAmount;
        }

        var grandTotal = subtotal - discountTotal + taxTotal;

        return new InvoiceTotals(
            decimal.Round(subtotal, 4, MidpointRounding.AwayFromZero),
            decimal.Round(discountTotal, 4, MidpointRounding.AwayFromZero),
            decimal.Round(taxTotal, 4, MidpointRounding.AwayFromZero),
            decimal.Round(grandTotal, 4, MidpointRounding.AwayFromZero));
    }
}
