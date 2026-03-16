using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SmartShift.Blazor.Client.Pages;
using SmartShift.Blazor.Components;
using SmartShift.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

// Add Microsoft Identity Web
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
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
// Named client for SmartOps Web API
builder.Services.AddHttpClient("SmartOpsApi");

// Configure and add claims transformation for role assignment via configuration
builder.Services.Configure<RoleOptions>(
    builder.Configuration.GetSection(RoleOptions.SectionName));
builder.Services.AddScoped<IClaimsTransformation, SmartShiftClaims>();

// Replaceable real data service that proxies to SmartOps Web API
builder.Services.AddScoped<IShiftDataService, ShiftDataService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
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
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(SmartShift.Blazor.Client._Imports).Assembly);

// Map Microsoft Identity Web endpoints
app.MapControllers();

app.Run();


