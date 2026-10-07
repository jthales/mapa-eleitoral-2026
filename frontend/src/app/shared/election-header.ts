import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DataSource, ElectionStatus, statusLabel } from '../core/models/election';

@Component({
  selector: 'app-election-header',
  imports: [DatePipe],
  template: `
    <div class="page-heading">
      <div>
        <p class="eyebrow">ELEIÇÕES GERAIS / 2026</p>
        <h1>{{ title() }}</h1>
        <p class="subtitle">Presidente <span class="separator">/</span> {{ round() }}º turno</p>
      </div>
      <div class="update-meta">
        <span class="status-pill"
          ><span class="status-dot"></span
          >{{ candidatesOnly() ? 'Aguardando apuração do 2º turno' : label(status()) }}</span
        >
        <p>
          {{ candidatesOnly() ? 'Classificação no 1º turno:' : 'Última atualização:' }}
          <strong>{{
            updatedAt()
              ? (updatedAt()
                | date: (candidatesOnly() ? 'dd/MM/yyyy HH:mm:ss' : 'HH:mm:ss') : '-0300')
              : '—'
          }}</strong>
          <small>(Brasília)</small>
        </p>
      </div>
    </div>
    @if (candidatesOnly()) {
      <div class="notice" role="status">
        Candidatos classificados segundo o resultado oficial do 1º turno. Ainda não há resultados do
        2º turno disponíveis nesta aplicação.
      </div>
    }
    @if (source() === 'mock') {
      <div class="notice mock-notice" role="status">
        <strong>AMBIENTE DE DEMONSTRAÇÃO</strong
        ><span>Candidatos e votos fictícios. Estes não são resultados do TSE.</span>
      </div>
    }
    @if (stale()) {
      <div class="notice warning" role="alert">
        <strong>Dados temporariamente desatualizados.</strong
        ><span
          >Última atualização válida:
          {{ updatedAt() | date: 'HH:mm:ss' : '-0300' }} (Brasília).</span
        >
      </div>
    }
  `,
})
export class ElectionHeaderComponent {
  candidatesOnly = input(false);
  title = input('O Brasil, estado por estado.');
  round = input(2);
  status = input<ElectionStatus>('not_started');
  updatedAt = input<string | null>(null);
  source = input<DataSource | null>(null);
  stale = input(false);
  label = statusLabel;
}
