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

        var connectionString = ResolveConnectionString(configuration);

        services.AddDbContext<InvoiceDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<InvoiceDraftService>();
        services.AddScoped<InvoiceTemplateService>();
        services.AddScoped<InvoiceTemplateUploadService>();
        services.AddScoped<InvoiceTemplateLayoutService>();
        services.AddScoped<TemplateBackgroundImageService>();
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

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var configuredValue = configuration.GetConnectionString("MyWorkbench");

        var password = Environment.GetEnvironmentVariable("MY_WORKBENCH_SQL_PASSWORD") ?? Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD");

        if (!string.IsNullOrWhiteSpace(configuredValue))
        {
            return ExpandEnvironmentVariables(configuredValue, password);
        }

        var builtValue = $"Server=localhost\\MSSQLSERVER01;Database=MyWorkbench;User Id=sa;Password={password};TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True;";

        return string.IsNullOrWhiteSpace(password)
            ? throw new InvalidOperationException("A SQL Server connection string is required. Set the MyWorkbench connection string or MY_WORKBENCH_SQL_PASSWORD.")
            : builtValue;
    }

    private static string ExpandEnvironmentVariables(string value, string? password)
    {
        var expanded = Environment.ExpandEnvironmentVariables(value);

        if (!string.IsNullOrWhiteSpace(password))
        {
            expanded = expanded.Replace("${MY_WORKBENCH_SQL_PASSWORD}", password, StringComparison.Ordinal);
            expanded = expanded.Replace("%MY_WORKBENCH_SQL_PASSWORD%", password, StringComparison.Ordinal);
            expanded = expanded.Replace("${MSSQL_SA_PASSWORD}", password, StringComparison.Ordinal);
            expanded = expanded.Replace("%MSSQL_SA_PASSWORD%", password, StringComparison.Ordinal);
        }

        return expanded;
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
