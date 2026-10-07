import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
@Component({
  selector: 'app-election-round-selector',
  imports: [RouterLink],
  template: `<nav class="round-selector" aria-label="Turno da eleição">
    @for (value of [1, 2]; track value) {
      <a
        [routerLink]="[]"
        [queryParams]="{ round: value }"
        queryParamsHandling="merge"
        [attr.aria-current]="round() === value ? 'page' : null"
        >{{ value }}º turno</a
      >
    }
  </nav>`,
  styles: [
    `
      .round-selector {
        display: flex;
        gap: 8px;
        margin: 20px 0;
      }
      a {
        padding: 10px 20px;
        border: 1px solid #aab8b7;
        border-radius: 24px;
        color: #244946;
        text-decoration: none;
        font-weight: 600;
      }
      a[aria-current] {
        color: white;
        background: #244946;
        border-color: #244946;
      }
      a:focus-visible {
        outline: 3px solid #776ac3;
        outline-offset: 3px;
      }
    `,
  ],
})
export class ElectionRoundSelectorComponent {
  readonly round = input(2);
}
export function selectedRound(value: string | null): number {
  return value === '1' ? 1 : 2;
}
