using Microsoft.EntityFrameworkCore;
using MyWorkbench.Api;
using MyWorkbench.Modules.Invoices;
using MyWorkbench.Modules.Invoices.Persistence;
using MyWorkbench.Shared;

var builder = WebApplication.CreateBuilder(args);

var requireAuthentication = AppAuthenticationSettings.ShouldRequireAuthentication(builder.Configuration, builder.Environment);

if (requireAuthentication)
{
    builder.Services.AddAuthentication("MyWorkbenchCookie")
        .AddCookie("MyWorkbenchCookie", options =>
        {
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/login";
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
        });

    builder.Services.AddAuthorization();
}
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyWorkbenchDev", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "http://127.0.0.1:4200",
                "https://localhost:4200",
                "https://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddOpenApi();

var invoiceEnabled = builder.Configuration.GetValue("Modules:Invoices:Enabled", true);
builder.Services.AddInvoicesModule(builder.Configuration, invoiceEnabled);

var app = builder.Build();

var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MyWorkbench.Startup");

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Failed to initialize the invoice database during startup.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("MyWorkbench.Exceptions");
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();

        if (feature?.Error is not null)
        {
            logger.LogError(
                feature.Error,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path.Value);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred." });
    });
});

app.UseCors("MyWorkbenchDev");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

if (requireAuthentication)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/login", () => Results.Ok(new
{
    message = "Authentication required.",
    redirect = "/login"
}));

var modulesEndpoint = app.MapGet("/api/modules", (IEnumerable<ModuleDescriptor> modules) =>
    Results.Ok(ModuleRegistry.FilterEnabled(modules)));

if (requireAuthentication)
{
    modulesEndpoint.RequireAuthorization();
}

var invoicesEndpoint = app.MapInvoicesModule();
if (requireAuthentication)
{
    invoicesEndpoint.RequireAuthorization();
}

app.Run();
