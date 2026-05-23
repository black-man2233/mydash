using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using MyDash.Application.Options;
using MyDash.Application.Repositories;
using MyDash.Application.Services;
using MyDash.Application.UseCases;
using MyDash.Hub.Components;
using MyDash.Hub.Services;
using MyDash.Infrastructure.BackgroundServices;
using MyDash.Infrastructure.Data;
using MyDash.Infrastructure.GrpcServices;
using MyDash.Infrastructure.Repositories;
using MyDash.Infrastructure.Services;
using MyDash.Infrastructure.Sms;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/hub-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/hub-.log", rollingInterval: RollingInterval.Day));

    builder.Services.AddRazorComponents().AddInteractiveServerComponents();
    builder.Services.AddMudServices();
    builder.Services.AddGrpc();

    var connStr = builder.Configuration["Database:ConnectionString"]
        ?? "Server=localhost;Database=MyDash;User Id=sa;Password=MyDash_Str0ng!;TrustServerCertificate=true;";
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connStr));

    builder.Services.Configure<SmsOptions>(builder.Configuration.GetSection("Sms"));
    builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));
    builder.Services.Configure<HubOptions>(builder.Configuration.GetSection("Hub"));

    builder.Services.AddScoped<IServerRepository, EfServerRepository>();
    builder.Services.AddScoped<IServiceRepository, EfServiceRepository>();
    builder.Services.AddScoped<IEnrollmentTokenRepository, EfEnrollmentTokenRepository>();
    builder.Services.AddScoped<IAuditRepository, EfAuditRepository>();
    builder.Services.AddScoped<IPinChallengeRepository, EfPinChallengeRepository>();
    builder.Services.AddScoped<IUserPreferencesRepository, EfUserPreferencesRepository>();

    builder.Services.AddScoped<IAuditWriter, AuditWriter>();
    builder.Services.AddScoped<RequestPinHandler>();
    builder.Services.AddScoped<VerifyPinHandler>();

    var smsProvider = builder.Configuration["Sms:Provider"] ?? "TextBelt";
    builder.Services.AddHttpClient<TextBeltSmsSender>();
    switch (smsProvider)
    {
        case "Twilio":
            builder.Services.AddScoped<ISmsSender, TwilioSmsSender>();
            break;
        case "Vonage":
            builder.Services.AddHttpClient<VonageSmsSender>();
            builder.Services.AddScoped<ISmsSender, VonageSmsSender>();
            break;
        case "ClickSend":
            builder.Services.AddHttpClient<ClickSendSmsSender>();
            builder.Services.AddScoped<ISmsSender, ClickSendSmsSender>();
            break;
        case "SMSAPI":
            builder.Services.AddHttpClient<SmsApiSmsSender>();
            builder.Services.AddScoped<ISmsSender, SmsApiSmsSender>();
            break;
        default:
            builder.Services.AddScoped<ISmsSender, TextBeltSmsSender>();
            break;
    }

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(o =>
        {
            o.LoginPath = "/login";
            o.AccessDeniedPath = "/login";
            o.SlidingExpiration = true;
            var hours = builder.Configuration.GetValue<int>("Security:SessionCookieHours", 12);
            o.ExpireTimeSpan = TimeSpan.FromHours(hours);
        });
    builder.Services.AddAuthorization();
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSingleton<PendingAuthStore>();

    builder.Services.AddHostedService<AgentHeartbeatWatchdog>();
    builder.Services.AddHostedService<EnrollmentTokenJanitor>();
    builder.Services.AddHostedService<PinChallengeJanitor>();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
        app.UseExceptionHandler("/Error", createScopeForErrors: true);

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    // Blazor interactive components run over WebSocket and cannot set cookies.
    // Login.razor issues a 30-second one-time token and force-navigates here
    // so SignInAsync runs on a real HTTP request.
    app.MapGet("/auth/signin", async (string t, HttpContext ctx, PendingAuthStore store) =>
    {
        if (!store.Consume(t))
            return Results.Redirect("/login");

        var claims = new[] { new Claim(ClaimTypes.Name, "admin") };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Results.Redirect("/");
    }).AllowAnonymous();

    app.MapGrpcService<AuthGrpcService>();
    app.MapGrpcService<FleetGrpcService>();
    app.MapGrpcService<AgentGrpcService>();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Hub terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
