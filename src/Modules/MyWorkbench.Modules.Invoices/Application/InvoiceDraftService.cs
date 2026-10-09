using Microsoft.EntityFrameworkCore;
using MyWorkbench.Modules.Invoices.Contracts;
using MyWorkbench.Modules.Invoices.Domain;
using MyWorkbench.Modules.Invoices.Persistence;

namespace MyWorkbench.Modules.Invoices.Application;

public class InvoiceDraftService
{
    private readonly InvoiceDbContext _dbContext;

    public InvoiceDraftService(InvoiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BusinessProfile> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.BusinessProfiles.FirstOrDefaultAsync(cancellationToken);

        if (profile is not null)
        {
            return profile;
        }

        profile = new BusinessProfile();
        _dbContext.BusinessProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return profile;
    }

    public async Task<Invoice> CreateDraftAsync(CreateInvoiceDraftRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await GetSettingsAsync(cancellationToken);

        if (request.LineItems.Count == 0)
        {
            throw new InvalidOperationException("At least one line item is required.");
        }

        var lineItems = request.LineItems
            .Select((line, index) => new InvoiceLineItem
            {
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                DiscountPercent = line.DiscountPercent,
                TaxRate = line.TaxRate,
                SortOrder = index,
                InvoiceId = Guid.Empty
            })
            .ToList();

        var totals = InvoiceTotalsCalculator.Calculate(lineItems);
        var invoice = new Invoice
        {
            InvoiceNumber = InvoiceNumberAllocator.NextNumber(profile),
            Status = InvoiceStatus.Draft,
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? profile.DefaultCurrencyCode : request.CurrencyCode,
            IssuerName = profile.IssuerName,
            IssuerAddress = profile.IssuerAddress,
            IssuerEmail = profile.IssuerEmail,
            IssuerPhone = profile.IssuerPhone,
            IssuerRegistrationNumber = profile.IssuerRegistrationNumber,
            IssuerTaxNumber = profile.IssuerTaxNumber,
            CustomerName = request.CustomerName,
            CustomerAddress = request.CustomerAddress,
            CustomerEmail = request.CustomerEmail,
            CustomerPhone = request.CustomerPhone,
            CustomerRegistrationNumber = request.CustomerRegistrationNumber,
            CustomerTaxNumber = request.CustomerTaxNumber,
            Notes = request.Notes,
            Terms = string.IsNullOrWhiteSpace(request.Terms) ? profile.DefaultTerms : request.Terms,
            IssueDate = request.IssueDate,
            DueDate = request.DueDate,
            Subtotal = totals.Subtotal,
            DiscountTotal = totals.DiscountTotal,
            TaxTotal = totals.TaxTotal,
            GrandTotal = totals.GrandTotal,
            LineItems = lineItems
        };

        foreach (var lineItem in invoice.LineItems)
        {
            lineItem.Invoice = invoice;
        }

        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return invoice;
    }

    public async Task<IReadOnlyList<Invoice>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .AsNoTracking()
            .Include(x => x.LineItems)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
