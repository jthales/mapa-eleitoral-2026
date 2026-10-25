import { Component, computed, effect, input, signal, untracked } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { CandidateResult } from '../core/models/election';

@Component({
  selector: 'app-candidate-result-card',
  imports: [DecimalPipe],
  template: ` <article
    class="candidate-card"
    [class.candidate-muted]="notElected()"
    [style.--candidate-color]="color()"
  >
    <div class="candidate-top">
      <span class="candidate-avatar" aria-hidden="true">
        @if (candidate().photoUrl && failedPhoto() !== candidate().photoUrl) {
          <img
            [src]="candidate().photoUrl"
            alt=""
            width="64"
            height="64"
            loading="lazy"
            (error)="failedPhoto.set(candidate().photoUrl ?? null)"
          />
        } @else {
          {{ candidate().name.slice(0, 1) }}
        }
      </span>
      <div>
        <h3>{{ candidate().name }}</h3>
        <p>
          {{ candidate().party }} <span>· {{ candidate().number }}</span>
        </p>
      </div>
      <span class="color-dot" aria-hidden="true"></span>
    </div>
    @if (candidate().votes !== null && candidate().percentage !== null) {
      <div class="candidate-result">
        <div class="candidate-share">
          <strong>{{ candidate().percentage | number: '1.2-2' : 'pt-BR' }}<span>%</span></strong>
          @if (trend(); as delta) {
            <span
              class="candidate-trend"
              [class.trend-up]="delta > 0"
              [class.trend-down]="delta < 0"
              [attr.aria-label]="
                (delta > 0 ? 'Subiu ' : 'Caiu ') +
                ((delta < 0 ? -delta : delta) | number: '1.2-2' : 'pt-BR') +
                ' pontos percentuais desde a última atualização'
              "
              title="Variação desde a última atualização"
              ><span aria-hidden="true">{{ delta > 0 ? '▲' : '▼' }}</span>
              {{ (delta < 0 ? -delta : delta) | number: '1.2-2' : 'pt-BR' }} p.p.</span
            >
          }
        </div>
        <span>{{ candidate().votes | number: '1.0-0' : 'pt-BR' }} votos</span>
      </div>
      <div class="candidate-track" aria-hidden="true">
        <span [style.width.%]="candidate().percentage"></span>
      </div>
    } @else {
      <p class="notice">Aguardando apuração do 2º turno</p>
    }
    @if (advanced()) {
      <p class="runoff-badge">Classificado para o 2º turno</p>
    } @else if (candidate().officialStatus) {
      <p class="official-status">Situação oficial: {{ candidate().officialStatus }}</p>
    }
  </article>`,
})
export class CandidateResultCardComponent {
  candidate = input.required<CandidateResult>();
  firstRoundSummary = input(false);
  readonly advanced = computed(
    () =>
      this.firstRoundSummary() &&
      this.candidate().officialStatus?.trim().toLocaleLowerCase('pt-BR') === '2º turno',
  );
  readonly notElected = computed(
    () =>
      this.firstRoundSummary() &&
      this.candidate().officialStatus?.trim().toLocaleLowerCase('pt-BR') === 'não eleito',
  );
  readonly failedPhoto = signal<string | null>(null);
  color = input('#167f78');
  /** Identifies the result context (e.g. scope and round); a new key restarts the trend. */
  trendKey = input('');
  /** Change in p.p. from the last different percentage seen; kept until the next change. */
  readonly trend = signal<number | null>(null);
  private trendContext: { key: string; id: string; percentage: number | null } | null = null;

  constructor() {
    effect(() => {
      const key = this.trendKey();
      const { id, percentage } = this.candidate();
      untracked(() => this.updateTrend(key, id, percentage));
    });
  }

  private updateTrend(key: string, id: string, percentage: number | null) {
    const rounded = percentage === null ? null : Math.round(percentage * 100) / 100;
    const last = this.trendContext;
    if (!last || last.key !== key || last.id !== id) {
      this.trendContext = { key, id, percentage: rounded };
      this.trend.set(null);
      return;
    }
    if (rounded === null || rounded === last.percentage) return;
    if (last.percentage !== null) {
      this.trend.set(Math.round((rounded - last.percentage) * 100) / 100);
    }
    last.percentage = rounded;
  }
}
