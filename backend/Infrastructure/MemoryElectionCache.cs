using Election.Application;
using Election.Domain;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Election.Infrastructure;

public sealed class MemoryElectionCache(IMemoryCache memory, TimeProvider clock, IOptions<ElectionOptions> options) : IElectionCache
{
    private readonly object gate = new();
    private static string Key(int round) => $"election-snapshot-round-{round}";
    public CachedElection? Read(int round = 2)
    {
        lock (gate)
        {
            var entry = memory.Get<CachedElection>(Key(round));
            return entry is null ? null : entry with
            {
                IsStale = entry.IsStale || clock.GetUtcNow() - entry.Snapshot.FetchedAt > TimeSpan.FromSeconds(options.Value.ForRound(round).StaleAfterSeconds)
            };
        }
    }
    public void Store(ElectionSnapshot snapshot)
    {
        lock (gate)
        {
            var round = snapshot.Brazil.Round;
            if (round is not (1 or 2) || snapshot.States.Any(s => s.Round != round || s.ElectionId != snapshot.Brazil.ElectionId))
                throw new InvalidDataException("Snapshot mistura eleições ou turnos.");
            var old = memory.Get<CachedElection>(Key(round))?.Snapshot;
            if (old is not null && old.Brazil.ElectionId == snapshot.Brazil.ElectionId &&
                (snapshot.Brazil.UpdatedAt < old.Brazil.UpdatedAt || snapshot.States.Any(s =>
                    old.States.Any(o => o.Scope == s.Scope && o.UpdatedAt > s.UpdatedAt))))
                throw new InvalidDataException("Snapshot anterior ao último dado válido.");
            // No TTL: retain last valid data during extended upstream outages.
            memory.Set(Key(round), new CachedElection(snapshot, false));
        }
    }
    public void MarkStale(int round = 2)
    {
        lock (gate)
        {
            var entry = memory.Get<CachedElection>(Key(round));
            if (entry is not null) memory.Set(Key(round), entry with { IsStale = true });
        }
    }
}
