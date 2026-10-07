using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Election.Application;
using Election.Domain;
using Election.Infrastructure;
using Election.Infrastructure.Tse;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Election.Tests;

public class ElectionTests
{
    private static readonly IOptions<ElectionOptions> Options = Microsoft.Extensions.Options.Options.Create(new ElectionOptions());
    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 3, 33.33)]
    [InlineData(100, 100, 100)]
    public void PercentagesHandleZeroAndRounding(long value, long total, decimal expected) =>
        Assert.Equal(expected, ElectionMath.Percentage(value, total));

    [Fact]
    public void LeadershipIsNotVictoryAndTieHasNoLeader()
    {
        CandidateResult[] candidates = [new("a", "90", "Candidato A", "A", 510, 51), new("b", "91", "Candidato B", "B", 490, 49)];
        Assert.Equal(("a", 2m), ElectionMath.Leadership(candidates));
        Assert.Equal((null, 0m), ElectionMath.Leadership([candidates[0], candidates[1] with { Votes = 510, Percentage = 51 }]));
    }

    [Fact]
    public void ParsesOfficialEa20FixtureWithoutHardcodedCandidates()
    {
        var json = Fixture("ea20-6257-br.json");
        var root = JsonNode.Parse(json)!;
        var parsed = new TseResultParser().Parse(json, "6257", 1, "BR", TimeSpan.FromHours(-3));
        Assert.Equal(long.Parse(root["s"]!["st"]!.ToString()), parsed.ProcessedSections);
        Assert.NotEmpty(parsed.Candidates);
        Assert.All(parsed.Candidates, c => Assert.InRange(c.Percentage, 0m, 100m));
        Assert.Equal("completed", parsed.Status);
        Assert.Contains(parsed.Candidates, c => c.OfficialStatus == "2º turno");
    }

    [Fact]
    public void RejectsWrongRoundAndInvalidContent()
    {
        var parser = new TseResultParser();
        Assert.Throws<InvalidDataException>(() => parser.Parse(Fixture("ea20-6257-br.json"), "6257", 2, "BR", TimeSpan.Zero));
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => parser.Parse("not JSON", "6257", 1, "BR", TimeSpan.Zero));
        var payload = JsonNode.Parse(Fixture("ea20-6257-br.json"))!;
        payload["s"]!["st"] = "999999999";
        Assert.Throws<InvalidDataException>(() => parser.Parse(payload.ToJsonString(), "6257", 1, "BR", TimeSpan.Zero));
    }

    [Fact]
    public void DoesNotPublishWithheldVotesOrPrematureOfficialStatus()
    {
        var payload = JsonNode.Parse(Fixture("ea20-6257-br.json"))!;
        payload["dv"] = "n";
        payload["tf"] = "n";
        var parsed = new TseResultParser().Parse(payload.ToJsonString(), "6257", 1, "BR", TimeSpan.Zero);
        Assert.False(parsed.HasData);
        Assert.All(parsed.Candidates, c => { Assert.Equal(0, c.Votes); Assert.Equal(0, c.Percentage); Assert.Null(c.OfficialStatus); });
    }

    [Fact]
    public void DiscoversRoundTwoFromOfficialConfiguration()
    {
        var parser = new TseConfigurationParser();
        var config = parser.Parse(Fixture("ea11.json"), Options.Value);
        Assert.Equal("6258", config.ElectionId);
        Assert.Equal("https://resultados.tse.jus.br/oficial/ele2026/6258/dados/sp/sp-c0001-e006258-u.json",
            parser.ResultUri(config, Options.Value, "SP").ToString());
        Assert.Throws<InvalidDataException>(() => parser.ResultUri(config with { DirectoryTemplate = "https://example.com/<uf>" }, Options.Value, "SP"));
    }

    [Fact]
    public async Task MockIncludesAllStatesAndConsistentNationalTotals()
    {
        var snapshot = await new MockElectionProvider(TimeProvider.System, Options).FetchAsync(default);
        Assert.Equal(27, snapshot.States.Count);
        Assert.Equal(27, snapshot.States.Select(s => s.Scope).Distinct().Count());
        Assert.Contains(snapshot.States, s => !s.HasData);
        Assert.Equal(snapshot.States.Sum(s => s.ProcessedSections), snapshot.Brazil.ProcessedSections);
        Assert.All(snapshot.States, s => Assert.Equal(["Candidato A", "Candidato B"], s.Candidates.Select(c => c.Name)));
    }

    [Fact]
    public async Task CacheAgesAndRetainsLastSnapshotOnFailure()
    {
        var clock = new TestClock();
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new MemoryElectionCache(memory, clock, Options);
        Assert.Null(cache.Read());
        var snapshot = await new MockElectionProvider(clock, Options).FetchAsync(default);
        cache.Store(snapshot);
        Assert.False(cache.Read()!.IsStale);
        clock.Now += TimeSpan.FromSeconds(91);
        Assert.True(cache.Read()!.IsStale);
        cache.Store(snapshot with { FetchedAt = clock.Now });
        Assert.False(cache.Read()!.IsStale);
        var worker = new ElectionUpdateWorker(new FailingProvider(), cache, Options, NullLogger<ElectionUpdateWorker>.Instance, new ElectionWarmup());
        Assert.False(await worker.RefreshAsync(default));
        Assert.True(cache.Read()!.IsStale);
        Assert.Equal(snapshot.Brazil, cache.Read()!.Snapshot.Brazil);
        Assert.Throws<InvalidDataException>(() => cache.Store(snapshot with { Brazil = snapshot.Brazil with { UpdatedAt = snapshot.Brazil.UpdatedAt.AddMinutes(-1) } }));
    }

    [Fact]
    public async Task HttpClientRetriesServerErrorsButNot404()
    {
        var settings = Microsoft.Extensions.Options.Options.Create(new ElectionOptions { RequestSpacingMilliseconds = 0, RetryCount = 1 });
        var handler = new SequenceHandler(HttpStatusCode.InternalServerError, HttpStatusCode.OK);
        var client = new TseHttpClient(new TestFactory(handler), settings, NullLogger<TseHttpClient>.Instance);
        Assert.Equal("{}", await client.GetAsync(new Uri("https://resultados.tse.jus.br/test"), default));
        Assert.Equal(2, handler.Calls);
        var missing = new SequenceHandler(HttpStatusCode.NotFound);
        var missingClient = new TseHttpClient(new TestFactory(missing), settings, NullLogger<TseHttpClient>.Instance);
        await Assert.ThrowsAsync<HttpRequestException>(() => missingClient.GetAsync(new Uri("https://resultados.tse.jus.br/test"), default));
        Assert.Equal(1, missing.Calls);
    }

    [Fact]
    public async Task ApiValidatesUfAndReturnsUnavailableBeforeFirstSnapshot()
    {
        await using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            var workers = services.Where(d => d.ServiceType == typeof(IHostedService)).ToArray();
            foreach (var worker in workers) services.Remove(worker);
        }));
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/election/states/XX")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/election")).StatusCode);
        var cache = app.Services.GetRequiredService<IElectionCache>();
        cache.Store(await new MockElectionProvider(TimeProvider.System, Options).FetchAsync(default));
        var brazil = await client.GetFromJsonAsync<BrazilElectionResultDto>("/api/election");
        Assert.Equal("mock", brazil!.Source);
        Assert.Equal(27, (await client.GetFromJsonAsync<StateElectionResultDto[]>("/api/election/states"))!.Length);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/election/states/sp")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/election?round=1")).StatusCode);
        cache.Store(await new MockElectionProvider(TimeProvider.System, Options).FetchAsync(default, 1));
        Assert.Equal(1, (await client.GetFromJsonAsync<BrazilElectionResultDto>("/api/election?round=1"))!.Round);
        Assert.Equal(2, (await client.GetFromJsonAsync<BrazilElectionResultDto>("/api/election?round=2"))!.Round);
        Assert.All((await client.GetFromJsonAsync<StateElectionResultDto[]>("/api/election/states?round=1"))!, s => Assert.Equal(1, s.Round));
        foreach (var endpoint in new[] { "/api/election", "/api/election/states", "/api/election/states/SP", "/health" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(endpoint + "?round=3")).StatusCode);
        cache.MarkStale();
        Assert.Equal("stale", (await client.GetFromJsonAsync<BrazilElectionResultDto>("/api/election"))!.DataStatus);
    }

    [Fact]
    public async Task RoundCachesAndFailuresAreIndependent()
    {
        var clock = new TestClock();
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new MemoryElectionCache(memory, clock, Options);
        var provider = new MockElectionProvider(clock, Options);
        cache.Store(await provider.FetchAsync(default, 2));
        Assert.Null(cache.Read(1));
        cache.Store(await provider.FetchAsync(default, 1));
        Assert.Equal(1, cache.Read(1)!.Snapshot.Brazil.Round);
        Assert.Equal(2, cache.Read()!.Snapshot.Brazil.Round);
        var worker = new ElectionUpdateWorker(new FailingProvider(), cache, Options, NullLogger<ElectionUpdateWorker>.Instance, new ElectionWarmup());
        Assert.False(await worker.RefreshAsync(default, 1));
        Assert.True(cache.Read(1)!.IsStale);
        Assert.False(cache.Read(2)!.IsStale);
        cache.Store(await provider.FetchAsync(default, 1));
        clock.Now += TimeSpan.FromSeconds(91);
        Assert.False(cache.Read(1)!.IsStale);
        Assert.True(cache.Read(2)!.IsStale);
    }

    [Fact]
    public async Task PhotosUseOfficialTemplateAndCacheWithoutRetryingMissingFiles()
    {
        var parser = new TseConfigurationParser();
        var settings = Microsoft.Extensions.Options.Options.Create(new ElectionOptions { RequestSpacingMilliseconds = 0 });
        var config = parser.Parse(Fixture("ea11.json"), settings.Value.ForRound(1));
        Assert.Equal("https://resultados.tse.jus.br/oficial/ele2026/6257/fotos/br/123.jpeg",
            parser.PhotoUri(config, settings.Value.ForRound(1), "123")!.ToString());
        Assert.Throws<InvalidDataException>(() => parser.PhotoUri(config, settings.Value, "../other"));
        Assert.Throws<InvalidDataException>(() => parser.PhotoUri(config with { PhotoDirectoryTemplate = "https://example.com" }, settings.Value, "123"));
        var tally = new TseResultParser().Parse(Fixture("ea20-6257-br.json"), "6257", 1, "BR", TimeSpan.Zero);
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var handler = new PhotoHandler();
        var http = new TseHttpClient(new TestFactory(handler), settings, NullLogger<TseHttpClient>.Instance);
        var photos = new TseCandidatePhotos(http, parser, memory, NullLogger<TseCandidatePhotos>.Instance);
        await photos.WarmAsync(tally, config, settings.Value, default);
        Assert.Equal(tally.Candidates.Count, handler.Calls);
        await photos.WarmAsync(tally, config, settings.Value, default);
        Assert.Equal(tally.Candidates.Count, handler.Calls);
        Assert.NotNull(photos.Read("6257", tally.Candidates[0].Id));
        Assert.Null(photos.Read("6258", tally.Candidates[0].Id));
        using var missingMemory = new MemoryCache(new MemoryCacheOptions());
        var missing = new SequenceHandler(HttpStatusCode.NotFound);
        var missingPhotos = new TseCandidatePhotos(new TseHttpClient(new TestFactory(missing), settings, NullLogger<TseHttpClient>.Instance),
            parser, missingMemory, NullLogger<TseCandidatePhotos>.Instance);
        await missingPhotos.WarmAsync(tally, config, settings.Value, default);
        await missingPhotos.WarmAsync(tally, config, settings.Value, default);
        Assert.Equal(1, missing.Calls);
        Assert.Null(missingPhotos.Read("6257", tally.Candidates[0].Id));
        await Assert.ThrowsAsync<InvalidDataException>(() => new TseHttpClient(new TestFactory(new SequenceHandler(HttpStatusCode.OK)), settings,
            NullLogger<TseHttpClient>.Instance).GetPhotoAsync(new Uri("https://resultados.tse.jus.br/test.jpeg"), default));
    }

    private sealed class PhotoHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var content = new ByteArrayContent([0xff, 0xd8, 0xff, 0xd9]);
            content.Headers.ContentType = new("image/jpeg");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }

    [Fact]
    public async Task RunoffPreviewUsesOnlyOfficialQualificationAndNeverCopiesVotes()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new MemoryElectionCache(memory, TimeProvider.System, Options);
        var service = new TseElectionService(cache);
        var first = new TseResultParser().Parse(Fixture("ea20-6257-br.json"), "6257", 1, "BR", TimeSpan.Zero);
        var snapshot = new ElectionSnapshot(first, [], "tse", DateTimeOffset.UtcNow);
        cache.Store(snapshot with { Source = "mock" });
        await Assert.ThrowsAsync<ElectionUnavailableException>(() => service.GetBrazilResultAsync(default, 2));
        cache.Store(snapshot with { Brazil = first with { Status = "in_progress" } });
        await Assert.ThrowsAsync<ElectionUnavailableException>(() => service.GetBrazilResultAsync(default, 2));
        cache.Store(snapshot);
        var preview = await service.GetBrazilResultAsync(default, 2);
        Assert.True(preview.CandidatesOnly);
        Assert.False(preview.HasData);
        Assert.Equal(2, preview.Round);
        Assert.Null(preview.ElectionId);
        Assert.Null(preview.TotalSections);
        Assert.Null(preview.ProcessedPercentage);
        Assert.Equal(2, preview.Candidates.Count);
        Assert.All(preview.Candidates, c => { Assert.Null(c.Votes); Assert.Null(c.Percentage); Assert.Equal("2º turno", c.OfficialStatus); });
        Assert.Equal(first.Candidates.Where(c => c.OfficialStatus == "2º turno").Select(c => c.Id).Order(), preview.Candidates.Select(c => c.Id).Order());
        Assert.Null(cache.Read(2));
        var states = await service.GetStatesAsync(default, 2);
        Assert.Equal(27, states.Count);
        Assert.All(states, s => { Assert.True(s.CandidatesOnly); Assert.Null(s.LeadingCandidateId); Assert.Null(s.ProcessedPercentage); Assert.Null(s.MarginPercentagePoints); });
        Assert.True((await service.GetStateAsync("sp", default, 2))!.Result.CandidatesOnly);
        cache.MarkStale(1);
        Assert.Equal("stale", (await service.GetBrazilResultAsync(default, 2)).DataStatus);
        var second = await new MockElectionProvider(TimeProvider.System, Options).FetchAsync(default, 2);
        cache.Store(second with { Source = "tse" });
        var actual = await service.GetBrazilResultAsync(default, 2);
        Assert.False(actual.CandidatesOnly);
        Assert.NotNull(actual.ProcessedPercentage);
        Assert.Equal(second.Brazil.Candidates[0].Votes, actual.Candidates[0].Votes);
        Assert.False((await service.GetBrazilResultAsync(default, 1)).CandidatesOnly);
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class FailingProvider : IElectionProvider
    {
        public Task<ElectionSnapshot> FetchAsync(CancellationToken cancellationToken, int? round = null) => throw new HttpRequestException("Test outage");
    }
    private sealed class SequenceHandler(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statuses[Math.Min(Calls++, statuses.Length - 1)]) { Content = new StringContent("{}") });
    }
    private sealed class TestFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
