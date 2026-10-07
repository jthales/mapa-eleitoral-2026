using Election.Application;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Election.Infrastructure;

public sealed class ElectionUpdateWorker(IElectionProvider provider, IElectionCache cache,
    IOptions<ElectionOptions> options, ILogger<ElectionUpdateWorker> logger) : BackgroundService
{
    public async Task<bool> RefreshAsync(CancellationToken cancellationToken, int round = 2)
    {
        try
        {
            var snapshot = await provider.FetchAsync(cancellationToken, round);
            if (snapshot.Brazil.Round != round) throw new InvalidDataException("Turno recebido difere do solicitado.");
            cache.Store(snapshot);
            logger.LogInformation("Snapshot atualizado: {ElectionId}, fonte {Source}, {States} UFs",
                snapshot.Brazil.ElectionId, snapshot.Source, snapshot.States.Count);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            cache.MarkStale(round);
            logger.LogWarning(ex, "Falha de atualização no turno {Round}; preservando o último snapshot válido", round);
            return false;
        }
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextRefresh = new Dictionary<int, DateTimeOffset> { [2] = DateTimeOffset.MinValue, [1] = DateTimeOffset.MinValue };
        while (!stoppingToken.IsCancellationRequested)
        {
            // One scheduler keeps outbound calls sequential, with independent deadlines/backoff.
            foreach (var round in new[] { 2, 1 })
            {
                if (DateTimeOffset.UtcNow < nextRefresh[round]) continue;
                var startedAt = DateTimeOffset.UtcNow;
                var success = await RefreshAsync(stoppingToken, round);
                var settings = options.Value.ForRound(round);
                // Target start-to-start cadence; never overlap or replay missed cycles.
                nextRefresh[round] = success
                    ? startedAt.AddSeconds(settings.RefreshIntervalSeconds)
                    : DateTimeOffset.UtcNow.AddSeconds(settings.FailureBackoffSeconds);
            }
            var delay = nextRefresh.Values.Min() - DateTimeOffset.UtcNow;
            await Task.Delay(delay > TimeSpan.Zero ? delay : TimeSpan.FromMilliseconds(100), stoppingToken);
        }
    }
}
