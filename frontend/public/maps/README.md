# Geometria do Brasil

Fonte: [API de malhas geográficas do IBGE](https://servicodados.ibge.gov.br/api/docs/malhas?versao=3).

URL coletada em 2026-10-07:
https://servicodados.ibge.gov.br/api/v3/malhas/paises/BR?formato=application/vnd.geo+json&qualidade=minima&intrarregiao=UF

- brazil-states.geojson: resposta original, 27 features, qualidade mínima.
- brazil-states.json: derivado local com paths SVG e centros para rótulos. Regenerar na raiz com `node scripts/generate-map.mjs`.
- A conversão usa projeção equiretangular simplificada, sem dependência cartográfica e sem alterar resultados eleitorais.
- codarea é mapeado explicitamente para as 27 siglas de UF. Não há mapeamento municipal.

Atribuição IBGE é exibida na interface. **Licença específica de redistribuição: ainda não confirmada na fonte consultada**; não atribuir CC/MIT por suposição. A fonte pública e a derivação estão registradas para revisão antes de publicação. Nenhum mapa foi publicado nesta sessão.
