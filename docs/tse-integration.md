# Integração com o TSE

Verificação: 2026-10-07. Implementação em backend/Infrastructure/Tse; ambos os turnos reais por padrão; sem resultado do segundo, exibir apenas classificados oficiais do primeiro. Documentação confirmada não equivale à disponibilidade futura de resultados.

## Fontes oficiais

- [Página técnica/FAQ 2026](https://www.tse.jus.br/eleicoes/informacoes-tecnicas-sobre-a-divulgacao-de-resultados).
- [Instruções de download v1.0, 25/05/2026](https://www.tse.jus.br/eleicoes/eleicoes-2026-content/arquivos/divulgacao-de-resultados/tse-instrucoes-para-download-dos-arquivos-da-divulgacao-2026).
- [EA11, 23/06/2026](https://www.tse.jus.br/eleicoes/eleicoes-2026-content/arquivos/divulgacao-de-resultados/tse-ea11-arquivo-de-configuracao-de-eleicoes).
- [EA12, 22/05/2026](https://www.tse.jus.br/eleicoes/eleicoes-2026-content/arquivos/divulgacao-de-resultados/tse-ea12-arquivo-de-configuracao-de-municipios).
- [EA14, 10/06/2026](https://www.tse.jus.br/eleicoes/eleicoes-2026-content/arquivos/divulgacao-de-resultados/tse-ea14-arquivo-de-acompanhamento-brasil).
- [EA15](https://www.tse.jus.br/eleicoes/eleicoes-2026-content/arquivos/divulgacao-de-resultados/tse-ea15-arquivo-de-acompanhamento-uf).
- [EA20, 10/07/2026](https://www.tse.jus.br/eleicoes/eleicoes-2026-content/arquivos/divulgacao-de-resultados/tse-ea20-arquivo-de-resultado-unificado).

## Arquivos e uso no projeto

| Arquivo | Finalidade confirmada | Uso atual |
| --- | --- | --- |
| EA11 | Configuração de eleições | Descoberta/validação de eleição e diretório |
| EA12 | Configuração de municípios | Não consumido; evolução municipal |
| EA14 | Acompanhamento Brasil | Não consumido; possível otimização |
| EA15 | Acompanhamento UF | Não consumido; download do layout bloqueado |
| EA20 | Resultado unificado | Brasil e 27 UFs para Presidente |

CONFIRMADO NA DOCUMENTAÇÃO — nomes/finalidades no catálogo oficial. EA15: AINDA NÃO CONFIRMADO nesta sessão o dicionário completo; resposta recebida pelo download não era PDF válido. Não foi inventado parser para ele.

## URLs e construção

CONFIRMADO NA DOCUMENTAÇÃO — conforme EA11 e instruções: configuração em `[base]/[ambiente]/comum/config/ele-c.json`; `arq[].dir` informa templates. EA20 nacional/UF termina em `{uf}-c0001-e{eleicao:6}-u.json`; cargo Presidente 0001, UF minúscula, eleição com seis dígitos no nome. O diretório usa o código sem esse preenchimento.

A implementação substitui tokens do diretório recebido do EA11, valida host HTTPS e rejeita tokens desconhecidos. A origem real é resultados.tse.jus.br, ambiente oficial. Não constrói URLs com nomes de candidatos.

| Consulta de 2026-10-07 | Resultado observado |
| --- | --- |
| [EA11 oficial](https://resultados.tse.jus.br/oficial/comum/config/ele-c.json) | 200, fixture preservada |
| [EA20 Brasil/6257](https://resultados.tse.jus.br/oficial/ele2026/6257/dados/br/br-c0001-e006257-u.json) | 200, fixture preservada |
| [EA20 Brasil/6258](https://resultados.tse.jus.br/oficial/ele2026/6258/dados/br/br-c0001-e006258-u.json) | 404 nesta consulta |
| EA20 das 27 UFs/6257, mesmo template validado | Uma coleta pelo provedor .NET passou |

Essas observações são resultados dos testes desta sessão. Um 404 não demonstra por si só a data em que o arquivo será publicado.

## Códigos

CONFIRMADO NA DOCUMENTAÇÃO E NO EA11 COLETADO — pleito 3220, ciclo ele2026, federal 6257/turno 1 com cdt2=6258; estadual 6259 com cdt2=6260; Conselho Distrital 6261. Fontes: página técnica e fixture EA11.

O provedor valida eleição/turno/cargo no EA11; para o turno 2 aceita referência cdt2 de uma eleição de turno 1 e valida novamente o turno no EA20. Não usa o exemplo inicial que combinava 6257 e turno 2 como dado oficial.

## Campos normalizados

CONFIRMADO NA DOCUMENTAÇÃO — EA20, especialmente páginas 8–16:

| Campo/caminho | Significado → destino |
| --- | --- |
| ele, t, f | Eleição, turno, fase; validação |
| tpabr, cdabr | Tipo/código da abrangência; validação |
| dg, hg | Geração → updatedAt |
| and | n/p/f → not_started/in_progress/completed |
| tf | Indica totalização final |
| dv | Liberação da votação; n suprime votos |
| s.ts, s.st | Seções totais/totalizadas → progresso |
| carg[].cd | Cargo; selecionar Presidente |
| agr[].par[].sg | Sigla partidária |
| par[].cand[].sqcand, n | ID e número do candidato |
| cand[].nmu | Nome de urna |
| cand[].vap | Votos computados |
| cand[].pvapn / pvap | Percentual publicado |
| cand[].st | Situação oficial após totalização final |

O parser usa a hierarquia `carg → agr → par → cand`. Aceita números representados como strings e decimais com vírgula; não usa cultura do sistema. `e=s` também pode indicar segundo turno, portanto nunca é convertido isoladamente em vitória. `and=f` e `tf=s` não são sinônimos; a UI não infere vencedor pela totalização.

EA11: CONFIRMADO NA DOCUMENTAÇÃO — `pl[].c` é ciclo; `pl[].e[].cd/t/cdt2` identifica eleição/turno/segundo turno; `arq[].tp/dir` define diretórios; `abr[].cp[].cd` identifica cargo.

EA12: CONFIRMADO NA DOCUMENTAÇÃO — `abr[].cd/ds` identifica UF/nome; `mu[].cd/cdi/nm` corresponde a código TSE/IBGE/nome municipal. Não assumir equivalência entre códigos ou truncar identificadores.

EA14: CONFIRMADO NA DOCUMENTAÇÃO — `abr[].tpabr/cdabr` identifica abrangência, `dt/ht` informa totalização, `and` andamento; `s` reúne seções. Consulta ainda não usada no runtime.

## Exemplo real mínimo

Trecho do arquivo EA20 Brasil/6257 coletado, com outros campos omitidos:

```json
{
  "ele": "6257",
  "t": "1",
  "f": "o",
  "tpabr": "br",
  "cdabr": "br",
  "dg": "05/10/2026",
  "hg": "12:51:47",
  "dv": "s",
  "tf": "s",
  "and": "f"
}
```

A fixture completa fica em backend/Tests/Fixtures/ea20-6257-br.json. Não é mock nem resposta hardcodada da API; candidatos reais só aparecem nesse insumo oficial de teste e nas respostas obtidas do TSE.

AINDA NÃO CONFIRMADO — offset/fuso dos campos dg/hg. O projeto assume -03:00 por configuração SourceUtcOffsetMinutes=-180 e documenta a hipótese. Confirmar antes de operação eleitoral. A hora de coleta é registrada independentemente em UTC.

## Disponibilidade, frequência e limites

CONFIRMADO NA DOCUMENTAÇÃO — [FAQ](https://www.tse.jus.br/eleicoes/informacoes-tecnicas-sobre-a-divulgacao-de-resultados): máximo de 100 requisições/IP/segundo, incluindo 304; excesso pode bloquear por 10 minutos, com reinício do bloqueio em novas tentativas. Repetidos 404 também podem bloquear. EA14/EA15 podem orientar consultas, mas arquivos distribuídos pela CDN podem chegar em momentos diferentes.

AINDA NÃO CONFIRMADO — SLA/cadência garantida de geração e cronograma exato dos arquivos de segundo turno. Os 30 segundos são escolha do projeto, não promessa do TSE. Não afirmar que todos os arquivos só existem durante apuração: EA11 e EA20 de primeiro turno estavam acessíveis; EA20 de segundo turno não estava.

No runtime: chamadas sequenciais com espaçamento de 150ms, timeout de 15s e até duas retentativas para rede/timeout/5xx. Falha de ciclo espera 600s; 403/404/429 não têm retry imediato. Uma implantação com vários processos/IP compartilhado exige orçamento agregado e futura coordenação.

## Decisões e limitações

- Backend é o único consumidor, com parser separado e DTOs legíveis.
- Cache publica somente coleta completa BR/27 UFs; qualquer falha mantém último snapshot e marca stale.
- Primeiro carregamento indisponível retorna 503; não há dados falsos para preencher ausência.
- Mock depende de UseMockData (segundo) ou FirstRoundUseMockData (primeiro) explícito, sem fallback silencioso.
- O resultado nacional real vem de BR, não da soma das UFs (abrangência nacional inclui exterior).
- Sem parser municipal, EA14/EA15, verificação JWS, cache distribuído ou teste de carga nesta entrega.
- Próxima validação: resultado efetivo do segundo turno, cenários de início/retotalização e confirmação do fuso.

## Consulta de turnos e fotos — 2026-10-07

GET da API própria aceita round=1|2, padrão 2. O worker usa 6257 no primeiro e 6258 no segundo; agendas e snapshots independentes. Primeiro real verificado novamente para Brasil e 27 UFs. Segundo habilitado em modo real; EA20 nacional ainda retorna 404 na verificação. CandidatesOnly é projeção local da classificação do primeiro, não arquivo de resultados do segundo.

CONFIRMADO NA DOCUMENTAÇÃO — [instruções de download, páginas 5–6](https://www.tse.jus.br/eleicoes/eleicoes-2026-content/arquivos/divulgacao-de-resultados/tse-instrucoes-para-download-dos-arquivos-da-divulgacao-2026): fotos têm nome sqcand.jpeg no diretório de fotos da abrangência. EA11 fornece o template ft; EA20 fornece sqcand. Presidente usa br, inclusive quando apresentado nas UFs.

ACESSO TESTADO — 12 fotos de candidatos da eleição 6257 retornaram JPEG e foram decodificadas no navegador via API local. Caminho resolvido: https://resultados.tse.jus.br/oficial/ele2026/6257/fotos/br/{sqcand}.jpeg. Nenhum nome/identificador real foi hardcodado.

TseCandidatePhotos baixa no worker, cacheia 24h por eleição/candidato e interrompe o lote em falha com backoff de 600s; candidato com foto falha espera 24h. Endpoint público apenas lê memória, sem proxy arbitrário. Resultados continuam disponíveis quando fotos falham.

Cadência revisada em 2026-10-07: segundo turno alvo 10s entre inícios de coleta após sucesso; primeiro 600s, falhas 600s. Frontend consulta somente cache a cada 5s no segundo turno. Limite 100 requisições/IP/s reconsultado na FAQ oficial; sem promessa de SLA de geração.
