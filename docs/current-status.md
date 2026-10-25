# Current Status

## Last updated
2026-10-25

## Working
- MVP executável: Angular 22.2.1 + CLI/build 22.2.2, TypeScript 6.0.3, .NET 10 (SDK verificado 10.0.401).
- Dashboard, cards, progresso, mapa SVG do IBGE, busca por estado e rota /states/:uf.
- Resultado nacional ordenado por votos, quatro cards iniciais e Mostrar mais/menos; expansão reiniciada ao trocar turno. Badge de classificação somente com officialStatus=2º turno; visual discreto somente com Não eleito no resumo do primeiro turno.
- Rótulo Mais votado na UF quando a totalização nacional está concluída; Liderança momentânea durante a apuração, sem inferir eleição.
- Painel lateral do mapa lista candidatos por votos decrescentes uma única vez; legenda duplicada removida, mantendo indicação neutra e aviso sobre liderança; mostra Brasil por padrão e ao sair da UF. Hover/foco mostra UF; saída de foco do componente retorna Brasil. Dados nacionais vêm da API BR, não da soma das UFs.
- Cores por partido conforme paleta fornecida pelo usuário em frontend/src/app/core/models/party-colors.ts; aplicadas a cards, mapa, tabela e legenda. UNIÃO/MISSÃO e PCdoB normalizados; partidos não listados e fictícios usam cinza.
- Segundo turno real habilitado; enquanto EA20 retorna 404, API apresenta apenas os dois classificados oficiais do primeiro turno (candidatesOnly), com fotos e métricas null. UI mostra espera e mapa neutro, inclusive nas UFs. Resultado real tem prioridade automática.
- API nacional, lista de estados e detalhes; validação de UF, 400/503 e health.
- Seletor 1º/2º turno na URL, mapa e detalhes; troca cancela requisições antigas.
- Worker com agenda e caches separados: segundo turno 10s/stale 30s; primeiro 600s/stale 1800s. Último resultado preservado em falhas.
- Fotos oficiais via TseCandidatePhotos e endpoint local; 12 JPEGs do primeiro turno verificados. Cache 24h, inicial como fallback; mock sem fotos reais.
- Adaptador EA11/EA20 com normalização, timeout/retry/cancelamento e validação de eleição/turno/fase/abrangência.
- Fonte real verificada em consulta única para eleição 6257, turno 1: Brasil e 27 UFs, sem candidatos hardcodados.
- Documentação atualizada e screenshots em docs/images/dashboard.png e docs/images/first-round.png.
- Arquivos principais: backend/Api/Program.cs, Application/Contracts.cs, Infrastructure/Tse/, Infrastructure/ElectionUpdateWorker.cs, frontend/src/app/features/ e scripts/generate-map.mjs.

- 2026-10-25 (noite do 2º turno): instância fria coletava o 1º turno antes do 2º e liberava requisições assim que o 1º chegava, servindo a prévia candidatesOnly ("Candidatos ao 2º turno", sem votos). Agora o worker coleta o 2º turno primeiro, o middleware espera o snapshot do turno pedido (até 25s) e, sem snapshot, falha usa backoff de 30s em vez de 600s. Frontend não substitui apuração real por prévia vinda de outra réplica (marca stale) e atualiza ao voltar para a aba, além do polling de 5s.

## In progress
- Hospedagem Vercel publicada em https://mapa-eleitoral-2026-omega.vercel.app; configuração e limitações registradas em docs/deployment.md.
- Ambiente local de preview iniciado durante a sessão: API 5080 e frontend 4200; processos não são serviço persistente.

## Pending
- Validar resultados efetivos do segundo turno e revalidar layouts/configuração quando publicados.
- Confirmar documentalmente o fuso dos horários de geração do TSE.
- Registrar licença específica de redistribuição da malha IBGE; fonte/derivação estão registradas, sem atribuir licença não verificada.
- Municípios, Redis, SSE/WebSocket, histórico e carga/observabilidade operacional. Na Vercel, coordenar cache/coleta entre réplicas antes de uso com alto tráfego.
- Recuperar RTK.md, se disponível.

