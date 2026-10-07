# Roadmap

Última revisão: 2026-10-07. Foco do MVP: Presidente, resultado, apuração e mapa por UF. Sem pesquisas, propostas e notícias.

## Phase 0 - Continuidade

- [x] Inspecionar pasta e criar documentação portátil.
- [x] Registrar stack, decisões, convenções e regras de neutralidade.
- [x] Atualizar documentação para refletir o MVP implementado.
- [ ] Recuperar RTK.md, se disponível.
- [x] Inicializar main e criar origem GitHub privada jthales/mapa-eleitoral-2026.

## Phase 1 - Base

- [x] Angular 22.2.1 / CLI 22.2.2, TypeScript 6.0.3 e Node compatível.
- [x] API .NET 10 com Api/Application/Domain/Infrastructure.
- [x] Comandos reais de instalação, execução, builds e testes.
- [x] ITseElectionService, IElectionProvider, DTOs e validação de UF.
- [x] Endpoints nacionais, UFs e detalhes de UF.
- [x] Angular conectado à API por HttpClient e proxy.
- [x] MockElectionProvider configurável, dois candidatos fictícios, 27 UFs, empate e UFs sem apuração.
- [x] Dashboard responsivo, cabeçalho, progresso, cards, fonte e identificação de mock.
- [x] Mapa SVG local derivado de GeoJSON IBGE, painel de hover/foco e navegação.
- [x] Estados loading/loaded/error/no-data e cores neutras para ausência/empate.
- [x] Busca por UF/nome e tela inicial de detalhes.
- [x] Resumo nacional ordenado por votos, quatro candidatos expansíveis e badge de classificação oficial para segundo turno.
- [x] Painel lateral com candidatos ordenados por votos, sem legenda duplicada, e resultado nacional ao sair do estado.
- [x] Aplicar paleta de cores por partido fornecida pelo usuário, compartilhada por dashboard/mapa/detalhes.
- [x] IElectionCache/IMemoryCache, worker configurável e último snapshot preservado.
- [x] Builds, 15 testes xUnit, 12 testes Vitest e smoke visual desktop/celular.
- [ ] Confirmar licença específica de redistribuição da geometria e registrar evidência.

## Phase 2 - TSE

- [x] Ler layouts EA11/EA20, documentar URLs, campos e códigos.
- [x] Documentar finalidade/campos relevantes de EA12 e EA14.
- [ ] Ler integralmente EA15 quando o download estiver acessível; consulta desta sessão foi bloqueada.
- [x] Validar EA11 e EA20 com fixtures oficiais rastreáveis.
- [x] Implementar parsing e provedor real, IHttpClientFactory, timeout/retry/cancelamento/logging.
- [x] Verificar uma coleta real: Brasil e 27 UFs do primeiro turno federal.
- [x] Identificar código 6258 via EA11 para segundo turno.
- [ ] Validar dados reais de segundo turno quando o arquivo estiver disponível (404 na consulta).
- [ ] Confirmar fuso de geração dos arquivos.
- [ ] Testar cenários oficiais adicionais de início de totalização, retotalização e divulgação restrita.
- [ ] Avaliar EA14/EA15 para reduzir consultas e atualização parcial por UF.

- [x] Consulta dos dois turnos com seletor e caches isolados, com modo real e espera pelos resultados do segundo turno.
- [x] Fotos oficiais de candidatos via backend, cache e alternativa visual; 12 retratos verificados.

- [x] Mostrar classificados oficiais no segundo turno sem copiar votos; priorizar snapshot real quando disponível.

## Phase 3 - Municípios

- [ ] Validar correspondência TSE/IBGE e geometria municipal.
- [ ] Implementar mapa estadual e drill-down.
- [ ] Exibir resultados municipais e estados sem dados.

## Phase 4 - Election Day

- [x] Polling da API própria: 5s segundo turno e 30s primeiro. Worker de segundo turno com alvo de 10s entre inícios, sem sobreposição.
- [ ] Medir carga, latência e orçamento de requisições em condições operacionais.
- [ ] Decidir se SSE/WebSocket traz benefício suficiente para substituir polling.
- [ ] Avaliar Redis/coordenação de réplicas conforme necessidade.
- [ ] Instrumentar métricas, alertas e testes de carga/falha prolongada.
- [x] Publicar Angular/API na Vercel Hobby com HTTPS e GitHub conectado.
- [ ] Completar observabilidade e validar carga/cold start; coordenar worker/cache entre réplicas.

## Phase 5 - Pós eleição

- [ ] Comparar primeiro/segundo turnos com dados compatíveis.
- [x] Mostrar margem observada por UF, sem previsão.
- [x] Diferenciar rótulo de liderança durante apuração e mais votado após totalização.
- [ ] Histórico e análises geográficas.

Ao concluir tarefa relevante, atualizar este arquivo e current-status.md; novas decisões em decisions.md.

- [x] Reduzir indisponibilidade no cold start: esperar brevemente pelo worker, priorizar turno 1 e repetir falhas transitórias no cliente (2026-10-07).

- [x] Adicionar crédito de Thales Silva e links pessoais GitHub/LinkedIn ao rodapé global (2026-10-07).
