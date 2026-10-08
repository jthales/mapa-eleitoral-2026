import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  map,
  distinctUntilChanged,
  switchMap,
  catchError,
  EMPTY,
  exhaustMap,
  forkJoin,
  Subject,
  merge,
  timer,
} from 'rxjs';
import { ElectionService } from '../../core/services/election-service';
import { electionPollingInterval } from '../../core/services/election-polling';
import { candidateColors, ElectionResult, StateElectionResult } from '../../core/models/election';
import { ElectionHeaderComponent } from '../../shared/election-header';
import { ElectionProgressComponent } from '../../shared/election-progress';
import { CandidateResultCardComponent } from '../../shared/candidate-result-card';
import { BrazilMapComponent } from '../map/brazil-map';

import {
  ElectionRoundSelectorComponent,
  selectedRound,
} from '../../shared/election-round-selector';

@Component({
  selector: 'app-election-dashboard',
  imports: [
    ElectionRoundSelectorComponent,
    DecimalPipe,
    RouterLink,
    ElectionHeaderComponent,
    ElectionProgressComponent,
    CandidateResultCardComponent,
    BrazilMapComponent,
  ],
  templateUrl: './election-dashboard.html',
})
export class ElectionDashboardComponent {
  private readonly service = inject(ElectionService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refresh = new Subject<void>();
  readonly round = signal(2);
  readonly pollingSeconds = computed(() => electionPollingInterval(this.round()) / 1000);
  readonly election = signal<ElectionResult | null>(null);
  readonly states = signal<StateElectionResult[]>([]);
  readonly failed = signal(false);
  readonly loading = signal(true);
  readonly query = signal('');
  readonly showAllCandidates = signal(false);
  readonly rankedCandidates = computed(() =>
    [...(this.election()?.candidates ?? [])].sort(
      (a, b) => (b.votes ?? 0) - (a.votes ?? 0) || a.name.localeCompare(b.name, 'pt-BR'),
    ),
  );
  readonly visibleCandidates = computed(() =>
    this.showAllCandidates() ? this.rankedCandidates() : this.rankedCandidates().slice(0, 4),
  );
  readonly colors = computed(() => candidateColors(this.election()?.candidates ?? []));
  readonly visibleStates = computed(() =>
    this.states().filter((s) =>
      `${s.name} ${s.uf}`
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .toLowerCase()
        .includes(
          this.query()
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .toLowerCase(),
        ),
    ),
  );
  readonly stale = computed(
    () =>
      this.failed() ||
      this.election()?.dataStatus === 'stale' ||
      this.states().some((s) => s.dataStatus === 'stale'),
  );
  constructor() {
    inject(ActivatedRoute)
      .queryParamMap.pipe(
        map((params) => selectedRound(params.get('round'))),
        distinctUntilChanged(),
        switchMap((round) => {
          this.round.set(round);
          this.showAllCandidates.set(false);
          this.election.set(null);
          this.states.set([]);
          this.failed.set(false);
          this.loading.set(true);
          return merge(timer(0, electionPollingInterval(round)), this.refresh).pipe(
            exhaustMap(() =>
              forkJoin({
                election: this.service.getElection(round),
                states: this.service.getStates(round),
              }).pipe(
                catchError(() => {
                  this.failed.set(true);
                  this.loading.set(false);
                  return EMPTY;
                }),
              ),
            ),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(({ election, states }) => {
        this.election.set(election);
        this.states.set(states);
        this.failed.set(false);
        this.loading.set(false);
      });
  }
  retry() {
    this.loading.set(true);
    this.refresh.next();
  }
  leader(state: StateElectionResult) {
    return state.candidates.find((c) => c.id === state.leadingCandidateId)?.name ?? 'Sem liderança';
  }
  leadingCandidate(state: StateElectionResult) {
    return state.candidates.find((candidate) => candidate.id === state.leadingCandidateId);
  }
}
