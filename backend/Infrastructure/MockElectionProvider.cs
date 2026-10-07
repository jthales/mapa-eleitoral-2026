using Election.Application;
using Election.Domain;
using Microsoft.Extensions.Options;

namespace Election.Infrastructure;

public sealed class MockElectionProvider(TimeProvider clock, IOptions<ElectionOptions> options) : IElectionProvider
{
    public Task<ElectionSnapshot> FetchAsync(CancellationToken cancellationToken, int? round = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = clock.GetUtcNow();
        var selectedRound = round ?? options.Value.Round;
        var states = BrazilStates.All.Keys.Select((uf, i) =>
        {
            var total = 1800L + i * 137;
            var processed = i % 11 == 0 ? 0 : total * (65 + i % 32) / 100;
            var votes = processed * 190;
            var a = votes * (42 + i * 7 % 18) / 100;
            return Tally(uf, total, processed, a, votes - a);
        }).ToArray();
        var brazil = Tally("BR", states.Sum(s => s.TotalSections), states.Sum(s => s.ProcessedSections),
            states.Sum(s => s.Candidates[0].Votes), states.Sum(s => s.Candidates[1].Votes));
        return Task.FromResult(new ElectionSnapshot(brazil, states, "mock", now));

        ElectionTally Tally(string scope, long total, long processed, long a, long b) =>
            new($"mock-2026-{selectedRound}", selectedRound, scope, processed == 0 ? "not_started" : "in_progress",
                now, total, processed,
                [new("mock-a", "90", "Candidato A", "Fictício A", a, ElectionMath.Percentage(a, a+b)),
                 new("mock-b", "91", "Candidato B", "Fictício B", b, ElectionMath.Percentage(b, a+b))]);
    }
}
