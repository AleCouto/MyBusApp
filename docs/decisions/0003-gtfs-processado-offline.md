# 0003 — GTFS processado offline e publicado como JSON versionado

**Estado:** aceite · registado retroativamente em 2026-11-10

## Contexto

Os feeds GTFS são ZIPs grandes, mudam com frequência e não são adequados para
o browser: o custo de download e de parsing cairia sobre o utilizador, e a
aplicação é client-side pura.

## Decisão

Processar o GTFS fora do browser com `Tools/GtfsDataGenerator`, gerar um
formato interno versionado (schema 2) e publicá-lo como `manifest.json` + um
JSON por linha lógica. A aplicação carrega o manifesto uma vez por sessão e só
o ficheiro da linha consultada.

O ZIP nunca é versionado; os JSON gerados podem ser, quando são os dados
publicados. O manifesto deve ter cache curto (ou revalidação); os ficheiros de
linha, que têm hash no nome, podem ter cache longo e imutável.

## Consequências

- O browser descarrega apenas o que consulta.
- Atualizar dados é um passo manual e explícito (ou automatizado mais tarde,
  no M15/M16).
- O formato interno é um contrato: mudá-lo exige versão nova e migração dos
  dados publicados.

## Alternativas rejeitadas

- Parse de GTFS no browser: descartado por peso e por responsabilidade.
- Backend que serve GTFS: contraria a arquitetura client-side e acrescenta
  infraestrutura para um problema resolvido offline.
