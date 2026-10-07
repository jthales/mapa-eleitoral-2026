# Hospedagem na Vercel

Configuração de 2026-10-07: projeto `jthales-projects/mapa-eleitoral-2026`, plano Hobby, conectado ao repositório privado `jthales/mapa-eleitoral-2026`. Branch de produção: `main`.

Site público: https://mapa-eleitoral-2026-omega.vercel.app. Build remoto concluído e acesso sem autenticação verificado, incluindo API real, classificados, fotos, mapa, deep link estadual e viewport móvel.

## Construção e execução

`Dockerfile.vercel` compila Angular com Node 24 e publica ASP.NET Core com .NET 10. O container final serve arquivos Angular em `wwwroot` e a API na mesma origem. `vercel.json` encaminha as rotas ao serviço `app`. `PORT=8080` está configurado em Production/Preview; não há segredos de aplicação necessários para ler o TSE.

A API aceita `PORT`, disponibiliza `/health/live` e retorna `index.html` em deep links do Angular. Rotas desconhecidas sob `/api/` retornam 404. `/health?round=1|2` continua refletindo disponibilidade dos snapshots, podendo retornar 503 mesmo com o processo saudável.

Para publicar manualmente, na raiz e com Vercel CLI autenticado: `npx vercel deploy --prod`. O GitHub conectado permite deploys por push. Arquivos `.env*`, `.vercel`, dependências, builds locais e ferramentas não devem ser versionados nem enviados no contexto Docker.

## Limites operacionais

Container Images é um recurso beta da Vercel, disponível em todos os planos. Hobby está sujeito às franquias e regras do provedor; hospedagem neste plano não é promessa de capacidade ilimitada.

O worker/cache continua por processo. Cada réplica pode executar sua própria coleta; reinícios perdem resultados e fotos. A plataforma pode suspender/encerrar instâncias sem tráfego. Portanto, esta configuração não garante coleta permanente a cada 10 segundos nem consulta upstream única para todos os visitantes. No início, a API pode retornar 503 até concluir a coleta.

Antes de usar como infraestrutura para alto tráfego durante a apuração: adotar armazenamento/cache compartilhado e coordenação de coleta, verificar execução do agendador e medir latência/carga. Não foi contratado serviço pago nem configurado Redis nesta entrega.

Fontes: [Container Images](https://vercel.com/docs/functions/container-images), [ASP.NET Core com Docker](https://vercel.com/kb/guide/dot-net-asp-net-on-vercel-with-docker).

## Recuperação na inicialização

A API aguarda até 25s pela primeira coleta do worker antes de responder sem snapshot. Turno 1 tem prioridade, também permitindo a projeção de classificados do segundo. O navegador repete falhas transitórias duas vezes com intervalo de 3s. Falhas prolongadas ainda retornam indisponibilidade; último snapshot válido continua preservado durante a vida da instância. Essa melhoria não resolve perda de cache em reinícios.
