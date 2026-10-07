# Decisões arquiteturais

ADRs iniciais de 2026-10-07. `Accepted` significa escolha aprovada no escopo, **não implementação concluída**. `Proposed` indica decisão em aberto. O estado inicial foi apenas documental; a implementação do MVP em 2026-10-07 está registrada nas atualizações abaixo.

## ADR-001 - Backend intermediando TSE

### Status
Accepted

### Context
O Mapa Eleitoral 2026 exibirá resultados oficiais com múltiplos usuários e atualizações frequentes.

### Decision
Somente o backend consulta o TSE. Separar acesso HTTP, parsing, modelos internos, normalização e DTOs da API.

### Reason
Requisito explícito; centraliza chamadas e isola o frontend de layouts externos.

### Consequences
Angular consome a API própria. Layouts e URLs precisam de evidência oficial antes do parser. EA11/EA20 já implementados e verificados no primeiro turno.

## ADR-002 - Angular por feature

### Status
Accepted

### Context
O MVP precisa de dashboard responsivo, mapa vetorial e detalhes de UF.

### Decision
Usar Angular 20 ou estável mais recente compatível, TypeScript, SCSS, standalone components, HttpClient e signals quando úteis; organização por feature e sem biblioteca visual pesada inicialmente.

### Reason
Stack e princípios solicitados pelo usuário.

### Consequences
Versões adotadas e componentes implementados constam no README; Angular 22, Node 24 e Vitest.

## ADR-003 - .NET 10 sem banco no MVP

### Status
Accepted

### Context
A API deve expor dados normalizados com cache e atualização periódica sem complexidade desnecessária.

### Decision
ASP.NET Core Web API, .NET 10 e C#, com separação Api/Application/Domain/Infrastructure, IHttpClientFactory e testes xUnit. Não adicionar banco nesta primeira versão.

### Reason
Stack solicitada; favorece separação de responsabilidades com escopo contido.

### Consequences
Projetos Api/Application/Domain/Infrastructure e testes implementados. Histórico persistente não faz parte do MVP.

## ADR-004 - IMemoryCache com abstração

### Status
Accepted

### Context
Consultas dos usuários não devem gerar uma chamada ao TSE por acesso.

### Decision
Usar IMemoryCache atrás de IElectionCache inicialmente; manter possibilidade de implementação Redis sem adotá-lo agora.

### Reason
Requisito explícito e suficiente para a base inicial em uma instância.

### Consequences
Cache é local ao processo e não sobrevive a reinícios. Uma chave conserva o último snapshot, sem TTL destrutivo; múltiplas instâncias exigirão revisão. Redis não está instalado nem tem migração aprovada.

## ADR-005 - Worker e último snapshot válido

### Status
Accepted

### Context
O resultado precisa se atualizar sem depender de cada requisição da interface e tolerar falhas externas.

### Decision
ElectionUpdateWorker com BackgroundService; intervalo configurável, inicialmente 30 segundos em desenvolvimento. Usar timeout, retry simples, cancelamento e logging; conservar último snapshot válido e expor fresh/stale.

### Reason
Requisitos de resiliência e atualização do escopo.

### Consequences
Critérios implementados: frescor por coleta de até 90s; sem cache, HTTP 503; até duas retentativas transitórias. Não prometer atualização oficial a cada 30 segundos.

## ADR-006 - Mock explícito

### Status
Accepted

### Context
A interface deve poder ser desenvolvida independentemente de dados reais do segundo turno.

### Decision
MockElectionProvider selecionado por Election:UseMockData, com Candidato A, Candidato B, 26 estados e DF, totalizações variadas e estados sem apuração. Sinalizar a simulação na interface.

### Reason
Reproduzibilidade sem fabricar resultados oficiais ou hardcodar candidatos reais.

### Consequences
Não trocar para mock silenciosamente quando o TSE falhar. Provider, fixtures e configuração foram implementados.

## ADR-007 - Transporte de atualização da interface

### Status
Accepted (atualizado em 2026-10-07; inicialmente Proposed)

### Context
O backend atualiza o cache periodicamente e a interface precisa refletir mudanças.

### Decision
Polling de 30 segundos para a API própria no MVP, sem sobreposição. SSE/WebSocket não implementados.

### Reason
Atende o fluxo atual sem manter conexões persistentes e infraestrutura adicional.

