import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ElectionResult, StateElectionDetails, StateElectionResult } from '../models/election';

@Injectable({ providedIn: 'root' })
export class ElectionService {
  private readonly http = inject(HttpClient);
  getElection(round?: number) {
    return this.http.get<ElectionResult>('/api/election', { params: round ? { round } : {} });
  }
  getStates(round?: number) {
    return this.http.get<StateElectionResult[]>('/api/election/states', {
      params: round ? { round } : {},
    });
  }
  getState(uf: string, round?: number) {
    return this.http.get<StateElectionDetails>(`/api/election/states/${encodeURIComponent(uf)}`, {
      params: round ? { round } : {},
    });
  }
}
