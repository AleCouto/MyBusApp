# 0005 — Sem dependências externas no código de produção

**Estado:** aceite · registado retroativamente em 2026-11-10

## Contexto

A aplicação é descarregada para o browser a cada visita e mantida por uma
pessoa. Cada pacote acrescenta peso de arranque, superfície de atualização e
mais um sítio onde a arquitetura pode divergir.

## Decisão

O código de produção usa apenas o SDK de Blazor WebAssembly, Bootstrap,
`System.Text.Json` e a biblioteca padrão. Mapeamento é manual, estado é feito
com classes simples e `event Action`, chamadas são diretas por DI.

A regra não se aplica às dependências de teste já declaradas em
`MyBusApp.Tests` (xUnit, test SDK, coverlet).

## Consequências

- Menos peso e menos manutenção; comportamento previsível.
- Mais código escrito à mão (mapeamentos, estado) — aceite como custo.
- Uma necessidade futura exige justificação explícita, não um `dotnet add`.

## Alternativas rejeitadas

- AutoMapper, MediatR, Fluxor: resolvem problemas que o projeto não tem, ao
  preço de convenções e reflexão.
- Kits de UI (MudBlazor, Radzen, Syncfusion, Telerik): substituiriam Bootstrap
  e trariam dependência de fornecedor.
