---
name: add-operator
description: Adicionar um novo operador ou fonte de transportes ao MyBusApp — decidir a fonte, gerar dados GTFS quando aplicável, implementar IBusService ou ITrainService, registar em DI, tratar UI, favoritos e testes. Usar quando o pedido envolve integrar uma operadora, um feed GTFS ou uma API novos.
---

# Adicionar um operador

Procedimento para integrar um operador ou fonte novos. O objetivo é acrescentar
dados reais, não preencher a interface.

## Quando usar

- Pedido para integrar uma nova empresa de transportes ou um feed GTFS.
- Pedido para acrescentar um modo novo (metro, barco) com fonte real.

Não usar para alterar um operador existente nem para corrigir dados de um feed
já integrado.

## 0. Ficha de decisão (antes de código)

Preenche, por operador: oficialidade da fonte, licença e permissão de
redistribuição, cobertura, formato (API, GTFS estático, ambos), existência de
realtime, frequência de atualização e estabilidade dos identificadores. Se
algum ponto não for verificável, **não integres** — fica registado como
candidato.

Resultado esperado: uma linha nova na tabela de operadores do
[`roadmap.md`](../../../docs/roadmap.md#41-operadores-e-fontes) e a decisão do
modo. O modelo completo está na secção 8 do mesmo documento.

## 1. Escolher o caminho

| Fonte | Modo | Implementação |
|---|---|---|
| API de autocarros | Autocarro | `IBusService` com DTOs próprios |
| GTFS estático | Autocarro | gerar JSON (skill `gtfs-pipeline`) + `IBusService` sobre o formato `GtfsStatic*` |
| GTFS estático | Comboio | herdar `GtfsTrainService` e registar `ITrainService` |
| Sem fonte real | Metro/Barco | não integrar; manter o estado de indisponibilidade |

## 2. Autocarro — `IBusService`

1. DTOs em `Models/DTOs/{Provider}/` (só para APIs; GTFS já tem formato
   partilhado).
2. Serviço em `Services/`, com `Provider`, `GetLineAsync`, `GetDirectionsAsync`,
   `GetStopsAsync` e `GetArrivalsAsync`.
3. Mapear DTO → `BusLine`, `BusDirection`, `BusStop`, `BusArrival` dentro do
   serviço. Nada de DTOs na UI.
4. Tratar `HttpRequestException`, `TaskCanceledException` e `IOException`:
   registar com `AppLogger` e devolver vazio/null.
5. Cache de referência com `_cached* ??=`; chegadas nunca são cacheadas.
6. `IsRealTime` só é `true` quando a fonte o garante.

## 3. Comboio — `ITrainService`

1. Herdar `GtfsTrainService` num serviço mínimo, como `CpService` e
   `FertagusService`: só passam o nome do operador e a base URL de recurso.
2. Registar `ITrainService` no `Program.cs` com uma fábrica que crie o
   `HttpClient` e injete `ApiSettings`/`AppLogger`.
3. Não criar um motor GTFS novo nem uma flag de realtime que a fonte não tem.

## 4. Registo e prioridade

- Autocarro: `builder.Services.AddScoped<IBusService, NovoServico>();`
- Comboio: `builder.Services.AddScoped<ITrainService>(sp => new NovoServico(...));`
- O `BusServiceRouter` ordena por prioridade fixa (Metropolitana, Carris
  Lisboa, Transitland). Se o novo provider precisar de outra posição, altera o
  router **e** os testes dele; um provider desconhecido cai para o fim.

## 5. Favoritos

Acrescentar um operador obriga a mexer em mais sítios do que parece:

- `Models/Domain/TransitFavorite.cs` → novo valor em `FavoriteProvider`.
- `Pages/Buses.razor` ou `Pages/Trains.razor` → conversão provider ↔ favorito.
- `Components/FavoritesDashboard.razor` → conversão inversa e nome apresentado.
- Testes de favoritos e de página.

Nomes apresentados nunca entram na identidade do favorito.

## 6. Interface

- Autocarro: a página `Pages/Buses.razor` já usa o router; confirma que a linha
  aparece com o operador certo.
- Comboio: `Pages/Trains.razor` injeta `IEnumerable<ITrainService>`; o novo
  operador aparece na lista automaticamente.
- Não apresentes um agregador como operador. Se o nome real não estiver
  validado, não inventes (ver
  [`docs/decisions/0002-transitland-e-fonte.md`](../../../docs/decisions/0002-transitland-e-fonte.md)).

## 7. Dados estáticos

Se for GTFS, usa a skill `gtfs-pipeline`. No fim:

- dados em `wwwroot/data/<operador>/`;
- `StaticDataBaseUrl` em `Configuration/Configuration.cs` e
  `wwwroot/appsettings.json`.

## 8. Testes mínimos

- Serviço: parsing/mapeamento, resposta vazia, erro de rede.
- Composição: o novo serviço é resolvido pelo contrato certo em DI.
- Router: prioridade e fallback (só autocarros).
- Página: o operador aparece e é selecionável.
- Favoritos: identidade e conversão para o novo provider.

```bash
dotnet test MyBusApp.Tests/MyBusApp.Tests.csproj
```

## 9. Fecho

- [ ] Ficha de decisão preenchida e registada no roadmap.
- [ ] `docs/architecture.md` atualizado se o fluxo ou o DI mudaram.
- [ ] `docs/domain.md` atualizado se a semântica mudou.
- [ ] ADR novo se houve uma decisão estrutural (não para uma integração
      rotineira).
- [ ] Sem dados fictícios, sem `IsRealTime` otimista, sem chaves prometidas
      como secretas.
