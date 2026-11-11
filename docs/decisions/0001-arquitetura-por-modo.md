# 0001 — Contratos separados por modo, sem abstração universal

**Estado:** aceite · registado retroativamente em 2026-11-10

## Contexto

A aplicação cobre autocarros e comboios. Os dois mundos não partilham
conceitos: autocarro resolve linha → sentido → paragem → chegada; comboio
resolve estação → destino → partida. Uma interface única teria de fingir que
"linha" e "estação" são a mesma coisa.

## Decisão

Manter `IBusService` e `ITrainService` separados. `BusServiceRouter` existe
apenas para autocarros e não há `TrainServiceRouter` nem router multimodal. Um
modo novo acrescenta o contrato que lhe é próprio ou reutiliza um existente.

## Consequências

- Cada modo evolui sem partir o outro; o fallback é definido por modo.
- Há duplicação de experiência (páginas e fluxos distintos) — aceite.
- Um modo novo obriga a uma decisão explícita, não a um encaixe forçado.

## Alternativas rejeitadas

- `ITransportService` universal: obrigaria a campos opcionais e a `if (modo)`
  espalhados pelos serviços.
- Herança entre serviços de modos diferentes: mistura responsabilidades e
  dificulta os testes.
