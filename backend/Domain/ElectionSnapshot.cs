namespace Election.Domain;

public sealed record CandidateResult(string Id, string Number, string Name, string Party,
    long Votes, decimal Percentage, string? OfficialStatus = null, string? PhotoUrl = null);

public sealed record ElectionTally(string ElectionId, int Round, string Scope, string Status,
    DateTimeOffset UpdatedAt, long TotalSections, long ProcessedSections,
    IReadOnlyList<CandidateResult> Candidates, bool VotesReleased = true)
{
    public decimal ProcessedPercentage => ElectionMath.Percentage(ProcessedSections, TotalSections);
    public bool HasData => VotesReleased && ProcessedSections > 0;
}

public sealed record ElectionSnapshot(ElectionTally Brazil, IReadOnlyList<ElectionTally> States,
    string Source, DateTimeOffset FetchedAt);

public static class ElectionMath
{
    public static decimal Percentage(long value, long total)
    {
        if (value < 0 || total < 0 || value > total) throw new ArgumentOutOfRangeException(nameof(value));
        return total == 0 ? 0 : Math.Round(100m * value / total, 2, MidpointRounding.AwayFromZero);
    }

    public static (string? Leader, decimal Margin) Leadership(IReadOnlyList<CandidateResult> candidates)
    {
        var ordered = candidates.OrderByDescending(c => c.Votes).ToArray();
        if (ordered.Length < 2 || ordered[0].Votes == 0 || ordered[0].Votes == ordered[1].Votes)
            return (null, 0);
        return (ordered[0].Id, Math.Round(Math.Abs(ordered[0].Percentage - ordered[1].Percentage), 2));
    }
}
