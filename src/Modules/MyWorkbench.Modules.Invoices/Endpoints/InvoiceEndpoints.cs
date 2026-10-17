using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MyWorkbench.Modules.Invoices.Application;
using MyWorkbench.Modules.Invoices.Contracts;
using MyWorkbench.Modules.Invoices.Domain;

namespace MyWorkbench.Modules.Invoices.Endpoints;

public static class InvoiceEndpoints
{
    public static RouteGroupBuilder MapInvoicesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/invoices");

        // Business Profile Endpoints
        group.MapGet("/profiles", GetProfiles);
        group.MapGet("/profiles/{profileId}", GetProfileById);
        group.MapPost("/profiles", CreateProfile);
        group.MapPut("/profiles/{profileId}", UpdateProfile);
        group.MapDelete("/profiles/{profileId}", DeleteProfile);

        group.MapGet("/settings", GetSettings);


        group.MapGet("", async (InvoiceDraftService service, CancellationToken cancellationToken) =>
        {
            var invoices = await service.ListAsync(cancellationToken);
            return Results.Ok(invoices.Select(invoice => new
            {
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.Status,
                invoice.CurrencyCode,
                invoice.GrandTotal,
                invoice.IssueDate,
                CustomerName = invoice.BusinessProfile?.IssuerName ?? string.Empty
            }));
        });

        group.MapPost("/drafts", async (CreateInvoiceDraftRequest request, InvoiceDraftService service, CancellationToken cancellationToken) =>
        {
            if (request is null)
            {
                return Results.BadRequest(new { message = "Request body is required." });
            }

            if (request.LineItems is null || request.LineItems.Count == 0)
            {
                return Results.BadRequest(new { message = "At least one line item is required." });
            }

            var invoice = await service.CreateDraftAsync(request, cancellationToken);
            return Results.Ok(new
            {
                invoice.Id,
                invoice.BusinessProfileId,
                invoice.InvoiceNumber,
                invoice.Status,
                invoice.GrandTotal,
                invoice.CreatedAtUtc,
                LineItemColumns = invoice.LineItemColumns
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            });
        });

        group.MapGet("/templates", async (InvoiceTemplateService service, CancellationToken cancellationToken) =>
        {
            var templates = await service.ListAsync(cancellationToken);
            return Results.Ok(templates.Select(template => new
            {
                template.Id,
                template.Name,
                template.Description,
                template.ActiveVersionId,
                template.IsArchived,
                template.ActiveVersion?.VersionNumber,
                template.ActiveVersion?.SourceType,
                template.ActiveVersion?.LayoutJson,
                template.UpdatedAtUtc
            }));
        });

