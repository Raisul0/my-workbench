using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MyWorkbench.Api;

public static class AppAuthenticationSettings
{
    public static bool ShouldRequireAuthentication(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration.GetValue("Authentication:RequireAuthentication", environment.IsDevelopment() ? false : true);
        return configured;
    }
}
