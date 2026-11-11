# 0006 — Tempo real quando válido, programado como fallback explícito

**Estado:** aceite · registado retroativamente em 2026-11-10

## Contexto

Só a Carris Metropolitana fornece previsões em tempo real. As restantes fontes
servem horários planeados. Mostrar tudo como "previsão" daria ao utilizador uma
confiança que os dados não sustentam; esconder o horário planeado quando o
realtime falha tornaria a aplicação inútil no momento em que é mais precisa.

## Decisão

1. Usar a previsão realtime quando existe e é válida.
2. Caso contrário, usar o horário programado aplicável, quando existe.
3. Rotular sempre: "Tempo real" ou "Agendado". Nunca misturar.
4. Sem dados válidos: distinguir resposta vazia, falha de serviço e dados
   indisponíveis — não inventar nem reutilizar dados expirados.

Os três fallbacks (previsões, entre providers, distribuição) são problemas
distintos e não partilham uma abstração única.

## Consequências

- A UI tem de carregar a proveniência de cada partida, não só a hora.
- A regra é aplicada por operador, o que dá trabalho (M13) mas evita um motor
  universal prematuro.
- Um horário planeado pode ser menos útil do que uma previsão, mas nunca é
  enganador.

## Alternativas rejeitadas

- Mostrar apenas tempo real: deixaria a maioria dos operadores sem resposta.
- Apresentar horários planeados como previsão: viola a integridade dos dados.