### Consequences
A interface pode atrasar até um ciclo de polling após a coleta. Avaliar SSE/WebSocket com requisitos e carga reais.

## ADR-008 - Biblioteca de mapa e GeoJSON

### Status
Accepted para a escolha técnica (atualizado em 2026-10-07; inicialmente Proposed)

### Context
Mapa vetorial de 27 UFs com hover/foco, navegação, cores por candidato e ausência de dados.

### Decision
SVG local gerado da malha simplificada da API IBGE, sem biblioteca cartográfica. Fonte e derivação preservadas; licença específica ainda pendente de confirmação.

### Reason
Solução pequena, acessível por links/tabela e sem consultas geográficas externas no navegador.

### Consequences
Geometria é estática e simplificada. Revisar licença antes de publicação e usar outra malha para futuros municípios. Não atribuir licença por suposição.

## ADR-009 - Contexto persistido em Markdown

### Status
Accepted

### Context
O usuário quer alternar entre Codex, Claude Code, Cursor e outros agentes sem depender da conversa.

### Decision
AGENTS.md é a entrada; current-status e roadmap são atualizados após sessões significativas; decisões arquiteturais ficam neste arquivo e logs curtos são opcionais.

### Reason
Contexto técnico portátil, revisável e separado do histórico de chat.

### Consequences
Manter distinção entre pedido, decisão e implementação. Documentos e base do MVP existem; funcionalidades pendentes constam no roadmap. Se um agente não descobrir AGENTS automaticamente, apontá-lo explicitamente.

## Atualização de implementação — 2026-10-07

- ADR-001/003: API, camadas, DTOs e integração EA11/EA20 implementados. ITseElectionService lê o cache; IElectionProvider isola a aquisição externa. xUnit e testes de API aprovados.
- ADR-002: selecionados Angular 22.2.1 e CLI/build 22.2.2, TypeScript 6.0.3, Vitest 4.1.11. Node verificado 24.18.0; versões reproduzidas por package-lock.json. Standalone, signals, HttpClient e lazy routes implementados.
- ADR-004/005: IMemoryCache sem expiração destrutiva, fresh/stale por sucesso/idade da coleta; 30s de atualização, 90s para stale por idade, backoff mínimo de 600s após falhas. Cache conserva dados até reinício; não é persistência.
- ADR-006: mock explícito implementado e ativo por padrão, com 27 UFs e totais coerentes. Nenhuma falha real ativa o mock automaticamente.
- ADR-007: decisão do MVP agora Accepted: polling de 30s da API própria, sem sobreposição. SSE/WebSocket ficam para avaliação com carga real.
- ADR-008: escolha técnica agora Accepted: SVG local gerado do GeoJSON simplificado da API IBGE, sem biblioteca cartográfica. Fonte e derivação em frontend/public/maps/README.md. Licença específica ainda não confirmada; não apresentar essa pendência como resolvida.
- ADR-009: README, estado, roadmap e log de implementação atualizados; registros iniciais acima preservam o contexto da decisão.

## ADR-010 - Publicar coletas completas e validar origem

### Status
Accepted

### Context
Arquivos BR e UFs podem chegar em gerações diferentes, e uma falha parcial não deve apagar o último resultado válido.

### Decision
Obter Brasil e 27 UFs sequencialmente e substituir o snapshot somente após validar todo o conjunto. Rejeitar regressão temporal por abrangência e identificar stale em falhas. Validar eleição/turno/cargo/fase/abrangência e host do template antes de publicar.

### Reason
Mantém um modelo simples e impede combinação silenciosa de eleições ou perda de dados por resposta inválida.

### Consequences
Uma UF indisponível atrasa a atualização do conjunto inteiro. O snapshot é atômico no cache, mas a geração upstream de cada arquivo pode ser diferente. Sem cache inicial, retornar 503; evolução futura pode adicionar snapshots independentes por UF.

## ADR-011 - Não inferir confirmação oficial

### Status
Accepted

### Context
Percentual de apuração, andamento e situação do candidato têm significados distintos no EA20.

### Decision
Preservar percentuais oficiais, respeitar dv=n e só expor st quando tf=s. Não converter e=s, liderança ou 100% em declaração de vencedor. Fuso de geração sem offset é hipótese operacional configurável, atualmente -180 minutos, pendente de confirmação oficial.

