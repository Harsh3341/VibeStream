using VibeStream.Infrastructure.DependencyInjection;
using VibeStream.Worker;

// =================================================================================================
// THE WORKER COMPOSITION ROOT.
//
// Same Infrastructure registration as the API (DbContext, Spotify clients, token/ingestion services)
// via one AddInfrastructure call — the point of putting that wiring in Infrastructure. The Worker adds
// only its hosted loop on top. No JWT/auth here: the Worker authenticates to Spotify with stored
// tokens, it never authenticates HTTP callers.
// =================================================================================================

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<IngestionWorker>();

IHost host = builder.Build();

host.Run();
