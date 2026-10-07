using Election.Application;
using Election.Domain;
using Election.Infrastructure.Tse;
using Microsoft.Extensions.Options;

namespace Election.Infrastructure;

public sealed class ConfiguredElectionProvider(MockElectionProvider mock, TseElectionProvider tse,
    IOptions<ElectionOptions> options) : IElectionProvider
{
    public Task<ElectionSnapshot> FetchAsync(CancellationToken cancellationToken, int? round = null)
    {
        var selected = round ?? options.Value.Round;
        return options.Value.ForRound(selected).UseMockData
            ? mock.FetchAsync(cancellationToken, selected)
            : tse.FetchAsync(cancellationToken, selected);
    }
}
