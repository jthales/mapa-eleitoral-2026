using Election.Application;
using Election.Domain;
using Microsoft.Extensions.Options;

namespace Election.Infrastructure.Tse;

public sealed class TseElectionProvider(TseHttpClient client, TseConfigurationParser configurationParser,
    TseResultParser resultParser, IOptions<ElectionOptions> options, TimeProvider clock, TseCandidatePhotos photos) : IElectionProvider
{
    public async Task<ElectionSnapshot> FetchAsync(CancellationToken cancellationToken, int? round = null)
    {
        var o = options.Value.ForRound(round ?? options.Value.Round);
        var json = await client.GetAsync(new Uri($"{o.BaseUrl.TrimEnd('/')}/{o.Environment}/comum/config/ele-c.json"), cancellationToken);
        var config = configurationParser.Parse(json, o);
        var brazil = await ReadAsync("BR");
        var states = new List<ElectionTally>();
        foreach (var uf in BrazilStates.All.Keys) states.Add(await ReadAsync(uf));
        await photos.WarmAsync(brazil, config, o, cancellationToken);
        ElectionTally WithPhotos(ElectionTally tally) => tally with
        {
            Candidates = tally.Candidates.Select(c => c with
            {
                PhotoUrl = photos.Read(o.ElectionId, c.Id) is null ? null : $"/api/election/candidates/{c.Id}/photo?round={o.Round}"
            }).ToArray()
        };
        brazil = WithPhotos(brazil);
        states = states.Select(WithPhotos).ToList();
        // Publish only a fully validated refresh. A failure preserves the previous snapshot.
        return new(brazil, states.AsReadOnly(), "tse", clock.GetUtcNow());

        async Task<ElectionTally> ReadAsync(string scope)
        {
            var uri = configurationParser.ResultUri(config, o, scope);
            var payload = await client.GetAsync(uri, cancellationToken);
            return resultParser.Parse(payload, o.ElectionId, o.Round, scope, TimeSpan.FromMinutes(o.SourceUtcOffsetMinutes));
        }
    }
}
