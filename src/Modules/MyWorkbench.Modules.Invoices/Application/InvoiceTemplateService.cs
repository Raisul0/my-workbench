using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyWorkbench.Modules.Invoices.Contracts;
using MyWorkbench.Modules.Invoices.Domain;
using MyWorkbench.Modules.Invoices.Persistence;

namespace MyWorkbench.Modules.Invoices.Application;

public class InvoiceTemplateService
{
    private readonly InvoiceDbContext _dbContext;
    private readonly ILogger<InvoiceTemplateService> _logger;

    public InvoiceTemplateService(InvoiceDbContext dbContext, ILogger<InvoiceTemplateService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<InvoiceTemplate> CreateAsync(CreateInvoiceTemplateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceTemplateService), nameof(CreateAsync));
        InvoiceServiceLog.Started(_logger, nameof(CreateAsync));

        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new InvalidOperationException("Template name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.SourceStorageKey))
            {
                throw new InvalidOperationException("A source storage key is required.");
            }

            if (request.LayoutSchemaVersion <= 0)
            {
                throw new InvalidOperationException("Layout schema version must be greater than zero.");
            }

            var template = new InvoiceTemplate
            {
                Name = request.Name.Trim(),
                Description = request.Description ?? string.Empty,
                ActiveVersionId = null,
                IsArchived = false,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            _dbContext.InvoiceTemplates.Add(template);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var version = new InvoiceTemplateVersion
            {
                TemplateId = template.Id,
                VersionNumber = 1,
                SourceType = request.SourceType,
                SourceStorageKey = request.SourceStorageKey,
                SelectedPdfPageNumber = request.SelectedPdfPageNumber,
                PageWidthPoints = request.PageWidthPoints,
                PageHeightPoints = request.PageHeightPoints,
                LayoutSchemaVersion = request.LayoutSchemaVersion,
                LayoutJson = request.LayoutJson,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            _dbContext.InvoiceTemplateVersions.Add(version);
            await _dbContext.SaveChangesAsync(cancellationToken);

            template.ActiveVersionId = version.Id;
            template.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            var created = await _dbContext.InvoiceTemplates
                .Include(x => x.ActiveVersion)
                .SingleAsync(x => x.Id == template.Id, cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(CreateAsync), created.Id);
            return created;
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(CreateAsync));
            throw;
        }
    }

    public async Task<InvoiceTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceTemplateService), nameof(GetByIdAsync), templateId);
        InvoiceServiceLog.Started(_logger, nameof(GetByIdAsync), templateId);

        try
        {
            var template = await _dbContext.InvoiceTemplates
                .Include(x => x.ActiveVersion)
                .SingleOrDefaultAsync(x => x.Id == templateId, cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(GetByIdAsync), templateId);
            return template;
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(GetByIdAsync), templateId);
            throw;
        }
    }

    public async Task<IReadOnlyList<InvoiceTemplate>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceTemplateService), nameof(ListAsync));
        InvoiceServiceLog.Started(_logger, nameof(ListAsync));

        try
        {
            var result = await _dbContext.InvoiceTemplates
                .Include(x => x.ActiveVersion)
                .OrderBy(x => x.Name)
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
}
