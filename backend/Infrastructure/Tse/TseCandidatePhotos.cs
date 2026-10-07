using Election.Domain;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Election.Infrastructure.Tse;

// Only the background provider downloads photos. HTTP routes read this cache.
public sealed class TseCandidatePhotos(TseHttpClient client, TseConfigurationParser parser,
    IMemoryCache cache, ILogger<TseCandidatePhotos> logger)
{
    private static string Key(string electionId, string candidateId) => $"photo-{electionId}-{candidateId}";
    public byte[]? Read(string electionId, string candidateId) => cache.Get<byte[]>(Key(electionId, candidateId));

    public async Task WarmAsync(ElectionTally brazil, TseElectionConfiguration configuration,
        ElectionOptions options, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue("photo-backoff", out _)) return;
        foreach (var candidate in brazil.Candidates)
        {
            var key = Key(brazil.ElectionId, candidate.Id);
            if (cache.TryGetValue(key, out _) || cache.TryGetValue(key + "-missing", out _)) continue;
            try
            {
                var uri = parser.PhotoUri(configuration, options, candidate.Id);
                if (uri is null) return;
                var bytes = await client.GetPhotoAsync(uri, cancellationToken);
                cache.Set(key, bytes, TimeSpan.FromHours(24));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                // A missing photo must not discard valid votes or cause repeated 404s.
                cache.Set(key + "-missing", true, TimeSpan.FromHours(24));
                cache.Set("photo-backoff", true, TimeSpan.FromSeconds(options.FailureBackoffSeconds));
                logger.LogWarning(ex, "Foto indisponível para candidato {CandidateId}; mantendo resultados", candidate.Id);
                return;
            }
        }
    }
}
