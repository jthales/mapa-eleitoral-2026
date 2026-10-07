# Turnos e fotos

- Implementados seletor, query round, caches/agendas isolados e consulta real de primeiro turno.
- Fotos oficiais resolvidas por EA11/EA20, cache do backend e fallback nos cards.
- Arquivos centrais: ElectionOptions, MemoryElectionCache, ElectionUpdateWorker, ConfiguredElectionProvider, TseCandidatePhotos, Api/Program e componentes Angular.
- Validação: 14 xUnit, 10 Vitest, build Angular; smoke real BR/27 UFs/12 fotos, desktop/celular, navegação e cancelamento.
- Segundo turno segue mock; pendências de fuso/licença/deploy mantidas. ADR-012 registra decisões.
