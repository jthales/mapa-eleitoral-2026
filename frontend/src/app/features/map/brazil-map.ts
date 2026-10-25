import { Component, computed, inject, input, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ElectionResult, StateElectionResult } from '../../core/models/election';

interface StateShape {
  uf: string;
  name: string;
  path: string;
  x: number;
  y: number;
}
@Component({
  selector: 'app-brazil-map',
  host: { '(document:click)': 'tapOutside($event)' },
  imports: [DecimalPipe, RouterLink],
  templateUrl: './brazil-map.html',
  styleUrl: './brazil-map.scss',
})
export class BrazilMapComponent {
  national = input<ElectionResult | null>(null);
  states = input<StateElectionResult[]>([]);
  colors = input<Record<string, string>>({});
  status = input<'loading' | 'loaded' | 'error' | 'no-data'>('loading');
  readonly shapes = signal<StateShape[]>([]);
  readonly mapError = signal(false);
  readonly selected = signal<string | null>(null);
  // Touch screens: a tap only selects the state inline instead of opening its page.
  readonly touch = typeof matchMedia === 'function' && matchMedia('(hover: none)').matches;
  readonly hovered = computed(() => this.states().find((s) => s.uf === this.selected()));
  readonly panelResult = computed(() => this.hovered() ?? this.national());
  readonly rankedCandidates = computed(() =>
    [...(this.panelResult()?.candidates ?? [])].sort(
      (a, b) => (b.votes ?? 0) - (a.votes ?? 0) || a.name.localeCompare(b.name, 'pt-BR'),
    ),
  );
  readonly margin = computed(() => {
    const state = this.hovered();
    if (state) return state.marginPercentagePoints;
    const [first, second] = this.rankedCandidates();
    if (!first || !second || first.votes === second.votes) return 0;
    return Math.round(((first.percentage ?? 0) - (second.percentage ?? 0)) * 100) / 100;
  });
  hover(uf: string | null) {
    if (!this.touch) this.selected.set(uf);
  }
  tap(uf: string) {
    if (this.touch) this.selected.set(uf);
  }
  // Tapping anywhere but a state goes back to the national result.
  tapOutside(event: Event) {
    if (this.touch && !(event.target as Element | null)?.closest?.('svg a')) {
      this.selected.set(null);
    }
  }
  clearFocus(event: FocusEvent) {
    if (!(event.currentTarget as HTMLElement).contains(event.relatedTarget as Node | null)) {
      this.selected.set(null);
    }
  }
  readonly byUf = computed(() => new Map(this.states().map((s) => [s.uf, s])));
  constructor() {
    // Static, locally bundled geometry; no external requests from the frontend.
    inject(HttpClient)
      .get<StateShape[]>('/maps/brazil-states.json')
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (shapes) => this.shapes.set(shapes),
        error: () => this.mapError.set(true),
      });
  }
  fill(uf: string) {
    const state = this.byUf().get(uf);
    return state?.hasData && state.leadingCandidateId
      ? (this.colors()[state.leadingCandidateId] ?? '#dce2e0')
      : '#dce2e0';
  }
}
