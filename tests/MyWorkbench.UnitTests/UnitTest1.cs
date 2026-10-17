using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using MyWorkbench.Shared;
using MyWorkbench.Modules.Invoices.Application;
using MyWorkbench.Modules.Invoices.Contracts;
using MyWorkbench.Modules.Invoices.Domain;
using MyWorkbench.Modules.Invoices.Persistence;
using MyWorkbench.Infrastructure;
using MyWorkbench.Api;

namespace MyWorkbench.UnitTests;

public class ModuleRegistryTests
{
    [Fact]
    public void ModuleDescriptor_MustIncludeRequiredMetadata()
    {
        var descriptor = new ModuleDescriptor(
            "invoices",
            "Invoices",
            "Invoice management",
            "receipt",
            "/invoices",
            "/api/invoices",
            true,
            100);

        Assert.Equal("invoices", descriptor.Id);
        Assert.Equal("Invoices", descriptor.DisplayName);
        Assert.Equal("/invoices", descriptor.RoutePath);
        Assert.True(descriptor.Enabled);
    }

    [Fact]
    public void ModuleRegistry_ShouldFilterDisabledModules()
    {
        var modules = new[]
        {
            new ModuleDescriptor("invoices", "Invoices", "Invoice management", "receipt", "/invoices", "/api/invoices", true, 100),
            new ModuleDescriptor("settings", "Settings", "Settings", "settings", "/settings", "/api/settings", false, 200)
        };

        var enabledModules = ModuleRegistry.FilterEnabled(modules);

        Assert.Single(enabledModules);
        Assert.Equal("invoices", enabledModules[0].Id);
    }
}

public class AuthenticationSettingsTests
{
    [Fact]
    public void Development_Should_Not_Require_Authentication_By_Default()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:RequireAuthentication"] = "false"
            })
            .Build();

        var environment = new FakeHostEnvironment { EnvironmentName = Environments.Development };

        Assert.False(AppAuthenticationSettings.ShouldRequireAuthentication(configuration, environment));
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "MyWorkbench.Api";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = Environments.Production;
    }

    private sealed class NullFileProvider : IFileProvider
    {
        public IDirectoryContents GetDirectoryContents(string subpath) => new NotFoundDirectoryContents();
        public IFileInfo GetFileInfo(string subpath) => new NotFoundFileInfo(subpath);
        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

        private sealed class NotFoundDirectoryContents : IDirectoryContents
        {
            public bool Exists => false;
            public IEnumerator<IFileInfo> GetEnumerator() => Enumerable.Empty<IFileInfo>().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NotFoundFileInfo(string name) : IFileInfo
        {
            public bool Exists => false;
            public long Length => 0;
            public string PhysicalPath => name;
            public string Name => name;
            public DateTimeOffset LastModified => DateTimeOffset.MinValue;
            public bool IsDirectory => false;
            public Stream CreateReadStream() => new MemoryStream();
        }
    }
}

public class InvoiceCalculatorTests
{
    [Fact]
    public void CalculateInvoiceTotals_ShouldMatchTaxExclusiveRules()
    {
        var lineItems = new[]
        {
            new InvoiceLineItem
            {
                Description = "Consulting",
                Quantity = 2m,
                UnitPrice = 100m,
                DiscountPercent = 10m,
                TaxRate = 6m
            }
        };

        var totals = InvoiceTotalsCalculator.Calculate(lineItems);

        Assert.Equal(200m, totals.Subtotal);
        Assert.Equal(20m, totals.DiscountTotal);
        Assert.Equal(10.8m, totals.TaxTotal);
        Assert.Equal(190.8m, totals.GrandTotal);
    }

    [Fact]
    public void ReserveNextInvoiceNumber_ShouldNeverReuseNumbers()
    {
        var settings = new BusinessProfile
        {
            InvoicePrefix = "INV",
            NextSequence = 100
        };

        Assert.Equal("INV-101", InvoiceNumberAllocator.NextNumber(settings));
        settings.NextSequence = 101;
        Assert.Equal("INV-102", InvoiceNumberAllocator.NextNumber(settings));
    }
}