## Known issues
- Segundo turno: EA20 nacional 6258 retornou 404 na consulta desta sessão; não confundir isso com prova de cronograma de publicação.
- Cache em memória é por processo e desaparece ao reiniciar. Snapshot completo é publicado atomicamente; falha em qualquer UF mantém o anterior inteiro, ou 503 se não houver anterior.
- Pode haver diferença entre a geração do arquivo BR e das UFs, mesmo em uma coleta bem-sucedida; cada DTO preserva sua data.
- SourceUtcOffsetMinutes=-180 é hipótese operacional configurável, não confirmação documental do fuso TSE.
- A política fresh/stale mede sucesso/recência da consulta; dados oficialmente finalizados podem ter geração antiga e continuar fresh.
- Mapa simplificado, sem municípios e sem tratamento cartográfico especializado de pequenas UFs; tabela é alternativa acessível.
- RTK.md não encontrado. Git em main; origin privado: https://github.com/jthales/mapa-eleitoral-2026. Vercel Hobby conectado ao GitHub, Angular/API em um container; ver docs/deployment.md.
- Vercel escala/encerra instâncias: coleta permanente a cada 10s e consulta única global não são garantidas. Cache/worker permanecem por processo; inicialização pode responder 503 até aquecer.
- Falha anterior do sandbox não ocorreu nesta sessão. SDK local instalado em .tools/dotnet (ignorado); Node usa certificados do sistema quando necessário.

## TSE integration status
- Integração real implementada em backend/Infrastructure/Tse. EA11 resolve eleição/diretório; EA20 fornece Brasil e UFs.
- Fonte consultada e fixtures oficiais preservadas em backend/Tests/Fixtures com proveniência.
- EA12/EA14 documentados para evolução; EA15 identificado, download do layout bloqueado nesta sessão. Nenhum desses três é consumido no runtime.
- Segundo turno federal 6258 identificado via cdt2 do EA11; resultado nacional 404. Primeiro turno 6257 retornou 200 e a consulta completa real passou.
- Nenhum resultado eleitoral é fabricado em falhas. Modo real não troca automaticamente para mock.

## Mock status
- Segundo turno: Election:UseMockData=false. Primeiro turno real: FirstRoundUseMockData=false (6257). Modo offline completo exige os dois flags true. MockElectionProvider com Candidato A/B, todas as 27 UFs, percentuais variados, empate e UFs sem apuração.
- Totais nacionais do mock derivados das UFs; valores determinísticos e fictícios. Geração do snapshot usa horário da consulta; não simula avanço dos votos.
- Frontend identifica demonstração no topo e junto à fonte. Fixtures oficiais dos testes não são servidas pelo mock.

## Tests
- Deploy: 15 testes backend aprovados, publish .NET Release e build Angular aprovados, build Docker remoto Vercel concluído. Smoke local validou SPA/deep links, API, 400/404 e liveness. URL pública respondeu com primeiro turno oficial; Chromium validou classificados/fotos, 27 UFs neutras, detalhes, mobile e retorno ao primeiro turno, sem erros JavaScript. Sem teste de carga ou garantia de agendamento contínuo na plataforma.
- Backend: build sem avisos/erros; 15 testes xUnit aprovados.
- Angular: build de produção aprovado; 12 testes Vitest em 4 arquivos aprovados. Reexecutados após a paleta por partido; smoke Chromium confirmou cores nos cards e mapa. Nova alteração inclui projeção de classificados em Application/TseElectionService e métricas nullable nos DTOs.
- Cobertura comportamental: parsing real, valores inválidos, voto não divulgado, status oficial, round/configuração, percentuais/margem, mock, cache/stale, retry/404 e endpoints.
- Frontend: services, formatação de cards, progresso acessível, cores estáveis, mapa sem dados e erro de geometria. Após ajuste do painel: build e 10 testes aprovados; Chromium confirmou líder do Amapá primeiro, retorno ao Brasil e navegação por foco. Backend não alterado.
- Navegador Chromium: 27 paths/linhas, hover, clique em SP, busca sem acentos, viewport 390px sem overflow global e erro 503/retry; sem erros de JavaScript.
- Smoke real de uma coleta: Brasil e 27 UFs, 6257/turno 1; não usado na suíte offline.
- Validação adicional: isolamento de turno/cache/falha, query inválida, seleção e cancelamento de resposta atrasada, deep link/retorno, 12 fotos decodificadas no navegador e fallback HTTP 404, viewport 390px sem overflow.
- Resumo nacional: build e 11 testes aprovados; Chromium validou ordem dos 4/12 candidatos, badges oficiais, visual discreto, expandir/recolher, reset por turno e viewport 390px. Backend não alterado.
- Lista duplicada da legenda removida de brazil-map.html e cálculo não utilizado removido de brazil-map.ts. Build aprovado; mantidos Sem dados ou empate e aviso de que liderança não representa vitória.
- Espera de segundo turno: 15 testes backend/12 frontend aprovados e build Angular. Smoke Chromium validou dois classificados, fotos, ausência de métricas inventadas, mapa neutro, detalhes de SP, mobile e primeiro turno preservado. 404 nacional de segundo turno confirmado novamente.
- /health do segundo turno continua 503 enquanto não houver snapshot real desse turno; a lista de classificados não representa saúde da coleta de resultados.
- Cadência rápida: segundo turno alvo 10s entre inícios, sem sobrepor coletas nem recuperar ciclos perdidos; frontend consulta cache a cada 5s (primeiro mantém 30s). Primeiro turno upstream 600s; backoff 600s preservado. Build e 15 testes backend/12 frontend aprovados. Latência final depende de geração/CDN/coleta/polling; não há garantia de exibir em menos de 10s.
- Repositório GitHub privado criado a pedido do usuário. .gitignore exclui ferramentas, temporários, dependências, builds, arquivos .env e certificados; .gitattributes preserva fixtures oficiais. Preparação do commit inicial sem alterações funcionais ou reexecução de testes (27 testes aprovados na última mudança).
- Comandos reproduzíveis no README. Scripts temporários de verificação ficam em tmp, fora de versionamento.

