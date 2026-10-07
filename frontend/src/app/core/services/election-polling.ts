// Poll only our cached API. Upstream collection is independently scheduled by the backend.
export function electionPollingInterval(round: number): number {
  return round === 2 ? 5_000 : 30_000;
}
