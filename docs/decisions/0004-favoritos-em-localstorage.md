# 0004 — Favoritos em localStorage, sem sincronização

**Estado:** aceite · registado retroativamente em 2026-11-10

## Contexto

Os favoritos são o centro da aplicação, mas não existe backend nem autenticação.
Precisam de sobreviver a reloads e de ser simples de guardar e reordenar.

## Decisão

Persistir em `localStorage` através de `IJSRuntime`, num único `FavoriteStore`
com chave versionada (`mybusapp:favorites:v1`), JSON com enums como string e
validação na leitura (entradas inválidas e duplicados). A identidade de cada
favorito é definida por modo e usa apenas identificadores, nunca nomes.

## Consequências

- Os favoritos ficam no browser: limpar dados ou mudar de dispositivo perde-os.
- Não há sincronização entre dispositivos nesta fase.
- Uma evolução do modelo obriga a versão nova da chave e a migração explícita.

## Alternativas rejeitadas

- `Blazored.LocalStorage`: dependência externa para uma chamada que o
  `IJSRuntime` já resolve.
- IndexedDB: complexidade desproporcionada para uma lista pequena.
- Backend de favoritos: fora do âmbito e exigiria autenticação.
