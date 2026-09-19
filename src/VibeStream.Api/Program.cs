using VibeStream.Api.Extensions;
using VibeStream.Infrastructure.DependencyInjection;

// =================================================================================================
// THE COMPOSITION ROOT.
//
// Top-level statements: the compiler generates the namespace, class and Main. This is the ONE place
// allowed to know about every layer; everything else depends only on abstractions. It stays a table
// of contents — each Add*/Use* call is an extension method defined next to the concern it wires.
// =================================================================================================

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Web layer: controllers + JSON conventions, OpenAPI document, liveness check.
builder.Services
	.AddApiControllers()
	.AddApiDocumentation()
	.AddApiHealthChecks();

// Infrastructure layer: Redis connection + the Postgres/Redis readiness checks.
builder.Services.AddInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

app.UseApiPipeline();

app.Run();

// =================================================================================================
// TEST HOOK. Top-level statements generate an INTERNAL Program; WebApplicationFactory<Program> in the
// integration tests needs it public. This partial declaration widens it without adding members.
// =================================================================================================
public partial class Program
{
}
