using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Features.PublicApi.Payments;
using PaymentGateway.Web.Features.PublicApi.Webhooks;
using PaymentGateway.Web.Infrastructure.Multitenancy;
using PaymentGateway.Web.Infrastructure.Outbox;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Providers;
using PaymentGateway.Web.Infrastructure.Providers.Paymob;
using PaymentGateway.Web.Infrastructure.Security;
using PaymentGateway.Web.Infrastructure.Startup;

var builder = WebApplication.CreateBuilder(args);

// --- Persistence -------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>((sp, opts) =>
{
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
    if (builder.Environment.IsDevelopment()) opts.EnableSensitiveDataLogging();
});

// --- DataProtection — keys persisted in DB so multi-instance can decrypt -----
builder.Services
    .AddDataProtection()
    .SetApplicationName("PaymentGateway")
    .PersistKeysToDbContext<AppDbContext>();

// --- Multitenancy + Security -------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<ISecretProtector, SecretProtector>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IApiKeyGenerator, ApiKeyGenerator>();

// --- Payment providers -------------------------------------------------------
// Each provider is a typed HttpClient so HttpMessageHandler rotation works.
// Add new providers by:
//   1. implement IPaymentProvider
//   2. add an enum value to PaymentProviderCode
//   3. AddHttpClient<TProvider>() + AddScoped<IPaymentProvider>(sp => sp.GetRequiredService<TProvider>())
builder.Services.AddHttpClient<PaymobProvider>();
builder.Services.AddScoped<IPaymentProvider>(sp => sp.GetRequiredService<PaymobProvider>());
builder.Services.AddScoped<IPaymentProviderRegistry, PaymentProviderRegistry>();

// --- Outbox ------------------------------------------------------------------
builder.Services.AddHttpClient(OutboxDispatcher.HttpClientName);
builder.Services.AddHostedService<OutboxDispatcher>();

// --- Cookie auth (Razor admin) ----------------------------------------------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/auth/login";
        o.LogoutPath = "/auth/logout";
        o.AccessDeniedPath = "/auth/denied";
        o.Cookie.Name = "pg.auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("SuperAdmin", p => p.RequireClaim(CurrentUser.ClaimRole, "1"))
    .AddPolicy("CompanyAdmin", p => p.RequireClaim(CurrentUser.ClaimRole, "2"))
    .AddPolicy("CompanyAny", p => p.RequireClaim(CurrentUser.ClaimRole, "2", "3"));

// --- Razor Pages + folder authz ---------------------------------------------
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/SuperAdmin", "SuperAdmin");
    o.Conventions.AuthorizeFolder("/Company", "CompanyAny");
    o.Conventions.AllowAnonymousToFolder("/Auth");
    o.Conventions.AllowAnonymousToPage("/Index");
    o.Conventions.AllowAnonymousToFolder("/Pay");
})
.AddRazorPagesOptions(o => o.RootDirectory = "/Features");

builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UsePathBase("/PaymentGateway");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapPublicPaymentApis();
app.MapWebhookEndpoints();

await DatabaseBootstrap.RunAsync(app.Services, app.Configuration);

app.Run();
