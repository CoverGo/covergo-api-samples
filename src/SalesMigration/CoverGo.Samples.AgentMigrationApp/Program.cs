using CoverGo.Samples.Application;
using CoverGo.Samples.Domain;
using CoverGo.Samples.Domain.Agents;
using CoverGo.Samples.Infrastructure;
using CoverGo.Samples.Infrastructure.Agents;
using CoverGo.Samples.Infrastructure.Authentication;
using CoverGo.Samples.Infrastructure.GatewayV1Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Loads brokers into CoverGo as agents.
//
//   dotnet run                       -- reads data/agents.sample.json
//   dotnet run -- --in-code          -- builds one agent in code instead
//   dotnet run -- --file <path>      -- reads a different extract
//
// Credentials come from user secrets or the environment; nothing secret is committed.

bool inCode = args.Contains("--in-code");

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddCoverGoSamples(builder.Configuration);

CoverGoOptions options = builder.Configuration
    .GetSection(CoverGoOptions.SectionName)
    .Get<CoverGoOptions>() ?? throw new InvalidOperationException(
        $"Configuration section '{CoverGoOptions.SectionName}' is missing.");

builder.Services.AddCoverGoGatewayV1<CoverGoTokenHandler>(options.GatewayV1Url
    ?? throw new InvalidOperationException("CoverGo:GatewayV1Url is not configured."));

builder.Services.AddSingleton<IMigrationTarget<Agent>, GatewayAgentTarget>();
builder.Services.AddSingleton<MigrationRunner<Agent>>();

if (inCode)
{
    builder.Services.AddSingleton<IAgentSource, InCodeAgentSource>();
}
else
{
    int flag = Array.IndexOf(args, "--file");
    string extract = flag >= 0 && flag + 1 < args.Length
        ? args[flag + 1]
        : Path.Combine(AppContext.BaseDirectory, "data", "agents.sample.json");
    builder.Services.AddSingleton<IAgentSource>(_ => new JsonFileAgentSource(extract));
}

// MigrationRunner takes the generic source; the per-entity interface is what callers register.
builder.Services.AddSingleton<IMigrationSource<Agent>>(sp => sp.GetRequiredService<IAgentSource>());

using IHost host = builder.Build();

ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation(
    "Loading agents into tenant {TenantId} via {Gateway} from {Source}",
    options.TenantId, options.GatewayV1Url, inCode ? "code" : "a JSON extract");

MigrationRunner<Agent> runner = host.Services.GetRequiredService<MigrationRunner<Agent>>();

try
{
    MigrationReport report = await runner.RunAsync();

    // A partial load is a normal outcome, not a crash: rejected records are reported per row
    // so they can be fixed in the extract and re-run. Exit non-zero so a pipeline notices.
    foreach (LoadResult failure in report.Failures)
    {
        logger.LogError("Fix and re-run {SourceKey}: {Error}", failure.SourceKey, failure.Error);
    }

    logger.LogInformation(
        "Done. {Created} created, {Existed} already present, {Failed} failed",
        report.Created, report.AlreadyExisted, report.Failed);

    return report.AnyFailed ? 1 : 0;
}
catch (Exception ex)
{
    // Thrown only when the whole call failed -- transport, or authorization.
    logger.LogError(ex, "Agent load could not run");
    return 1;
}
