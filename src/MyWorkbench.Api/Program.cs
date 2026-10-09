using MyWorkbench.Modules.Invoices;
using MyWorkbench.Shared;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddOpenApi();

var invoiceEnabled = builder.Configuration.GetValue("Modules:Invoices:Enabled", true);
builder.Services.AddInvoicesModule(builder.Configuration, invoiceEnabled);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/login", () => Results.Ok(new
{
    message = "Authentication required.",
    redirect = "/login"
}));

app.MapGet("/api/modules", (IEnumerable<ModuleDescriptor> modules) =>
    Results.Ok(ModuleRegistry.FilterEnabled(modules)))
    .RequireAuthorization();

app.MapInvoicesModule().RequireAuthorization();

app.Run();
