using Election.Application;
using Election.Domain;
using Election.Infrastructure;
using Election.Infrastructure.Tse;

var builder = WebApplication.CreateBuilder(args);
// Container platforms supply PORT; local development keeps appsettings.json's URL.
if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port) && port is > 0 and <= 65535)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
builder.Services.AddProblemDetails();
builder.Services.AddOptions<ElectionOptions>().BindConfiguration("Election")
    .Validate(o => o.RefreshIntervalSeconds >= 10 && o.StaleAfterSeconds >= o.RefreshIntervalSeconds &&
        o.FailureBackoffSeconds >= 600 && o.HttpTimeoutSeconds is >= 1 and <= 60 &&
        o.RetryCount is >= 0 and <= 3 && o.RequestSpacingMilliseconds >= 100 && o.Round is 1 or 2 &&
        o.SourceUtcOffsetMinutes is >= -840 and <= 840 && o.FirstRoundRefreshIntervalSeconds >= 30 &&
        o.FirstRoundStaleAfterSeconds >= o.FirstRoundRefreshIntervalSeconds, "Configuração Election inválida.")
    .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https" &&
        uri.Host == "resultados.tse.jus.br" && uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo) &&
        o.Environment == "oficial", "O provedor real deve usar a origem oficial do TSE.")
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IElectionCache, MemoryElectionCache>();
builder.Services.AddSingleton<ITseElectionService, TseElectionService>();
builder.Services.AddSingleton<TseConfigurationParser>();
builder.Services.AddSingleton<TseResultParser>();
builder.Services.AddSingleton<TseHttpClient>();
builder.Services.AddSingleton<TseCandidatePhotos>();
builder.Services.AddHttpClient("Tse", (sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ElectionOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MapaEleitoral2026/0.1");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton<MockElectionProvider>();
builder.Services.AddSingleton<TseElectionProvider>();
builder.Services.AddSingleton<IElectionProvider, ConfiguredElectionProvider>();
builder.Services.AddSingleton<ElectionWarmup>();
builder.Services.AddHostedService<ElectionUpdateWorker>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    // Keep a cold instance active while its existing worker prepares the shared snapshot.
    // Invalid round queries retain their immediate 400 response.
    var roundText = context.Request.Query["round"].ToString();
    if (context.Request.Path.StartsWithSegments("/api/election") &&
        (roundText.Length == 0 || roundText is "1" or "2"))
    {
        var cache = context.RequestServices.GetRequiredService<IElectionCache>();
        var round = roundText == "1" ? 1 : 2;
        if (cache.Read(round) is null && cache.Read(1) is null)
            await context.RequestServices.GetRequiredService<ElectionWarmup>()
                .WaitAsync(TimeSpan.FromSeconds(25), context.RequestAborted);
    }
    await next(context);
});
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    try { await next(context); }
    catch (ElectionUnavailableException)
    {
        context.Response.Headers.RetryAfter = "30";
        await Results.Problem(statusCode: 503, title: "Dados eleitorais indisponíveis",
            detail: "Ainda não há dados válidos. Aguarde a próxima atualização.").ExecuteAsync(context);
    }
});
app.MapGet("/api/election", async (int? round, ITseElectionService service, CancellationToken ct) =>
    round is not (null or 1 or 2) ? InvalidRound() : Results.Ok(await service.GetBrazilResultAsync(ct, round ?? 2)));
app.MapGet("/api/election/states", async (int? round, ITseElectionService service, CancellationToken ct) =>
    round is not (null or 1 or 2) ? InvalidRound() : Results.Ok(await service.GetStatesAsync(ct, round ?? 2)));
app.MapGet("/api/election/states/{uf}", async (string uf, int? round, ITseElectionService service, CancellationToken ct) =>
{
    if (round is not (null or 1 or 2)) return InvalidRound();
    if (!BrazilStates.All.ContainsKey(uf.ToUpperInvariant()))
        return Results.Problem(statusCode: 400, title: "UF inválida", detail: "Informe uma das 27 siglas de UF.");
    var state = await service.GetStateAsync(uf, ct, round ?? 2);
    return state is null ? Results.NotFound() : Results.Ok(state);
});
app.MapGet("/api/election/candidates/{candidateId}/photo", (string candidateId, int? round, IElectionCache cache, TseCandidatePhotos photos) =>
{
    if (round is not (null or 1 or 2)) return InvalidRound();
    var snapshot = cache.Read(round ?? 2)?.Snapshot;
    if (snapshot is null || snapshot.Source != "tse" || !snapshot.Brazil.Candidates.Any(c => c.Id == candidateId))
        return Results.NotFound();
    var bytes = photos.Read(snapshot.Brazil.ElectionId, candidateId);
    return bytes is null ? Results.NotFound() : Results.File(bytes, "image/jpeg");
});
app.MapGet("/health", (int? round, IElectionCache cache) => round is not (null or 1 or 2) ? InvalidRound() : cache.Read(round ?? 2) is { } entry
    ? Results.Ok(new { status = entry.IsStale ? "degraded" : "ok", source = entry.Snapshot.Source })
    : Results.Problem(statusCode: 503, title: "Aguardando primeiro snapshot"));
app.MapGet("/api/{**path}", () => Results.NotFound());
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapFallbackToFile("index.html");
app.Run();

static IResult InvalidRound() => Results.Problem(statusCode: 400, title: "Turno inválido", detail: "Informe round=1 ou round=2.");

public partial class Program;