public class Phase3TemplateTests
{
    [Fact]
    public void FileValidation_ShouldRejectUnsupportedMimeAndPathTraversal()
    {
        var validation = new TemplateUploadValidator();

        var invalidPath = "..\\evil.pdf";
        Assert.False(validation.IsAllowedSafeFileName(invalidPath));
        Assert.False(validation.IsSupportedContentType("application/x-executable"));
    }

    [Fact]
    public void TemplateLayout_ShouldRequireSchemaVersionAndBindingRoot()
    {
        var validator = new TemplateLayoutValidator();
        var layout = new TemplateLayoutDocument
        {
            SchemaVersion = 1,
            Fields = new List<TemplateFieldBinding>
            {
                new TemplateFieldBinding { Name = "invoice_number", Type = "Text" }
            }
        };

        Assert.True(validator.IsValid(layout));

        var invalid = new TemplateLayoutDocument { SchemaVersion = 0, Fields = new List<TemplateFieldBinding>() };
        Assert.False(validator.IsValid(invalid));
    }
}

public class TemplateVersioningTests
{
    [Fact]
    public async Task CreateInvoiceTemplate_ShouldCreateFirstVersionAndSetItActive()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InvoiceDbContext(options);
        var service = new InvoiceTemplateService(dbContext, NullLogger<InvoiceTemplateService>.Instance);

        var template = await service.CreateAsync(new CreateInvoiceTemplateRequest
        {
            Name = "Standard Invoice",
            Description = "Base design",
            SourceType = TemplateSourceType.Pdf,
            SourceStorageKey = "storage-key-1.pdf",
            SelectedPdfPageNumber = 1,
            PageWidthPoints = 595m,
            PageHeightPoints = 842m,
            LayoutSchemaVersion = 1,
            LayoutJson = "{\"schemaVersion\":1,\"fields\":[{\"name\":\"invoice_number\",\"type\":\"Text\"}]}"
        });

