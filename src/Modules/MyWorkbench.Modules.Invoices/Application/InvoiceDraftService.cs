using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyWorkbench.Modules.Invoices.Contracts;
using MyWorkbench.Modules.Invoices.Domain;
using MyWorkbench.Modules.Invoices.Persistence;

namespace MyWorkbench.Modules.Invoices.Application;

public class InvoiceDraftService
{
    private readonly InvoiceDbContext _dbContext;
    private readonly ILogger<InvoiceDraftService> _logger;

    public InvoiceDraftService(InvoiceDbContext dbContext, ILogger<InvoiceDraftService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<BusinessProfile> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceDraftService), nameof(GetSettingsAsync));
        InvoiceServiceLog.Started(_logger, nameof(GetSettingsAsync));

        try
        {
            var profile = await ResolveBusinessProfileAsync(null, cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(GetSettingsAsync), profile.Id);
            return profile;
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(GetSettingsAsync));
            throw;
        }
    }

    public async Task<Invoice> CreateDraftAsync(CreateInvoiceDraftRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceDraftService), nameof(CreateDraftAsync));
        InvoiceServiceLog.Started(_logger, nameof(CreateDraftAsync));

        try
        {
            var profile = await ResolveBusinessProfileAsync(request.BusinessProfileId, cancellationToken);

            if (request.LineItems.Count == 0)
            {
                throw new InvalidOperationException("At least one line item is required.");
            }

            var orderedColumns = ResolveLineItemColumns(request.LineItemColumns ?? [], profile);
            var columnsCsv = InvoiceLineItemColumnCatalog.Serialize(orderedColumns);

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
                BusinessProfileId = profile.Id,
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
                LineItemColumnsCsv = columnsCsv,
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
            InvoiceServiceLog.Succeeded(_logger, nameof(CreateDraftAsync), invoice.Id);

            return invoice;
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(CreateDraftAsync));
            throw;
        }
    }

    public async Task<IReadOnlyList<Invoice>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceDraftService), nameof(ListAsync));
        InvoiceServiceLog.Started(_logger, nameof(ListAsync));

        try
        {
            var result = await _dbContext.Invoices
                .AsNoTracking()
                .Include(x => x.LineItems)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync(cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(ListAsync));
            return result;
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(ListAsync));
            throw;
        }
    }

    private async Task<BusinessProfile> ResolveBusinessProfileAsync(Guid? businessProfileId, CancellationToken cancellationToken)
    {
        if (businessProfileId.HasValue)
        {
            var selected = await _dbContext.BusinessProfiles.SingleOrDefaultAsync(x => x.Id == businessProfileId.Value, cancellationToken);
            if (selected is null)
            {
                throw new InvalidOperationException("The selected business profile was not found.");
            }

            return selected;
        }

        var defaultProfile = await _dbContext.BusinessProfiles
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultProfile is not null)
        {
            return defaultProfile;
        }

        defaultProfile = new BusinessProfile();
        _dbContext.BusinessProfiles.Add(defaultProfile);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return defaultProfile;
    }

    private IReadOnlyList<string> ResolveLineItemColumns(IReadOnlyCollection<string> requestedColumns, BusinessProfile profile)
    {
        if (requestedColumns.Count == 0)
        {
            return InvoiceLineItemColumnCatalog.ParseOrDefault(profile.LineItemColumnsCsv);
        }

        var normalizedColumns = new List<string>(requestedColumns.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var requested in requestedColumns)
        {
            if (!InvoiceLineItemColumnCatalog.TryNormalize(requested, out var normalized))
            {
                throw new InvalidOperationException($"Unsupported line item column '{requested}'.");
            }

            if (!seen.Add(normalized))
            {
                throw new InvalidOperationException($"Line item column '{requested}' is duplicated.");
            }

            normalizedColumns.Add(normalized);
        }

        profile.LineItemColumnsCsv = InvoiceLineItemColumnCatalog.Serialize(normalizedColumns);
        profile.UpdatedAtUtc = DateTimeOffset.UtcNow;

        return normalizedColumns;
    }
}
