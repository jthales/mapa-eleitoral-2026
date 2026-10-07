using Election.Domain;

namespace Election.Application;

public sealed record CandidateResultDto(string Id, string Number, string Name, string Party,
    long? Votes, decimal? Percentage, string? OfficialStatus, string? PhotoUrl = null);
public sealed record BrazilElectionResultDto(string? ElectionId, int Round, string Status,
    DateTimeOffset UpdatedAt, DateTimeOffset FetchedAt, decimal? ProcessedPercentage,
    long? TotalSections, long? ProcessedSections, IReadOnlyList<CandidateResultDto> Candidates,
    string Source, string DataStatus, bool HasData, bool VotesReleased, bool CandidatesOnly = false);
public sealed record StateElectionResultDto(string Uf, string Name, string? ElectionId, int Round,
    string Status, DateTimeOffset UpdatedAt, decimal? ProcessedPercentage,
    IReadOnlyList<CandidateResultDto> Candidates, string? LeadingCandidateId,
    decimal? MarginPercentagePoints, string Source, string DataStatus, bool HasData, bool VotesReleased, bool CandidatesOnly = false);
public sealed record StateElectionDetailsDto(StateElectionResultDto Result, bool MunicipalitiesAvailable = false);

public interface ITseElectionService
{
    Task<BrazilElectionResultDto> GetBrazilResultAsync(CancellationToken cancellationToken = default, int round = 2);
    Task<IReadOnlyCollection<StateElectionResultDto>> GetStatesAsync(CancellationToken cancellationToken = default, int round = 2);
    Task<StateElectionDetailsDto?> GetStateAsync(string uf, CancellationToken cancellationToken = default, int round = 2);
}

public sealed record CachedElection(ElectionSnapshot Snapshot, bool IsStale);
public interface IElectionCache
{
    CachedElection? Read(int round = 2);
    void Store(ElectionSnapshot snapshot);
    void MarkStale(int round = 2);
}

public interface IElectionProvider
{
    Task<ElectionSnapshot> FetchAsync(CancellationToken cancellationToken, int? round = null);
}

public sealed class ElectionUnavailableException() : Exception("Nenhum resultado válido está disponível no cache.");
