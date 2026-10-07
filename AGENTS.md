Before making changes, read this file and `docs/current-status.md`.

# Orientações para agentes

## Objetivo

**Mapa Eleitoral 2026**: aplicação politicamente neutra para acompanhar Presidente, apuração e mapa por UF, com consulta dos dois turnos de 2026. Pesquisas, propostas e notícias estão fora do MVP. A base executável existe; o estado datado e as limitações estão em [docs/current-status.md](docs/current-status.md).

## Stack e estrutura

- Angular 22, TypeScript, SCSS, standalone components, HttpClient, signals e lazy routes. Versões exatas resolvidas em frontend/package-lock.json.
- frontend/src/app/core: models e services; features: dashboard, map e state-details; shared: componentes reutilizáveis. Não há interceptor porque não foi necessário.
- ASP.NET Core Web API .NET 10/C#, com projetos backend/Api, Application, Domain, Infrastructure e Tests.
- Api configura HTTP/DI; Application contém contratos e leitura do cache; Domain contém modelos/matemática; Infrastructure implementa TSE, mock, cache e worker.
- IHttpClientFactory, BackgroundService e IMemoryCache atrás de IElectionCache. Sem banco ou biblioteca visual pesada.
- Geometria local em frontend/public/maps, gerada por scripts/generate-map.mjs.
- Documentação: architecture, current-status, roadmap, tse-integration, decisions, conventions e logs opcionais em docs/sessions.

## Execução, testes e comandos

Na raiz: `dotnet restore backend/Election.slnx`, `dotnet run --project backend/Api`, `dotnet build backend/Election.slnx`, `dotnet test backend/Election.slnx`.

Em frontend: `npm ci`, `npm start`, `npm run build`, `npm test`. API em localhost:5080; frontend em 127.0.0.1:4200 com proxy. Os testes usam xUnit e Vitest, respectivamente. Consultar [README](README.md) para requisitos, certificados Node e uso do SDK local .tools/dotnet quando não houver dotnet no PATH.

Formatação: `dotnet format backend/Election.slnx`; em frontend, `npx --no-install prettier --write "src/**/*.{ts,html,scss}"`. Regenerar mapa na raiz: `node scripts/generate-map.mjs`.

Não versionar .tools, tmp, node_modules, bin, obj, dist, .vercel, .env*, segredos ou logs. Git em main, origin privado https://github.com/jthales/mapa-eleitoral-2026. Vercel Hobby conectado ao GitHub; ver docs/deployment.md. Não alterar fixtures oficiais para fazer testes passarem.

## Arquitetura e integração

Frontend → API → cache. ElectionUpdateWorker usa IElectionProvider para obter e normalizar dados; requisições de usuários nunca consultam o TSE. TseElectionService implementa ITseElectionService e lê apenas o cache.

Integração em backend/Infrastructure/Tse: TseConfigurationParser (EA11), TseResultParser (EA20), TseHttpClient e TseElectionProvider. Fontes e significado dos campos em [docs/tse-integration.md](docs/tse-integration.md). Não confundir o read service com o provedor de download.

Segundo turno: UseMockData=false, intervalo alvo de 10s entre inícios após sucesso e backoff 600s em falha. Primeiro: FirstRoundUseMockData=false (TSE real), intervalo 600s. Caches e stale separados por turno; falhas preservam snapshot e marcam stale; sem snapshot de segundo turno, TseElectionService pode projetar candidatos oficiais do primeiro (candidatesOnly=true, métricas e electionId null); sem classificação oficial válida retorna 503. O modo real não faz fallback silencioso para mock. fresh significa coleta bem-sucedida recente, não necessariamente nova geração oficial.

## Regras importantes

1. Não inventar APIs ou campos do TSE; vincular afirmações às fontes oficiais e distinguir documentação de acesso testado.
2. Dados eleitorais reais devem vir de fontes oficiais.
3. O frontend nunca deve acessar diretamente o TSE.
4. O backend é responsável por normalização e cache.
5. Não hardcodar candidatos reais. Fixtures oficiais rastreáveis são insumos de teste, não dados de runtime.
6. Mocks devem usar Candidato A/Candidato B e ser identificados como fictícios.
7. Não recomendar candidatos ou atribuir notas.
8. Não realizar previsões eleitorais ou inferir tendências políticas como fato.
9. Não declarar vencedor antes do status oficial correspondente; liderança, 100% e e=s isolado não bastam.
10. Preservar compatibilidade com testes existentes; rodar os testes apropriados e registrar limitações.
11. Não apresentar planos ou testes não executados como funcionalidades verificadas.
12. Não mudar a arquitetura funcional apenas para adequá-la à documentação.

## Convenções

Seguir [docs/conventions.md](docs/conventions.md). Manter abreviações TSE isoladas no adaptador, CancellationToken propagado, logging estruturado e models frontend coerentes com DTOs. Cores seguem PARTY_MAP_COLORS em core/models/party-colors.ts, conforme paleta fornecida pelo usuário; siglas normalizadas sem acentos e fallback cinza para partidos não listados.

## Implementado e limitações

Dashboard, seletor de turno via ?round=1|2, mapa de UFs, detalhes, fotos oficiais, API, mock, cache/worker, adaptador EA11/EA20 e testes estão implementados. Fotos passam exclusivamente pelo backend (TseCandidatePhotos), com cache de 24h e fallback de inicial na UI. Deploy usa Dockerfile.vercel, Angular servido em wwwroot pela API e PORT=8080. Municípios, Redis, SSE/WebSocket, banco, histórico e testes de carga não estão implementados. Vercel pode criar/encerrar réplicas; não prometer coleta única global ou worker contínuo enquanto cache/agenda forem por processo.

Modo real exige validação do arquivo da eleição desejada; teste de primeiro turno não comprova segundo turno. Fuso de geração TSE é hipótese configurável, documentada. Cache é por processo e publica somente coletas completas de Brasil/UFs. Registro da licença específica da geometria ainda precisa de confirmação.

A instrução recebida referencia @RTK.md, mas o arquivo não foi localizado. Se passar a existir, ler suas instruções antes de trabalhar; não inventar seu conteúdo nem afirmar que foi seguido.

## O que não alterar sem necessidade

Preservar neutralidade, fronteira frontend/backend/TSE, contratos públicos, rastreabilidade de fixtures/geometria e distinção mock/real. Não trocar stack, remover testes, introduzir infraestrutura ou mudar critérios de totalização sem justificativa. Não desligar validação TLS para contornar falhas de rede.

## Handoff between AI agents

Antes de encerrar sessão significativa, atualizar docs/current-status.md e docs/roadmap.md; havendo decisão arquitetural, atualizar docs/decisions.md.

Registrar o que foi feito, arquivos principais, decisões, problemas conhecidos, validação e próximos passos. Somente contexto técnico necessário, sem pensamentos internos ou longas explicações.

Logs curtos opcionais: docs/sessions/YYYY-MM-DD-assunto.md. Não criar log para cada pequena alteração. O estado atual deve ser compreensível sem ler todos os logs. Caso a ferramenta não descubra AGENTS.md automaticamente, apontá-lo explicitamente.