## Next recommended task
- Adotar coordenação de coleta/cache compartilhado e medir cold start/carga na hospedagem. Revalidar EA20 do segundo turno, confirmar fuso e fechar licença específica da geometria.

## Correção de inicialização — 2026-10-07
- API aguarda até 25s pela primeira tentativa do worker quando não há snapshot, sem iniciar coleta por requisição. Cancelar um visitante não cancela a coleta ou a espera dos demais.
- Worker prioriza primeiro turno, que também fornece os classificados oficiais. Falha/backoff seguem preservados; espera não garante disponibilidade em falha prolongada do TSE.
- Frontend repete falhas transitórias 0/502/503/504 duas vezes com 3s de intervalo, mantendo carregamento; 400 não é repetido. Erro final continua explícito, sem fabricar dados.
- Validação: 18 testes backend, 14 frontend e build Angular aprovados. Integração cobre requisições concorrentes na API fria; testes cobrem cancelamento, limite de espera e recuperação no cliente.
- Cache compartilhado continua pendente; esta correção reduz falhas durante cold start, não fornece persistência entre réplicas.

## Crédito no rodapé — 2026-10-07
- Rodapé global inclui Desenvolvido por Thales Silva, GitHub jthales e LinkedIn thaleshenrique38, conforme solicitação. Links externos abrem nova aba com noopener/noreferrer.
- Arquivos: frontend/src/app/app.ts e frontend/src/styles.scss. Build Angular aprovado; Chromium verificou nomes/URLs e ausência de overflow em 1440px e 390px. Sem mudança no backend.

## Navegação para UF — 2026-10-07
- ElectionService guarda em memória do navegador os últimos resultados por UF/turno recebidos pela lista e pelo detalhe. StateDetails exibe esse resultado imediatamente e revalida pela API; falhas mantêm conteúdo com aviso. Cache anterior ao intervalo de polling é marcado stale ao reutilizar.
- Detalhe ordena candidatos por percentual decrescente, votos e nome como desempate, sem alterar o array da resposta.
- 15 testes frontend e build aprovados. Chromium verificou navegação para AM com requisição de detalhe bloqueada: renderização imediata, ordem correta e mobile sem overflow. Para o teste local, APIs foram interceptadas com respostas obtidas da aplicação publicada porque o proxy local retornou 502.
- Cache do navegador não persiste ao recarregar a página; acesso direto ainda depende da API/cache por instância. Redis/coordenação continuam pendentes.

## Votos absolutos e navegação — 2026-10-07
- Painel do mapa apresenta percentual e votos absolutos por candidato no hover/foco da UF e no resumo do Brasil. Valores null permanecem não divulgados, sem converter para zero.
- Tabela de UFs mostra votos/percentual do líder abaixo do nome. Cards nacionais e estaduais já mostravam ambos e foram preservados.
- Router usa withInMemoryScrolling com scrollPositionRestoration=top para abrir navegações no topo, inclusive no celular.
- Build Angular e 15 testes aprovados. Chromium com respostas controladas da API verificou valores exatos no painel, retorno ao Brasil, tabela, navegação móvel desde o fim da página com scrollY=0 e ausência de overflow.
