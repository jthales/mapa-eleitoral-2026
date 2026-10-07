import { Component, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';

@Component({
  selector: 'app-election-progress',
  imports: [DecimalPipe],
  template: ` <section class="progress-panel" aria-label="Andamento da totalização">
    <div>
      <p class="eyebrow">ANDAMENTO DA APURAÇÃO</p>
      <p class="progress-summary">
        <strong>{{ percentage() | number: '1.2-2' : 'pt-BR' }}<span>%</span></strong
        ><span>das seções totalizadas</span>
      </p>
    </div>
    <div class="progress-right">
      <div
        class="track"
        role="progressbar"
        aria-label="Seções totalizadas"
        aria-valuemin="0"
        aria-valuemax="100"
        [attr.aria-valuenow]="percentage()"
      >
        <div [style.width.%]="percentage()"></div>
      </div>
      <p>
        @if (total() > 0) {
          {{ processed() | number: '1.0-0' : 'pt-BR' }} de
          {{ total() | number: '1.0-0' : 'pt-BR' }} seções
        } @else {
          Aguardando dados das seções
        }
      </p>
    </div>
  </section>`,
})
export class ElectionProgressComponent {
  percentage = input(0);
  processed = input(0);
  total = input(0);
}
