import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { retry, tap, throwError, timer } from 'rxjs';
import { electionPollingInterval } from './election-polling';
import { ElectionResult, StateElectionDetails, StateElectionResult } from '../models/election';

@Injectable({ providedIn: 'root' })
export class ElectionService {
  private readonly http = inject(HttpClient);
  private readonly stateCache = new Map<
    string,
    { result: StateElectionResult; receivedAt: number }
  >();

  cachedState(uf: string, round: number): StateElectionResult | null {
    const entry = this.stateCache.get(`${round}:${uf.toUpperCase()}`);
    if (!entry) return null;
    return {
      ...entry.result,
      dataStatus:
        Date.now() - entry.receivedAt >= electionPollingInterval(round)
          ? 'stale'
          : entry.result.dataStatus,
    };
  }

  private rememberState(result: StateElectionResult) {
    const key = `${result.round}:${result.uf.toUpperCase()}`;
    // A warming server instance may answer with the preview; never let it replace a real tally.
    const previous = this.stateCache.get(key)?.result;
    if (result.candidatesOnly && previous && !previous.candidatesOnly) return;
    this.stateCache.set(key, {
      result,
      receivedAt: Date.now(),
    });
  }
  getElection(round?: number) {
    return this.http
      .get<ElectionResult>('/api/election', { params: round ? { round } : {} })
      .pipe(retryTransientFailure());
  }
  getStates(round?: number) {
    return this.http
      .get<StateElectionResult[]>('/api/election/states', {
        params: round ? { round } : {},
      })
      .pipe(
        retryTransientFailure(),
        tap((states) => states.forEach((state) => this.rememberState(state))),
      );
  }
  getState(uf: string, round?: number) {
    return this.http
      .get<StateElectionDetails>(`/api/election/states/${encodeURIComponent(uf)}`, {
        params: round ? { round } : {},
      })
      .pipe(
        retryTransientFailure(),
        tap((details) => {
          if (details.result) this.rememberState(details.result);
        }),
      );
  }
}

// Bounded retries keep cold-start failures in the loading state. Never retry invalid queries.
function retryTransientFailure<T>() {
  return retry<T>({
    count: 2,
    delay: (error) =>
      [0, 502, 503, 504].includes(error.status) ? timer(3000) : throwError(() => error),
  });
}
