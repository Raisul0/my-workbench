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
            var columnsCsv = orderedColumns.Count == 0
                ? profile.LineItemColumns
                : string.Join(',', orderedColumns);

            var lineItems = request.LineItems
                .Select((line, index) => new InvoiceLineItem
                {
                    Description = line.Description,
                    Total = line.Total,
                    OtherColumns = line.OtherColumns?.Trim() ?? string.Empty,
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
                Notes = request.Notes,
                Terms = string.IsNullOrWhiteSpace(request.Terms) ? profile.DefaultTerms : request.Terms,
                LineItemColumns = columnsCsv,
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
                .Include(x => x.BusinessProfile)
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

    public async Task DeleteDraftAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceDraftService), nameof(DeleteDraftAsync));
        InvoiceServiceLog.Started(_logger, nameof(DeleteDraftAsync));

        try
        {
            var invoice = await _dbContext.Invoices.SingleOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
            if (invoice is null)
            {
                throw new InvalidOperationException("Invoice not found.");
            }

            if (invoice.Status != InvoiceStatus.Draft)
            {
                throw new InvalidOperationException("Only draft invoices can be deleted.");
            }

            _dbContext.Invoices.Remove(invoice);
            await _dbContext.SaveChangesAsync(cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(DeleteDraftAsync), invoiceId);
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(DeleteDraftAsync));
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

    public async Task<BusinessProfile> CreateOrUpdateProfileAsync(Guid? profileId, BusinessProfile profileData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profileData);

        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceDraftService), nameof(CreateOrUpdateProfileAsync));
        InvoiceServiceLog.Started(_logger, nameof(CreateOrUpdateProfileAsync));

        try
        {
            BusinessProfile profile;

            if (profileId.HasValue)
            {
                // Update existing profile
                var existingProfile = await _dbContext.BusinessProfiles.SingleOrDefaultAsync(x => x.Id == profileId.Value, cancellationToken);
                if (existingProfile is null)
                {
                    throw new InvalidOperationException("Business profile not found.");
                }
                profile = existingProfile;

                profile.IssuerName = profileData.IssuerName;
                profile.IssuerAddress = profileData.IssuerAddress;
                profile.IssuerEmail = profileData.IssuerEmail;
                profile.IssuerPhone = profileData.IssuerPhone;
                profile.IssuerRegistrationNumber = profileData.IssuerRegistrationNumber;
                profile.IssuerTaxNumber = profileData.IssuerTaxNumber;
                profile.TaxLabel = profileData.TaxLabel;
                profile.DefaultCurrencyCode = profileData.DefaultCurrencyCode;
                profile.InvoicePrefix = profileData.InvoicePrefix;
                profile.DefaultTaxRate = profileData.DefaultTaxRate;
                profile.DefaultTerms = profileData.DefaultTerms;
                profile.UpdatedAtUtc = DateTimeOffset.UtcNow;
                profile.LineItemColumns = profileData.LineItemColumns;

                _dbContext.BusinessProfiles.Update(profile);
            }
            else
            {
                // Create new profile
                profile = new BusinessProfile
                {
                    Id = Guid.NewGuid(),
                    IssuerName = profileData.IssuerName,
                    IssuerAddress = profileData.IssuerAddress,
                    IssuerEmail = profileData.IssuerEmail,
                    IssuerPhone = profileData.IssuerPhone,
                    IssuerRegistrationNumber = profileData.IssuerRegistrationNumber,
                    IssuerTaxNumber = profileData.IssuerTaxNumber,
                    TaxLabel = profileData.TaxLabel,
                    DefaultCurrencyCode = profileData.DefaultCurrencyCode,
                    InvoicePrefix = profileData.InvoicePrefix,
                    DefaultTaxRate = profileData.DefaultTaxRate,
                    DefaultTerms = profileData.DefaultTerms,
                    LineItemColumns = string.IsNullOrWhiteSpace(profileData.LineItemColumns)
                        ? "description:text,total:number"
                        : profileData.LineItemColumns,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                };

                _dbContext.BusinessProfiles.Add(profile);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(CreateOrUpdateProfileAsync), profile.Id);

            return profile;
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(CreateOrUpdateProfileAsync));
            throw;
        }
    }

    public async Task<BusinessProfile?> GetProfileByIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceDraftService), nameof(GetProfileByIdAsync));
        InvoiceServiceLog.Started(_logger, nameof(GetProfileByIdAsync));

        try
        {
            var profile = await _dbContext.BusinessProfiles.SingleOrDefaultAsync(x => x.Id == profileId, cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(GetProfileByIdAsync), profileId);
            return profile;
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(GetProfileByIdAsync));
            throw;
        }
    }

    public async Task<IReadOnlyList<BusinessProfile>> ListProfilesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceDraftService), nameof(ListProfilesAsync));
        InvoiceServiceLog.Started(_logger, nameof(ListProfilesAsync));

        try
        {
            var profiles = await _dbContext.BusinessProfiles
                .AsNoTracking()
                .OrderBy(x => x.CreatedAtUtc)
                .ToListAsync(cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(ListProfilesAsync));
            return profiles;
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(ListProfilesAsync));
            throw;
        }
    }

    public async Task DeleteProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceDraftService), nameof(DeleteProfileAsync));
        InvoiceServiceLog.Started(_logger, nameof(DeleteProfileAsync));

        try
        {
            var profile = await _dbContext.BusinessProfiles.SingleOrDefaultAsync(x => x.Id == profileId, cancellationToken);
            if (profile is null)
            {
                throw new InvalidOperationException("Business profile not found.");
            }

            // Check if there are any invoices associated with this profile
            var invoiceCount = await _dbContext.Invoices
                .Where(x => x.BusinessProfileId == profileId)
                .CountAsync(cancellationToken);

            if (invoiceCount > 0)
            {
                throw new InvalidOperationException($"Cannot delete this profile because it has {invoiceCount} associated invoice(s). Please delete or reassign the invoices first.");
            }

            _dbContext.BusinessProfiles.Remove(profile);
            await _dbContext.SaveChangesAsync(cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(DeleteProfileAsync), profileId);
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(DeleteProfileAsync));
            throw;
        }
    }

    private IReadOnlyList<string> ResolveLineItemColumns(IReadOnlyCollection<string> requestedColumns, BusinessProfile profile)
    {
        if (requestedColumns.Count == 0)
        {
            return profile.LineItemColumns
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(column => column.Trim())
                .Where(column => column.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        var normalizedColumns = new List<string>(requestedColumns.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var requested in requestedColumns)
        {
            var normalized = requested?.Trim() ?? string.Empty;

            if (normalized.Length == 0)
            {
                continue;
            }

            if (!seen.Add(normalized))
            {
                throw new InvalidOperationException($"Line item column '{requested}' is duplicated.");
            }

            normalizedColumns.Add(normalized);
        }

        profile.LineItemColumns = string.Join(',', normalizedColumns);
        profile.UpdatedAtUtc = DateTimeOffset.UtcNow;

        return normalizedColumns;
    }
}