### Reason
Evita conclusões eleitorais que o arquivo não sustenta e torna a hipótese temporal auditável.

### Consequences
API distingue hasData/votesReleased/status/officialStatus. Testes cobrem voto retido e status prematuro. Validar fuso antes de operação eleitoral.

## ADR-012 - Consulta por turno e fotos oficiais

### Status
Accepted — 2026-10-07

### Context
Usuário solicitou consulta ao primeiro turno e fotos para facilitar identificação dos candidatos.

### Decision
Preservar segundo turno como padrão; aceitar round=1|2 na API e URL da UI. Separar snapshots, falhas e agendas. Primeiro turno usa TSE real com intervalo 600s, segundo mantém configuração mock/30s enquanto resultado real não foi validado. Trocar turno cancela requisições e remove dados do turno anterior.

Fotos vêm de ft/EA11 e sqcand/EA20; download pelo worker e cache em memória de 24h, servido por rota local apenas para candidatos do snapshot real. Ausência/erro usa inicial, sem bloquear resultados.

### Consequences
Nenhuma chamada ao TSE é disparada pelo navegador. Carga externa independe dos visitantes; caches desaparecem ao reiniciar. Consulta dos dois turnos não implementa comparação analítica nem histórico temporal. Primeiro turno requer internet por padrão; flags de mock separados permitem uso offline.

## Ajuste visual — 2026-10-07

Por solicitação explícita do usuário, a paleta por posição do ID foi substituída por PARTY_MAP_COLORS (21 partidos, valores fornecidos no pedido). Siglas são normalizadas sem acentos e sem distinção de caixa; desconhecidos usam cinza. Isso substitui a convenção visual anterior de cores independentes de partido, sem alterar neutralidade, votos ou critérios de liderança.

## ADR-013 - Classificados oficiais enquanto o segundo turno não tem resultados

Accepted — 2026-10-07. Usuário autorizou substituir demonstração por nomes/fotos oficiais. Habilitar modo real para segundo turno. Sem snapshot 2, derivar somente identidades de exatamente dois classificados oficiais em primeiro turno real concluído. API explicita candidatesOnly e usa null em métricas/electionId; não reutiliza votos nem cria snapshot oficial fictício. Datas e stale são do primeiro. Snapshot real posterior tem prioridade. Preservar worker/cache por instância e registrar custo por ciclo; nenhuma chamada upstream depende do número de visitantes.

## ADR-014 - Reduzir atraso na atualização do segundo turno

Accepted — 2026-10-07. A pedido do usuário, segundo turno passa a alvo de 10s entre inícios de coletas bem-sucedidas, stale após 30s. Worker mantém execução sequencial: uma coleta lenta ou coleta do primeiro turno pode atrasar o próximo início; não há sobreposição ou fila de ciclos perdidos. Interface consulta cache a cada 5s no segundo turno; primeiro mantém 30s na UI e 600s upstream. Backoff de falhas permanece 600s.

TSE confirma 100 requisições/s/IP, com possível bloqueio por excesso ou 404 repetidos. 29 arquivos/ciclo equivalem nominalmente a 2,9 requisições/s em média; espaçamento sequencial 150ms preservado, fotos/retries adicionais. Essa configuração não garante atualização ponta a ponta em 10s, pois depende de publicação/CDN, duração de coleta e polling. Cache segue por processo, sem teste de carga ou coordenação de réplicas.

## ADR-015 - Hospedar Angular e .NET na Vercel

Accepted — 2026-10-07, por solicitação do usuário. Usar Container Images (beta) no plano Hobby, preservando a stack: build Angular/Node 24 e publish .NET 10 em Dockerfile.vercel; ASP.NET serve wwwroot e API na mesma origem. Projeto conectado ao GitHub privado, branch main. PORT=8080; /health/live separado da saúde dos snapshots; fallback SPA não encobre 404 de /api/.

Build remoto e acesso público foram verificados. Esta decisão é de hospedagem inicial, não resolve coordenação de réplicas: cache e worker continuam por instância e podem desaparecer com scale down. A independência entre visitantes e chamadas TSE registrada anteriormente vale dentro de cada instância; autoscaling pode multiplicar coletas. Redis/coordenação e validação de coleta contínua permanecem pendentes. Nenhum plano pago contratado. Fontes e procedimento em deployment.md.
