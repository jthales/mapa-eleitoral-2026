import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { retry, throwError, timer } from 'rxjs';
import { ElectionResult, StateElectionDetails, StateElectionResult } from '../models/election';

@Injectable({ providedIn: 'root' })
export class ElectionService {
  private readonly http = inject(HttpClient);
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
      .pipe(retryTransientFailure());
  }
  getState(uf: string, round?: number) {
    return this.http
      .get<StateElectionDetails>(`/api/election/states/${encodeURIComponent(uf)}`, {
        params: round ? { round } : {},
      })
      .pipe(retryTransientFailure());
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
