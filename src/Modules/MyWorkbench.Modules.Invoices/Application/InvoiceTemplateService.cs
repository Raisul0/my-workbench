using Microsoft.EntityFrameworkCore;
using MyWorkbench.Modules.Invoices.Contracts;
using MyWorkbench.Modules.Invoices.Domain;
using MyWorkbench.Modules.Invoices.Persistence;

namespace MyWorkbench.Modules.Invoices.Application;

public class InvoiceTemplateService
{
    private readonly InvoiceDbContext _dbContext;

    public InvoiceTemplateService(InvoiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InvoiceTemplate> CreateAsync(CreateInvoiceTemplateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

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
            IsArchived = false,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        var version = new InvoiceTemplateVersion
        {
            Template = template,
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

        template.Versions.Add(version);
        template.ActiveVersion = version;
        template.ActiveVersionId = version.Id;

        _dbContext.InvoiceTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _dbContext.InvoiceTemplates
            .Include(x => x.ActiveVersion)
            .SingleAsync(x => x.Id == template.Id, cancellationToken);
    }

    public async Task<InvoiceTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.InvoiceTemplates
            .Include(x => x.ActiveVersion)
            .SingleOrDefaultAsync(x => x.Id == templateId, cancellationToken);
    }

    public async Task<IReadOnlyList<InvoiceTemplate>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.InvoiceTemplates
            .Include(x => x.ActiveVersion)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
