using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SmartOps.Blazor.Components;
using SmartOps.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Validate Azure AD configuration before proceeding
var azureAdSection = builder.Configuration.GetSection("AzureAd");
var missingSettings = new List<string>();

if (string.IsNullOrEmpty(azureAdSection["TenantId"])) missingSettings.Add("TenantId");
if (string.IsNullOrEmpty(azureAdSection["ClientId"])) missingSettings.Add("ClientId");
if (string.IsNullOrEmpty(azureAdSection["ClientSecret"])) missingSettings.Add("ClientSecret");

if (missingSettings.Count > 0)
{
    // In development, show a helpful startup message and run a minimal app with an error page
    if (builder.Environment.IsDevelopment())
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n========================================");
        Console.WriteLine("  AZURE AD CONFIGURATION MISSING");
        Console.WriteLine("========================================");
        Console.WriteLine($"Missing settings: {string.Join(", ", missingSettings)}");
        Console.WriteLine("\nPlease configure these values in your user secrets:");
        Console.WriteLine("  dotnet user-secrets set \"AzureAd:TenantId\" \"your-tenant-id\"");
        Console.WriteLine("  dotnet user-secrets set \"AzureAd:ClientId\" \"your-client-id\"");
        Console.WriteLine("  dotnet user-secrets set \"AzureAd:ClientSecret\" \"your-client-secret\"");
        Console.WriteLine("========================================\n");
        Console.ResetColor();

        // Run a minimal app that shows a configuration error page
        builder.Services.AddRazorComponents();
        var errorApp = builder.Build();
        errorApp.MapGet("/", () => Results.Content(
            """
            <!DOCTYPE html>
            <html>
            <head><title>Configuration Error</title></head>
            <body style="font-family: system-ui; max-width: 800px; margin: 50px auto; padding: 20px;">
                <h1 style="color: #dc3545;">⚠️ Azure AD Configuration Required</h1>
                <p>The application cannot start because Azure AD authentication is not configured.</p>
                <h3>Missing Settings:</h3>
                <ul>
            """ + string.Join("", missingSettings.Select(s => $"<li><code>{s}</code></li>")) + """
                </ul>
                <h3>To configure, run these commands:</h3>
                <pre style="background: #f4f4f4; padding: 15px; border-radius: 5px;">
            dotnet user-secrets set "AzureAd:TenantId" "your-tenant-id"
            dotnet user-secrets set "AzureAd:ClientId" "your-client-id"
            dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"</pre>
                <p>Then restart the application.</p>
            </body>
            </html>
            """, "text/html"));
        errorApp.Run();
        return;
    }
    else
    {
        // In production, throw a clear exception
        throw new InvalidOperationException(
            $"Azure AD configuration is incomplete. Missing: {string.Join(", ", missingSettings)}. " +
            "Please ensure all required Azure AD settings are configured.");
    }
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddAuthenticationStateSerialization();

// Add Microsoft Identity Web
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(azureAdSection)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddTokenAcquisition();

builder.Services.AddAuthorization(options =>
{
    // Add custom authorization policies if needed
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddControllers()
    .AddMicrosoftIdentityUI();

// Add HTTP client factory for Graph API calls in claims transformation
builder.Services.AddHttpClient();

// Configure and add claims transformation for role assignment via configuration
builder.Services.Configure<RoleOptions>(
    builder.Configuration.GetSection(RoleOptions.SectionName));
builder.Services.AddScoped<IClaimsTransformation, SmartOpsClaimsTransformation>();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Development-specific configuration
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map Microsoft Identity Web endpoints
app.MapControllers();

app.Run();
