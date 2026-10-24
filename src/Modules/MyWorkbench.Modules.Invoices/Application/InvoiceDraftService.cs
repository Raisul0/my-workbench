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

    public async Task<CustomerProfile> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceDraftService), nameof(GetSettingsAsync));
        InvoiceServiceLog.Started(_logger, nameof(GetSettingsAsync));

        try
        {
            var profile = await ResolveCustomerProfileAsync(null, cancellationToken);
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
            var profile = await ResolveCustomerProfileAsync(request.CustomerProfileId, cancellationToken);

            if (request.LineItems.Count == 0)
            {
                throw new InvalidOperationException("At least one line item is required.");
            }

            // Resolve template and get latest version
            var (templateId, templateVersionId) = await ResolveTemplateAsync(request.TemplateId, cancellationToken);

            var orderedColumns = ResolveLineItemColumns(request.LineItemColumns ?? [], profile);
            var columnsCsv = orderedColumns.Count == 0
                ? profile.LineItemColumns
                : InvoiceLineItemColumnCatalog.SerializeWithTypes(orderedColumns);

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
                CustomerProfileId = profile.Id,
                InvoiceNumber = InvoiceNumberAllocator.NextNumber(profile),
                Status = InvoiceStatus.Draft,
                TemplateId = templateId,
                TemplateVersionId = templateVersionId,
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
                .Include(x => x.CustomerProfile)
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

    private async Task<CustomerProfile> ResolveCustomerProfileAsync(Guid? customerProfileId, CancellationToken cancellationToken)
    {
        if (customerProfileId.HasValue)
        {
            var selected = await _dbContext.CustomerProfiles.SingleOrDefaultAsync(x => x.Id == customerProfileId.Value, cancellationToken);
            if (selected is null)
            {
                throw new InvalidOperationException("The selected customer profile was not found.");
            }

            return selected;
        }

        var defaultProfile = await _dbContext.CustomerProfiles
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultProfile is not null)
        {
            return defaultProfile;
        }

        defaultProfile = new CustomerProfile();
        _dbContext.CustomerProfiles.Add(defaultProfile);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return defaultProfile;
    }

    public async Task<CustomerProfile> CreateOrUpdateProfileAsync(Guid? profileId, CustomerProfile profileData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profileData);

        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceDraftService), nameof(CreateOrUpdateProfileAsync));
        InvoiceServiceLog.Started(_logger, nameof(CreateOrUpdateProfileAsync));

        try
        {
            CustomerProfile profile;

            if (profileId.HasValue)
            {
                // Update existing profile
                var existingProfile = await _dbContext.CustomerProfiles.SingleOrDefaultAsync(x => x.Id == profileId.Value, cancellationToken);
                if (existingProfile is null)
                {
                    throw new InvalidOperationException("Customer profile not found.");
                }
                profile = existingProfile;

                profile.CustomerName = profileData.CustomerName;
                profile.CustomerAddress = profileData.CustomerAddress;
                profile.CustomerEmail = profileData.CustomerEmail;
                profile.CustomerPhone = profileData.CustomerPhone;
                profile.CustomerRegistrationNumber = profileData.CustomerRegistrationNumber;
                profile.CustomerTaxNumber = profileData.CustomerTaxNumber;
                profile.TaxLabel = profileData.TaxLabel;
                profile.DefaultCurrencyCode = profileData.DefaultCurrencyCode;
                profile.InvoicePrefix = profileData.InvoicePrefix;
                profile.DefaultTaxRate = profileData.DefaultTaxRate;
                profile.DefaultTerms = profileData.DefaultTerms;
                profile.UpdatedAtUtc = DateTimeOffset.UtcNow;
                profile.LineItemColumns = profileData.LineItemColumns;

                _dbContext.CustomerProfiles.Update(profile);
            }
            else
            {
                // Create new profile
                profile = new CustomerProfile
                {
                    Id = Guid.NewGuid(),
                    CustomerName = profileData.CustomerName,
                    CustomerAddress = profileData.CustomerAddress,
                    CustomerEmail = profileData.CustomerEmail,
                    CustomerPhone = profileData.CustomerPhone,
                    CustomerRegistrationNumber = profileData.CustomerRegistrationNumber,
                    CustomerTaxNumber = profileData.CustomerTaxNumber,
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

                _dbContext.CustomerProfiles.Add(profile);
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

    public async Task<CustomerProfile?> GetProfileByIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceDraftService), nameof(GetProfileByIdAsync));
        InvoiceServiceLog.Started(_logger, nameof(GetProfileByIdAsync));

        try
        {
            var profile = await _dbContext.CustomerProfiles.SingleOrDefaultAsync(x => x.Id == profileId, cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(GetProfileByIdAsync), profileId);
            return profile;
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(GetProfileByIdAsync));
            throw;
        }
    }

    public async Task<IReadOnlyList<CustomerProfile>> ListProfilesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceDraftService), nameof(ListProfilesAsync));
        InvoiceServiceLog.Started(_logger, nameof(ListProfilesAsync));

        try
        {
            var profiles = await _dbContext.CustomerProfiles
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
            var profile = await _dbContext.CustomerProfiles.SingleOrDefaultAsync(x => x.Id == profileId, cancellationToken);
            if (profile is null)
            {
                throw new InvalidOperationException("Customer profile not found.");
            }

            // Check if there are any invoices associated with this profile
            var invoiceCount = await _dbContext.Invoices
                .Where(x => x.CustomerProfileId == profileId)
                .CountAsync(cancellationToken);

            if (invoiceCount > 0)
            {
                throw new InvalidOperationException($"Cannot delete this profile because it has {invoiceCount} associated invoice(s). Please delete or reassign the invoices first.");
            }

            _dbContext.CustomerProfiles.Remove(profile);
            await _dbContext.SaveChangesAsync(cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(DeleteProfileAsync), profileId);
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(DeleteProfileAsync));
            throw;
        }
    }

    private IReadOnlyList<string> ResolveLineItemColumns(IReadOnlyCollection<string> requestedColumns, CustomerProfile profile)
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

        profile.LineItemColumns = InvoiceLineItemColumnCatalog.SerializeWithTypes(normalizedColumns);
        profile.UpdatedAtUtc = DateTimeOffset.UtcNow;

        return normalizedColumns;
    }

    /// <summary>
    /// Resolves template ID and gets the latest version ID.
    /// If no template ID provided, uses the first available template.
    /// </summary>
    private async Task<(Guid TemplateId, Guid TemplateVersionId)> ResolveTemplateAsync(Guid? requestedTemplateId, CancellationToken cancellationToken)
    {
        InvoiceTemplateVersion? latestVersion;

        if (requestedTemplateId.HasValue)
        {
            // Get latest version of the specified template
            latestVersion = await _dbContext.InvoiceTemplateVersions
                .Where(x => x.TemplateId == requestedTemplateId.Value)
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestVersion == null)
            {
                throw new InvalidOperationException($"Template with ID {requestedTemplateId} not found or has no versions.");
            }

            return (requestedTemplateId.Value, latestVersion.Id);
        }

        // If no template specified, get the latest version of the first non-archived template
        latestVersion = await _dbContext.InvoiceTemplateVersions
            .Join(
                _dbContext.InvoiceTemplates.Where(t => !t.IsArchived),
                v => v.TemplateId,
                t => t.Id,
                (v, t) => v
            )
            .OrderBy(x => x.TemplateId)
            .ThenByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestVersion == null)
        {
            throw new InvalidOperationException("No active templates available. Please create a template first.");
        }

        return (latestVersion.TemplateId, latestVersion.Id);
    }
}