        Assert.Equal("Standard Invoice", template.Name);
        Assert.False(template.IsArchived);
        Assert.NotEqual(Guid.Empty, template.ActiveVersionId);
        Assert.Equal(1, template.ActiveVersion?.VersionNumber);
        Assert.Equal(TemplateSourceType.Pdf, template.ActiveVersion?.SourceType);
    }

    [Fact]
    public async Task UploadTemplate_ShouldPersistPrivateStorageKeyAndValidatedFile()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InvoiceDbContext(options);
        var storage = new LocalFileStorage();
        var templateService = new InvoiceTemplateService(dbContext, NullLogger<InvoiceTemplateService>.Instance);
        var service = new InvoiceTemplateUploadService(dbContext, storage, templateService, NullLogger<InvoiceTemplateUploadService>.Instance);

        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A };
        await using var stream = new MemoryStream(pdfBytes);
        var file = new FormFile(stream, 0, pdfBytes.Length, "file", "sample.pdf");

        var template = await service.UploadAsync(file, new CreateInvoiceTemplateRequest
        {
            Name = "Uploaded template",
            Description = "PDF source",
            SourceType = TemplateSourceType.Pdf,
            SelectedPdfPageNumber = 1,
            PageWidthPoints = 595m,
            PageHeightPoints = 842m,
            LayoutSchemaVersion = 1,
            LayoutJson = "{\"schemaVersion\":1,\"fields\":[]}"
        });

        Assert.Equal("Uploaded template", template.Name);
        Assert.NotEqual("sample.pdf", template.ActiveVersion!.SourceStorageKey);
        Assert.EndsWith(".pdf", template.ActiveVersion.SourceStorageKey);

        await using var stored = await storage.OpenReadAsync(template.ActiveVersion.SourceStorageKey);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task UpdateLayout_ShouldCreateNewVersionWithoutLosingPreviousTracks()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InvoiceDbContext(options);
        var templateService = new InvoiceTemplateService(dbContext, NullLogger<InvoiceTemplateService>.Instance);
        var layoutService = new InvoiceTemplateLayoutService(dbContext, NullLogger<InvoiceTemplateLayoutService>.Instance);

        var template = await templateService.CreateAsync(new CreateInvoiceTemplateRequest
        {
            Name = "Versioned template",
            Description = "Base",
            SourceType = TemplateSourceType.Image,
            SourceStorageKey = "base-image.png",
            PageWidthPoints = 595m,
            PageHeightPoints = 842m,
            LayoutSchemaVersion = 1,
            LayoutJson = "{\"schemaVersion\":1,\"fields\":[]}"
        });

        var originalVersionId = template.ActiveVersionId;

        var updated = await layoutService.UpdateLayoutAsync(template.Id, new TemplateLayoutDocument
        {
            SchemaVersion = 1,
            Fields = new List<TemplateFieldBinding>
            {
                new() { Name = "invoice_number", Type = "Text" }
            }
        });

        Assert.Equal(2, updated.VersionNumber);
        Assert.Equal(template.Id, updated.TemplateId);
        Assert.NotEqual(originalVersionId, updated.Id);
        Assert.Equal(2, await dbContext.InvoiceTemplateVersions.CountAsync());
    }

    [Fact]
    public async Task PreviewRenderer_ShouldReturnPdfBytesForTemplateVersion()
    {
        var renderer = new InvoicePdfRenderer(NullLogger<InvoicePdfRenderer>.Instance);
        var version = new InvoiceTemplateVersion
        {
            SourceType = TemplateSourceType.Pdf,
            SourceStorageKey = "sample.pdf",
            PageWidthPoints = 595m,
            PageHeightPoints = 842m,
            LayoutSchemaVersion = 1,
            LayoutJson = "{\"schemaVersion\":1,\"fields\":[]}",
            SelectedPdfPageNumber = 1
        };

        var invoice = new Invoice
        {
            CustomerName = "Acme",
            CurrencyCode = "USD",
            LineItemColumns = "client:text,description:text,total:number",
            LineItems =
            [
                new InvoiceLineItem
                {
                    Description = "Consulting",
                    Quantity = 2m,
                    UnitPrice = 100m,
                    LineTotal = 200m
                }
            ]
        };

        var bytes = await renderer.RenderPreviewAsync(version, invoice);
        var html = System.Text.Encoding.UTF8.GetString(bytes);

        Assert.NotEmpty(bytes);
        Assert.StartsWith("<!DOCTYPE", html);

        var clientHeaderPosition = html.IndexOf("<th>Client</th>", StringComparison.Ordinal);
        var descriptionHeaderPosition = html.IndexOf("<th>Description</th>", StringComparison.Ordinal);
        var totalHeaderPosition = html.IndexOf("<th style=\"text-align: right;\">Total</th>", StringComparison.Ordinal);

        Assert.True(clientHeaderPosition >= 0);
        Assert.True(descriptionHeaderPosition > clientHeaderPosition);
        Assert.True(totalHeaderPosition > descriptionHeaderPosition);
    }
}

public class InvoiceGenerationTests
{
    [Fact]
    public async Task GenerateInvoice_ShouldStorePdfAndMarkInvoiceGenerated()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InvoiceDbContext(options);
        var storage = new LocalFileStorage();
        var renderer = new InvoicePdfRenderer(NullLogger<InvoicePdfRenderer>.Instance);
        var generationService = new InvoiceGenerationService(dbContext, storage, renderer, NullLogger<InvoiceGenerationService>.Instance);
        var draftService = new InvoiceDraftService(dbContext, NullLogger<InvoiceDraftService>.Instance);

