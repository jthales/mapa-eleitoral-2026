import { filter, fromEvent } from 'rxjs';

// Poll only our cached API. Upstream collection is independently scheduled by the backend.
export function electionPollingInterval(round: number): number {
  return round === 2 ? 5_000 : 30_000;
}

// Background tabs throttle or suspend timers; fetch right away when the user returns.
export function tabBecameVisible(document: Document) {
  return fromEvent(document, 'visibilitychange').pipe(
    filter(() => document.visibilityState === 'visible'),
  );
}
