using Microsoft.Extensions.Options;
using MyDash.Agent.Discovery;
using MyDash.Agent.Services;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((_, lc) => lc.WriteTo.Console().WriteTo.File("logs/agent-.log", rollingInterval: RollingInterval.Day));

builder.Services.Configure<AgentOptions>(o =>
{
    o.HubUrl = Environment.GetEnvironmentVariable("MYDASH_HUB")
        ?? builder.Configuration["Agent:HubUrl"]
        ?? "http://localhost:8080";
    o.Token = Environment.GetEnvironmentVariable("MYDASH_TOKEN")
        ?? builder.Configuration["Agent:Token"]
        ?? "";
    o.Name = Environment.GetEnvironmentVariable("MYDASH_NAME")
        ?? builder.Configuration["Agent:Name"]
        ?? Environment.MachineName;
    o.CertPath = Environment.GetEnvironmentVariable("MYDASH_CERT_PATH")
        ?? builder.Configuration["Agent:CertPath"]
        ?? "/var/lib/mydash-agent/cert.pem";
});

builder.Services.AddSingleton<ServiceDiscovery>();
builder.Services.AddHostedService<AgentWorker>();

var host = builder.Build();
host.Run();
