# Convenções

Convenções aplicadas na base do MVP. Prettier está configurado no frontend; dotnet format é usado no backend. Ainda não há CI nem linter adicional.

## Backend

- Naming: PascalCase para tipos e membros públicos, camelCase para parâmetros e variáveis; interfaces com prefixo `I` e métodos assíncronos com sufixo `Async`.
- Usar `async/await` para I/O e evitar bloqueios com `.Result` ou `.Wait()`.
- Propagar `CancellationToken` da requisição/worker para serviços e chamadas HTTP; tratar cancelamento como fluxo esperado.
- Separar DTOs externos do TSE dos contratos da API; conferir tipos, formatos numéricos, nulabilidade e estados oficiais antes de mapear.
- Criar interfaces em fronteiras úteis, como integração externa e leitura de snapshots; evitar abstrações sem necessidade.
- Usar logging estruturado com contexto de eleição/abrangência e falha. Não registrar segredos ou payloads completos indiscriminadamente.
- Tratar falhas externas, conteúdo inválido e ausência de dados explicitamente. Não converter exceções em resultado eleitoral vazio ou zero votos silenciosamente.
- Centralizar tratamento de erros da API quando criada; não expor stack traces em respostas públicas.

## Frontend

- Preferir standalone components; implementados com Angular 22.
- Preferir signals para estado local e derivado, com estado assíncrono explícito; ver features/dashboard e features/map.
- Naming: arquivos em kebab-case, classes/tipos em PascalCase e membros em camelCase. Seguir também as convenções do gerador da versão escolhida.
- Organizar por feature, mantendo componentes, models e services próximos ao uso. Diretórios reais estão em architecture.md.
- Services acessam somente a API própria; não incluir URLs de consulta ao TSE no frontend.
- Models devem representar os contratos reais da API, sem copiar abreviações externas por conveniência.
- Usar SCSS por componente e compartilhar estilos apenas quando reutilizados; tokens visuais globais estão em src/styles.scss; estilos específicos do mapa ficam no componente.
- Distinguir carregamento, falha, dados desatualizados e modo mock; não apresentar fixture como apuração real.

## Testes e documentação

- Preservar testes existentes quando criados e registrar os comandos reais.
- Testar comportamento relevante: normalização, estados oficiais, falhas externas, cache e identificação de mocks. Não supor que build equivale a teste funcional.
- Usar xUnit no backend. No frontend, adotar o runner padrão recomendado para a versão Angular efetivamente escolhida, sem presumir qual será antes dessa decisão.
- Fixtures eleitorais reais precisam de fonte oficial, eleição/ambiente e data de coleta. Exemplos sintéticos devem ser identificados e usar nomes fictícios.
- Atualizar estado, roadmap e ADRs ao concluir trabalho significativo, conforme `AGENTS.md`.

## Git

Sugestão de prefixos: `feat:`, `fix:`, `refactor:`, `test:`, `docs:` e `chore:`. Usar descrição objetiva e commits coesos quando houver repositório Git. Não foi definida política de branches, CI ou publicação.

## Ferramentas e verificação atuais

Backend: dotnet build/test backend/Election.slnx; formatação com dotnet format. Frontend: npm run build, npm test (Vitest, sem watch) e Prettier conforme AGENTS. Preferir npm ci para preservar o lockfile.

A API pública mantém camelCase via ASP.NET; IDs e número eleitoral são strings. DTOs são imutáveis no backend; não expor JsonElement ao frontend. A URL base real é validada e não aceita hosts arbitrários.
