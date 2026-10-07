# Fixtures oficiais

Coleta em 2026-10-07, via HTTPS; conteúdo original preservado, sem substituir candidatos. Estes arquivos são exclusivamente insumos de teste do parser, não mocks nem dados servidos em runtime.

- `ea11.json`: https://resultados.tse.jus.br/oficial/comum/config/ele-c.json — HTTP 200.
- `ea20-6257-br.json`: https://resultados.tse.jus.br/oficial/ele2026/6257/dados/br/br-c0001-e006257-u.json — HTTP 200; primeiro turno, usado apenas para verificar o layout real.

O mock da aplicação está em `Infrastructure/MockElectionProvider.cs` e usa somente Candidato A/B. Testes que alteram a fixture em memória são cenários sintéticos identificados por seus nomes, não novos registros oficiais.
