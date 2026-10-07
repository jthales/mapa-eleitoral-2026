using Election.Domain;

namespace Election.Application;

// Read side: requests never initiate outbound TSE calls.
public sealed class TseElectionService(IElectionCache cache) : ITseElectionService
{
    public Task<BrazilElectionResultDto> GetBrazilResultAsync(CancellationToken cancellationToken = default, int round = 2)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (round == 2 && cache.Read(2) is null) return Task.FromResult(Preview());
        var entry = Read(round);
        var r = entry.Snapshot.Brazil;
        return Task.FromResult(new BrazilElectionResultDto(r.ElectionId, r.Round, r.Status, r.UpdatedAt,
            entry.Snapshot.FetchedAt, r.ProcessedPercentage, r.TotalSections, r.ProcessedSections,
            Candidates(r), entry.Snapshot.Source, entry.IsStale ? "stale" : "fresh", r.HasData, r.VotesReleased));
    }

    public Task<IReadOnlyCollection<StateElectionResultDto>> GetStatesAsync(CancellationToken cancellationToken = default, int round = 2)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (round == 2 && cache.Read(2) is null)
        {
            var preview = Preview();
            return Task.FromResult<IReadOnlyCollection<StateElectionResultDto>>(BrazilStates.All.Keys.Select(uf => PreviewState(uf, preview)).ToArray());
        }
        var entry = Read(round);
        return Task.FromResult<IReadOnlyCollection<StateElectionResultDto>>(entry.Snapshot.States.Select(r => State(r, entry)).ToArray());
    }

    public Task<StateElectionDetailsDto?> GetStateAsync(string uf, CancellationToken cancellationToken = default, int round = 2)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!BrazilStates.All.ContainsKey(uf.ToUpperInvariant())) return Task.FromResult<StateElectionDetailsDto?>(null);
        if (round == 2 && cache.Read(2) is null) return Task.FromResult<StateElectionDetailsDto?>(new(PreviewState(uf.ToUpperInvariant(), Preview())));
        var entry = Read(round);
        var result = entry.Snapshot.States.FirstOrDefault(s => s.Scope.Equals(uf, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(result is null ? null : new StateElectionDetailsDto(State(result, entry)));
    }

    // A candidate list derived from confirmed first-round status, never second-round results.
    private BrazilElectionResultDto Preview()
    {
        var entry = Read(1);
        var first = entry.Snapshot.Brazil;
        if (entry.Snapshot.Source != "tse" || first.Round != 1 || first.Status != "completed") throw new ElectionUnavailableException();
        var qualified = first.Candidates.Where(c => c.OfficialStatus?.Trim().Equals("2º turno", StringComparison.OrdinalIgnoreCase) == true)
            .OrderBy(c => c.Name, StringComparer.Ordinal).ToArray();
        if (qualified.Length != 2) throw new ElectionUnavailableException();
        var candidates = qualified.Select(c => new CandidateResultDto(c.Id, c.Number, c.Name, c.Party,
            null, null, c.OfficialStatus, c.PhotoUrl)).ToArray();
        return new(null, 2, "not_started", first.UpdatedAt, entry.Snapshot.FetchedAt, null, null, null,
            candidates, "tse", entry.IsStale ? "stale" : "fresh", false, false, true);
    }
    private static StateElectionResultDto PreviewState(string uf, BrazilElectionResultDto preview) =>
        new(uf, BrazilStates.All[uf], null, 2, "not_started", preview.UpdatedAt, null, preview.Candidates,
            null, null, preview.Source, preview.DataStatus, false, false, true);

    private CachedElection Read(int round) => cache.Read(round) ?? throw new ElectionUnavailableException();
    private static CandidateResultDto[] Candidates(ElectionTally tally) => tally.Candidates.Select(c =>
        new CandidateResultDto(c.Id, c.Number, c.Name, c.Party, c.Votes, c.Percentage, c.OfficialStatus, c.PhotoUrl)).ToArray();
    private static StateElectionResultDto State(ElectionTally tally, CachedElection entry)
    {
        var (leader, margin) = tally.HasData ? ElectionMath.Leadership(tally.Candidates) : (null, 0m);
        return new(tally.Scope, BrazilStates.All[tally.Scope], tally.ElectionId, tally.Round,
            tally.Status, tally.UpdatedAt, tally.ProcessedPercentage, Candidates(tally), leader, margin,
            entry.Snapshot.Source, entry.IsStale ? "stale" : "fresh", tally.HasData, tally.VotesReleased);
    }
}