        group.MapPost("/templates", async (CreateInvoiceTemplateRequest request, InvoiceTemplateService service, CancellationToken cancellationToken) =>
        {
            if (request is null)
            {
                return Results.BadRequest(new { message = "Request body is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { message = "Template name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.SourceStorageKey))
            {
                return Results.BadRequest(new { message = "Source storage key is required." });
            }

            var template = await service.CreateAsync(request, cancellationToken);
            return Results.Ok(new
            {
                template.Id,
                template.Name,
                template.ActiveVersionId,
                template.ActiveVersion?.VersionNumber,
                template.ActiveVersion?.LayoutSchemaVersion,
                template.CreatedAtUtc
            });
        });

        group.MapPost("/templates/upload", async (HttpRequest request, InvoiceTemplateUploadService service, CancellationToken cancellationToken) =>
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.FirstOrDefault();
            if (file is null)
            {
                return Results.BadRequest(new { message = "A template file is required." });
            }

            var templateRequest = new CreateInvoiceTemplateRequest
            {
                Name = form["name"].FirstOrDefault() ?? "New Template",
                Description = form["description"].FirstOrDefault() ?? string.Empty,
                SourceType = TemplateSourceType.Pdf,
                SelectedPdfPageNumber = int.TryParse(form["selectedPdfPageNumber"].FirstOrDefault(), out var pageNumber) ? pageNumber : null,
                PageWidthPoints = decimal.TryParse(form["pageWidthPoints"].FirstOrDefault(), out var width) ? width : 595m,
                PageHeightPoints = decimal.TryParse(form["pageHeightPoints"].FirstOrDefault(), out var height) ? height : 842m,
                LayoutSchemaVersion = int.TryParse(form["layoutSchemaVersion"].FirstOrDefault(), out var schemaVersion) ? schemaVersion : 1,
                LayoutJson = form["layoutJson"].FirstOrDefault() ?? "{\"schemaVersion\":1,\"fields\":[]}"
            };

            try
            {
                var template = await service.UploadAsync(file, templateRequest, cancellationToken);
                return Results.Ok(new
                {
                    template.Id,
                    template.Name,
                    template.ActiveVersionId,
                    template.ActiveVersion?.VersionNumber,
                    template.ActiveVersion?.SourceStorageKey,
                    template.ActiveVersion?.SourceType,
                    template.CreatedAtUtc
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapPost("/templates/{templateId}/layout", async (Guid templateId, TemplateLayoutDocument layout, InvoiceTemplateLayoutService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var version = await service.UpdateLayoutAsync(templateId, layout, cancellationToken);
                return Results.Ok(new
                {
                    version.Id,
                    version.TemplateId,
                    version.VersionNumber,
                    version.LayoutSchemaVersion,
                    version.LayoutJson,
                    version.CreatedAtUtc
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapPost("/templates/upload-background", async (HttpRequest request, TemplateBackgroundImageService service, CancellationToken cancellationToken) =>
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.FirstOrDefault();
            if (file is null)
            {
                return Results.BadRequest(new { message = "A background image file is required." });
            }

            try
            {
                var imagePath = await service.UploadBackgroundImageAsync(file, cancellationToken);
                return Results.Ok(new
                {
                    imagePath,
                    fileName = file.FileName,
                    uploadedAtUtc = DateTimeOffset.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapGet("/templates/background/{fileName}", async (string fileName, TemplateBackgroundImageService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var base64 = await service.GetBackgroundImageAsBase64Async(fileName, cancellationToken);
                if (string.IsNullOrEmpty(base64))
                {
                    return Results.NotFound(new { message = "Image not found." });
                }
                return Results.Ok(new { imageBase64 = base64 });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapPost("/templates/{templateId}/preview", async (Guid templateId, InvoiceTemplateService service, IInvoicePdfRenderer renderer, CancellationToken cancellationToken) =>
        {
            var template = await service.GetByIdAsync(templateId, cancellationToken);
            if (template is null)
            {
                return Results.NotFound(new { message = "Template not found." });
            }

            var version = template.ActiveVersion ?? throw new InvalidOperationException("The template has no active version.");
            var bytes = await renderer.RenderPreviewAsync(version, new { preview = true, templateName = template.Name }, cancellationToken);
            return Results.File(bytes, "application/pdf", $"{template.Name}-preview.pdf");
        });

        group.MapGet("{invoiceId}/preview", async (Guid invoiceId, InvoiceGenerationService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var bytes = await service.PreviewAsync(invoiceId, cancellationToken);
                return Results.File(bytes, "application/pdf", "invoice-preview.pdf");
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapPost("{invoiceId}/generate", async (Guid invoiceId, InvoiceGenerationService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var invoice = await service.GenerateAsync(invoiceId, cancellationToken);
                return Results.Ok(new
                {
                    invoice.Id,
                    invoice.InvoiceNumber,
                    invoice.Status,
                    invoice.PdfStorageKey,
                    invoice.PdfSha256,
                    invoice.GeneratedAtUtc
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapDelete("{invoiceId}", async (Guid invoiceId, InvoiceDraftService service, CancellationToken cancellationToken) =>
        {
            try
            {
                await service.DeleteDraftAsync(invoiceId, cancellationToken);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        return group;
    }

    private static async Task<IResult> GetProfiles(InvoiceDraftService service, CancellationToken cancellationToken)
    {
        var profiles = await service.ListProfilesAsync(cancellationToken);
        return Results.Ok(profiles.Select(profile => new
        {
            profile.Id,
            profile.IssuerName,
            profile.IssuerEmail,
            profile.DefaultCurrencyCode,
            profile.InvoicePrefix,
            profile.NextSequence,
            profile.DefaultTaxRate,
            profile.TaxLabel,
            profile.UpdatedAtUtc
        }));
    }

    private static async Task<IResult> GetProfileById(Guid profileId, InvoiceDraftService service, CancellationToken cancellationToken)
    {
        var profile = await service.GetProfileByIdAsync(profileId, cancellationToken);
        if (profile is null)
        {
            return Results.NotFound(new { message = "Business profile not found." });
        }

        return Results.Ok(new
        {
            profile.Id,
            profile.IssuerName,
            profile.IssuerAddress,
            profile.IssuerEmail,
            profile.IssuerPhone,
            profile.IssuerRegistrationNumber,
            profile.IssuerTaxNumber,
            profile.TaxLabel,
            profile.DefaultCurrencyCode,
            profile.InvoicePrefix,
            profile.NextSequence,
            profile.DefaultTaxRate,
            profile.DefaultTerms,
            profile.LineItemColumns,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc
        });
    }

    private static async Task<IResult> CreateProfile(BusinessProfile request, InvoiceDraftService service, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.IssuerName))
        {
            return Results.BadRequest(new { message = "Issuer name is required." });
        }

        try
        {
            var profile = await service.CreateOrUpdateProfileAsync(null, request, cancellationToken);
            return Results.StatusCode(201);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateProfile(Guid profileId, BusinessProfile request, InvoiceDraftService service, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.IssuerName))
        {
            return Results.BadRequest(new { message = "Issuer name is required." });
        }

        try
        {
            var profile = await service.CreateOrUpdateProfileAsync(profileId, request, cancellationToken);
            return Results.Ok(new
            {
                profile.Id,
                profile.IssuerName,
                profile.IssuerEmail,
                profile.DefaultCurrencyCode,
                profile.UpdatedAtUtc
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteProfile(Guid profileId, InvoiceDraftService service, CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteProfileAsync(profileId, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return Results.InternalServerError();
        }
    }

    private static async Task<IResult> GetSettings(InvoiceDraftService service, CancellationToken cancellationToken)
    {
        var profiles = await service.ListProfilesAsync(cancellationToken);
        return Results.Ok(profiles.Select(profile => new
        {
            profile.Id,
            profile.IssuerName,
            profile.IssuerEmail,
            profile.DefaultCurrencyCode,
            profile.InvoicePrefix,
            profile.NextSequence,
            profile.DefaultTaxRate,
            profile.TaxLabel,
            profile.UpdatedAtUtc
        }));
    }
}
