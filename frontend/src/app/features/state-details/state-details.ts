import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DecimalPipe, DOCUMENT } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  combineLatest,
  map,
  distinctUntilChanged,
  catchError,
  EMPTY,
  exhaustMap,
  merge,
  switchMap,
  timer,
} from 'rxjs';
import { ElectionService } from '../../core/services/election-service';
import { electionPollingInterval, tabBecameVisible } from '../../core/services/election-polling';
import { candidateColors, StateElectionResult } from '../../core/models/election';
import { ElectionHeaderComponent } from '../../shared/election-header';
import { CandidateResultCardComponent } from '../../shared/candidate-result-card';

import {
  ElectionRoundSelectorComponent,
  selectedRound,
} from '../../shared/election-round-selector';

@Component({
  selector: 'app-state-details',
  imports: [
    ElectionRoundSelectorComponent,
    RouterLink,
    DecimalPipe,
    ElectionHeaderComponent,
    CandidateResultCardComponent,
  ],
  template: `
    <a class="back-link" routerLink="/dashboard" queryParamsHandling="preserve"
      >← Voltar ao mapa do Brasil</a
    >
    <app-election-round-selector [round]="round()" />
    @if (state(); as s) {
      <app-election-header
        [candidatesOnly]="!!s.candidatesOnly"
        [title]="s.name"
        [round]="s.round"
        [status]="s.status"
        [updatedAt]="s.updatedAt"
        [source]="s.source"
        [stale]="failed() || s.dataStatus === 'stale'"
      />
      <section class="state-summary">
        <span class="uf-tag">{{ s.uf }}</span>
        <h2>
          {{
            s.candidatesOnly
              ? 'Aguardando apuração do 2º turno'
              : (s.processedPercentage | number: '1.2-2' : 'pt-BR') + '% das seções totalizadas'
          }}
        </h2>
        <p>
          {{
            s.hasData
              ? 'Votos computados aos candidatos nesta UF.'
              : 'Ainda não há dados de votação disponíveis nesta UF.'
          }}
        </p>
      </section>
      <div class="candidate-grid">
        @for (candidate of rankedCandidates(); track candidate.id) {
          <app-candidate-result-card
            [candidate]="candidate"
            [color]="colors()[candidate.id]"
            [firstRoundSummary]="!!s.candidatesOnly"
          />
        }
      </div>
      <p class="notice">A navegação por municípios será disponibilizada em uma próxima etapa.</p>
      <div class="source-line">
        Fonte: Tribunal Superior Eleitoral
        @if (s.source === 'mock') {
          <strong>· Demonstração com dados fictícios</strong>
        }
      </div>
    } @else {
      <div class="empty-state" [attr.role]="failed() ? 'alert' : 'status'">
        <h1>{{ failed() ? 'Estado indisponível' : 'Carregando estado…' }}</h1>
        <p>
          {{
            failed()
              ? 'Verifique a UF ou aguarde uma nova consulta automática.'
              : 'Consultando dados da API.'
          }}
        </p>
      </div>
    }
  `,
})
export class StateDetailsComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(ElectionService);
  private readonly document = inject(DOCUMENT);
  readonly round = signal(2);
  readonly state = signal<StateElectionResult | null>(null);
  readonly failed = signal(false);
  readonly colors = computed(() => candidateColors(this.state()?.candidates ?? []));
  readonly rankedCandidates = computed(() =>
    [...(this.state()?.candidates ?? [])].sort(
      (a, b) =>
        (b.percentage ?? -1) - (a.percentage ?? -1) ||
        (b.votes ?? -1) - (a.votes ?? -1) ||
        a.name.localeCompare(b.name, 'pt-BR'),
    ),
  );
  constructor() {
    combineLatest([this.route.paramMap, this.route.queryParamMap])
      .pipe(
        map(([params, query]) => ({
          uf: params.get('uf') ?? '',
          round: selectedRound(query.get('round')),
        })),
        distinctUntilChanged((a, b) => a.uf === b.uf && a.round === b.round),
        switchMap(({ uf, round }) => {
          this.round.set(round);
          this.state.set(this.service.cachedState(uf, round));
          this.failed.set(false);
          return merge(
            timer(0, electionPollingInterval(round)),
            tabBecameVisible(this.document),
          ).pipe(
            exhaustMap(() =>
              this.service.getState(uf, round).pipe(
                catchError(() => {
                  this.failed.set(true);
                  return EMPTY;
                }),
              ),
            ),
          );
        }),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe((details) => {
        // Keep a real tally on screen if a warming server instance answers with the preview.
        if (details.result.candidatesOnly && this.state() && !this.state()!.candidatesOnly) {
          this.failed.set(true);
          return;
        }
        this.state.set(details.result);
        this.failed.set(false);
      });
  }
}
