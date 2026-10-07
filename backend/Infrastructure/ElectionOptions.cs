namespace Election.Infrastructure;

public sealed class ElectionOptions
{
    public bool UseMockData { get; set; } = true;
    public bool FirstRoundUseMockData { get; set; } = false;
    public string FirstRoundElectionId { get; set; } = "6257";
    public int FirstRoundRefreshIntervalSeconds { get; set; } = 600;
    public int FirstRoundStaleAfterSeconds { get; set; } = 1800;
    public int RefreshIntervalSeconds { get; set; } = 10;
    public int StaleAfterSeconds { get; set; } = 30;
    public int FailureBackoffSeconds { get; set; } = 600;
    public int HttpTimeoutSeconds { get; set; } = 15;
    public int RetryCount { get; set; } = 2;
    public int RequestSpacingMilliseconds { get; set; } = 150;
    public string BaseUrl { get; set; } = "https://resultados.tse.jus.br";
    public string Environment { get; set; } = "oficial";
    public string Cycle { get; set; } = "ele2026";
    public string ElectionId { get; set; } = "6258";
    public int Round { get; set; } = 2;
    // Operational assumption: EA20 generation timestamps do not contain an offset.
    public int SourceUtcOffsetMinutes { get; set; } = -180;

    public ElectionOptions ForRound(int round)
    {
        if (round is not (1 or 2)) throw new ArgumentOutOfRangeException(nameof(round));
        var result = (ElectionOptions)MemberwiseClone();
        result.Round = round;
        if (round == 1)
        {
            result.ElectionId = FirstRoundElectionId;
            result.UseMockData = FirstRoundUseMockData;
            result.RefreshIntervalSeconds = FirstRoundRefreshIntervalSeconds;
            result.StaleAfterSeconds = FirstRoundStaleAfterSeconds;
        }
        return result;
    }
}
