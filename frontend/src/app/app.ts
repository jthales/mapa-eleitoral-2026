import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  template: ` <a class="skip-link" href="#main">Pular para o conteúdo</a>
    <header class="site-header">
      <div class="shell header-inner">
        <a
          routerLink="/dashboard"
          queryParamsHandling="preserve"
          class="brand"
          aria-label="Mapa Eleitoral 2026 — início"
        >
          <span class="brand-mark" aria-hidden="true">m<span>e</span></span>
          <span>mapa<span class="brand-light">eleitoral</span><small>2026</small></span>
        </a>
        <span class="header-note">DADOS ABERTOS · INFORMAÇÃO NEUTRA</span>
        <a class="nav-link" routerLink="/dashboard" queryParamsHandling="preserve"
          >Visão nacional <span aria-hidden="true">↗</span></a
        >
      </div>
    </header>
    <main id="main" class="shell"><router-outlet /></main>
    <footer class="shell site-footer">
      <span>Mapa Eleitoral 2026 <span class="muted">/ Informação para acompanhar.</span></span>
      <span>Sem previsões. Sem recomendações políticas.</span>
    </footer>`,
})
export class App {}
