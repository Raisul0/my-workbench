using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyWorkbench.Modules.Invoices.Contracts;
using MyWorkbench.Modules.Invoices.Domain;
using MyWorkbench.Modules.Invoices.Persistence;

namespace MyWorkbench.Modules.Invoices.Application;

public sealed class InvoiceTemplateLayoutService
{
    private readonly InvoiceDbContext _dbContext;
    private readonly ILogger<InvoiceTemplateLayoutService> _logger;

    public InvoiceTemplateLayoutService(InvoiceDbContext dbContext, ILogger<InvoiceTemplateLayoutService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<InvoiceTemplateVersion> UpdateLayoutAsync(Guid templateId, TemplateLayoutDocument layout, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(layout);

        using var scope = InvoiceServiceLog.BeginOperationScope(_logger, nameof(InvoiceTemplateLayoutService), nameof(UpdateLayoutAsync), templateId);
        InvoiceServiceLog.Started(_logger, nameof(UpdateLayoutAsync), templateId);

        try
        {
            var template = await _dbContext.InvoiceTemplates
                .Include(x => x.Versions)
                .SingleOrDefaultAsync(x => x.Id == templateId, cancellationToken);

            if (template is null)
            {
                throw new InvalidOperationException("The requested template was not found.");
            }

            if (template.IsArchived)
            {
                throw new InvalidOperationException("Archived templates cannot be modified.");
            }

            var validator = new TemplateLayoutValidator();
            if (!validator.IsValid(layout))
            {
                throw new InvalidOperationException("The template layout is invalid or missing required binding data.");
            }

            var previousVersion = await _dbContext.InvoiceTemplateVersions
                .Where(x => x.TemplateId == templateId)
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            var nextVersionNumber = previousVersion is null ? 1 : previousVersion.VersionNumber + 1;
            var nextVersion = new InvoiceTemplateVersion
            {
                TemplateId = templateId,
                Template = template,
                VersionNumber = nextVersionNumber,
                SourceType = previousVersion?.SourceType ?? TemplateSourceType.Image,
                SourceStorageKey = previousVersion?.SourceStorageKey ?? string.Empty,
                SelectedPdfPageNumber = previousVersion?.SelectedPdfPageNumber,
                PageWidthPoints = previousVersion?.PageWidthPoints ?? 595m,
                PageHeightPoints = previousVersion?.PageHeightPoints ?? 842m,
                LayoutSchemaVersion = layout.SchemaVersion,
                LayoutJson = JsonSerializer.Serialize(layout),
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            template.Versions.Add(nextVersion);
            template.ActiveVersion = nextVersion;
            template.ActiveVersionId = nextVersion.Id;
            template.UpdatedAtUtc = DateTimeOffset.UtcNow;

            _dbContext.InvoiceTemplateVersions.Add(nextVersion);
            await _dbContext.SaveChangesAsync(cancellationToken);
            InvoiceServiceLog.Succeeded(_logger, nameof(UpdateLayoutAsync), templateId);

            return nextVersion;
        }
        catch (Exception ex)
        {
            InvoiceServiceLog.Failed(_logger, ex, nameof(UpdateLayoutAsync), templateId);
            throw;
        }
    }
}
