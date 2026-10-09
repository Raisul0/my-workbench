using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyWorkbench.Infrastructure;
using MyWorkbench.Modules.Invoices.Application;
using MyWorkbench.Modules.Invoices.Endpoints;
using MyWorkbench.Modules.Invoices.Persistence;
using MyWorkbench.Shared;

namespace MyWorkbench.Modules.Invoices;

public static class ModuleRegistration
{
    public static IServiceCollection AddInvoicesModule(
        this IServiceCollection services,
        IConfiguration configuration,
        bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<InvoiceDbContext>(options =>
            options.UseInMemoryDatabase("MyWorkbenchInvoices"));

        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<InvoiceDraftService>();
        services.AddScoped<InvoiceTemplateService>();
        services.AddScoped<InvoiceTemplateUploadService>();
        services.AddScoped<InvoiceTemplateLayoutService>();
        services.AddScoped<IInvoicePdfRenderer, InvoicePdfRenderer>();
        services.AddScoped<InvoiceGenerationService>();

        services.AddSingleton(new ModuleDescriptor(
            Id: "invoices",
            DisplayName: "Invoices",
            Description: "Invoice templates, invoice generation, and payment-ready PDF output.",
            Icon: "receipt",
            RoutePath: "/invoices",
            ApiBasePath: "/api/invoices",
            Enabled: enabled,
            SortOrder: 100));

        return services;
    }

    public static RouteGroupBuilder MapInvoicesModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapInvoicesEndpoints();

        group.MapGet("/status", () => Results.Ok(new
        {
            module = "invoices",
            status = "ready",
            enabled = true
        }));

        return group;
    }
}