        var template = await new InvoiceTemplateService(dbContext, NullLogger<InvoiceTemplateService>.Instance).CreateAsync(new CreateInvoiceTemplateRequest
        {
            Name = "Invoice Template",
            Description = "Main layout",
            SourceType = TemplateSourceType.Pdf,
            SourceStorageKey = "source.pdf",
            SelectedPdfPageNumber = 1,
            PageWidthPoints = 595m,
            PageHeightPoints = 842m,
            LayoutSchemaVersion = 1,
            LayoutJson = "{\"schemaVersion\":1,\"fields\":[]}"
        });

        var invoice = await draftService.CreateDraftAsync(new CreateInvoiceDraftRequest
        {
            CustomerName = "Contoso",
            CurrencyCode = "USD",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            LineItems = new List<CreateInvoiceLineItemRequest>
            {
                new() { Description = "Consulting", Quantity = 2m, UnitPrice = 100m, DiscountPercent = 10m, TaxRate = 6m }
            }
        });

        invoice.TemplateVersionId = template.ActiveVersionId ?? Guid.Empty;
        dbContext.Invoices.Update(invoice);
        await dbContext.SaveChangesAsync();

        var preview = await generationService.PreviewAsync(invoice.Id, CancellationToken.None);
        Assert.NotEmpty(preview);

        var generated = await generationService.GenerateAsync(invoice.Id, CancellationToken.None);
        Assert.Equal(InvoiceStatus.Generated, generated.Status);
        Assert.False(string.IsNullOrWhiteSpace(generated.PdfStorageKey));
        Assert.False(string.IsNullOrWhiteSpace(generated.PdfSha256));
        Assert.NotNull(await storage.OpenReadAsync(generated.PdfStorageKey!));
    }

    [Fact]
    public async Task CreateDraft_ShouldUseSelectedProfileAndPersistOrderedColumns()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InvoiceDbContext(options);

        var firstProfile = new BusinessProfile
        {
            IssuerName = "Profile A",
            InvoicePrefix = "A",
            LineItemColumns = "description:text,total:number"
        };

        var secondProfile = new BusinessProfile
        {
            IssuerName = "Profile B",
            InvoicePrefix = "B",
            LineItemColumns = "description:text,total:number"
        };

        dbContext.BusinessProfiles.AddRange(firstProfile, secondProfile);
        await dbContext.SaveChangesAsync();

        var draftService = new InvoiceDraftService(dbContext, NullLogger<InvoiceDraftService>.Instance);

        var invoice = await draftService.CreateDraftAsync(new CreateInvoiceDraftRequest
        {
            BusinessProfileId = secondProfile.Id,
            CustomerName = "Northwind",
            LineItemColumns = new List<string> { "Client", "Description", "Total" },
            LineItems = new List<CreateInvoiceLineItemRequest>
            {
                new() { Description = "Development", Quantity = 5m, UnitPrice = 80m, DiscountPercent = 0m, TaxRate = 0m }
            }
        });

        Assert.Equal(secondProfile.Id, invoice.BusinessProfileId);
        Assert.Equal("client:text,description:text,total:number", invoice.LineItemColumns);

        var updatedProfile = await dbContext.BusinessProfiles.SingleAsync(x => x.Id == secondProfile.Id);
        Assert.Equal("client:text,description:text,total:number", updatedProfile.LineItemColumns);
    }

    [Fact]
    public async Task CreateDraft_ShouldRejectUnsupportedLineItemColumn()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InvoiceDbContext(options);
        var draftService = new InvoiceDraftService(dbContext, NullLogger<InvoiceDraftService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => draftService.CreateDraftAsync(new CreateInvoiceDraftRequest
        {
            CustomerName = "Tailspin",
            LineItemColumns = new List<string> { "Description", "UnknownField" },
            LineItems = new List<CreateInvoiceLineItemRequest>
            {
                new() { Description = "Support", Quantity = 1m, UnitPrice = 50m, DiscountPercent = 0m, TaxRate = 0m }
            }
        }));
    }
}
