# Arquitetura

> Verificado: 2026-11-10 @ `5549968`
>
> Descreve como o código **está**, não como devia estar. Se alterares a
> estrutura, atualiza este ficheiro no mesmo commit. As regras de estilo e as
> decisões históricas estão em [`conventions.md`](conventions.md) e
> [`decisions/`](decisions/).

## 1. Forma da aplicação

Blazor WebAssembly standalone, `net9.0`, sem backend. Tudo corre no browser:
não há servidor para esconder chaves, agregar respostas ou guardar estado. Duas
consequências permanentes:

- Qualquer valor em `wwwroot/appsettings.json` é público.
- O arranque é um download; cada dependência nova paga-se em tempo de carga.

`Program.cs` é a raiz de composição: configuração, DI e nada de lógica.

## 2. Consulta de autocarro

```text
Pages/Buses.razor
  → BusServiceRouter (resolve a linha)
      → CarrisMetropolitanaService   (API, realtime)
      → CarrisLisboaService          (JSON estático GTFS)
      → TransitlandService           (API, horários planeados)
  → IBusService escolhido (sentidos, paragens, chegadas)
  → DTO → Models/Domain → página
```

- O router injeta `IEnumerable<IBusService>` e ordena por prioridade:
  Metropolitana, Carris Lisboa, Transitland. Um serviço desconhecido cai para o
  fim.
- O fallback entre providers acontece **só na resolução da linha**. Depois de
  resolvida, a página usa o serviço que a resolveu (ou o provider pedido
  explicitamente em `ResolveLineAsync(provider, line)`).
- Se um serviço lança, o router regista, tenta o seguinte e, se nenhum
  resolver, relança a última falha. Os serviços individuais tratam
  `HttpRequestException`, `TaskCanceledException` e `IOException`, registam e
  devolvem vazio/null em vez de derrubar a UI.
- Cache por serviço (scoped, logo por circuito de utilizador): `_cachedLines`,
  `_cachedRoutes`, `_cachedPatterns`, `_cachedStops`, com `??=`. Dados de
  referência ficam em memória; chegadas nunca são cacheadas.

## 3. Consulta de comboio

```text
Pages/Trains.razor
  → IEnumerable<ITrainService> (CP, Fertagus)
  → GtfsTrainService (motor comum: manifesto + JSON por linha)
```

- Não há router ferroviário. A página deixa escolher o operador e injeta todos
  os `ITrainService`.
- `GtfsTrainService` é a base de `CpService` e `FertagusService`; cada um só
  fixa o nome do operador e a `StaticDataBaseUrl` de recurso.
- O domínio ferroviário não tem flag de tempo real: `TrainDeparture` não tem
  `IsRealTime`, por isso comboios são sempre apresentados como agendados.
- As partidas são derivadas do GTFS estático (calendário, exceções, horas
  superiores a 24h) e agrupadas por estação e destino.

## 4. Dados estáticos GTFS → JSON

- `Tools/GtfsDataGenerator` é uma consola offline. **Nunca** se processa um ZIP
  GTFS no browser.
- O formato é versionado (`GtfsStatic*`, versão 2) e publicado como
  `manifest.json` + um JSON por linha lógica.
- O manifesto tem `version`, `generatedAtUtc`, `feedHash` e a lista de
  ficheiros; a app carrega o manifesto uma vez por sessão e só o ficheiro da
  linha consultada.
- `CarrisLisboaService` (autocarros) e `GtfsTrainService` (comboios) partilham
  o formato, não o contrato: um implementa `IBusService`, o outro
  `ITrainService`.
- Os dados vivem em `wwwroot/data/<operador>/` e a base URL vem da
  configuração (`Apis:Carris:StaticDataBaseUrl`, `Apis:Cp`,
  `Apis:Fertagus`), para poder apontar para CDN em produção.
- O procedimento operacional está em
  [`.github/skills/gtfs-pipeline/SKILL.md`](../.github/skills/gtfs-pipeline/SKILL.md).

## 5. Favoritos

- `FavoriteStore` (scoped) persiste em `localStorage` através de `IJSRuntime`,
  chave `mybusapp:favorites:v1`, JSON com enums como string e validação de
  duplicados/entradas inválidas na leitura.
- `Components/FavoritesDashboard.razor` agrega as próximas partidas de todos os
  favoritos, com refresh periódico
  (`FavoritesDashboard:RefreshIntervalSeconds`, 300s por defeito) e pausa
  quando o documento não está visível.
- A identidade de cada favorito depende do modo; ver
  [`domain.md`](domain.md#4-favoritos).

## 6. Configuração e segredos

- `Configuration/Configuration.cs` define `ApiSettings`/`ApiEndpoint`; o bind é
  manual em `Program.cs` e o resultado é singleton.
- `wwwroot/appsettings.json` é a configuração de execução e é publicada tal e
  qual. A chave Transitland incluída é de desenvolvimento; não há segredos
  reais nesta arquitetura.
- `Utils/AppLogger` centraliza o log de consola e mascara `apikey=` nos URLs.

## 7. Registo de DI

| Registo | Implementação | Lifetime |
|---|---|---|
| `ApiSettings` | instância ligada da configuração | singleton |
| `AppLogger` | `AppLogger` | singleton |
| `TimeProvider` | `TimeProvider.System` | singleton |
| `IBusService` | `CarrisMetropolitanaService` | scoped |
| `IBusService` | `CarrisLisboaService` (fábrica) | scoped |
| `IBusService` | `TransitlandService` | scoped |
| `ITrainService` | `CpService` (fábrica) | scoped |
| `ITrainService` | `FertagusService` (fábrica) | scoped |
| `FavoriteStore` | `FavoriteStore` | scoped |
| `BusServiceRouter` | `BusServiceRouter` | scoped |
| `HttpClient` | base address do host | scoped |

As fábricas de `CarrisLisboaService`, `CpService` e `FertagusService` criam um
`HttpClient` próprio porque estes serviços mudam a `BaseAddress` para a dos
dados estáticos. É a exceção deliberada à regra de injetar `HttpClient`; não a
generalizes sem motivo.

## 8. Testes

`MyBusApp.Tests` usa xUnit (`Microsoft.NET.Test.Sdk`, `coverlet.collector`) e
referencia a app e o `GtfsDataGenerator`. Cobre serviços, router, dados
estáticos, favoritos e páginas. As páginas são testadas com um renderer de
teste próprio (`Renderer`/`RenderTree`), **sem bUnit** — não introduzas bUnit
sem uma necessidade que este mecanismo não resolva.

```bash
dotnet test MyBusApp.Tests/MyBusApp.Tests.csproj
```

## 9. Onde mexer

| Quero… | Ficheiro |
|---|---|
| acrescentar um operador | skill `add-operator` |
| gerar/atualizar dados GTFS | skill `gtfs-pipeline` |
| mudar o contrato de autocarros | `Services/IBusService.cs` |
| mudar o contrato de comboios | `Services/ITrainService.cs` |
| mudar prioridade de fallback | `Services/BusServiceRouter.cs` |
| mudar persistência de favoritos | `Services/FavoriteStore.cs` |
| acrescentar configuração | `Configuration/Configuration.cs` + `wwwroot/appsettings.json` |
| mudar a página inicial | `Pages/Home.razor` + `Components/FavoritesDashboard.razor` |

## 10. Lacunas conhecidas

- `Pages/Metro.razor` e `Pages/Boats.razor` são estados de indisponibilidade,
  sem providers nem horários simulados.
- Sem realtime ferroviário. Carris Lisboa e Transitland também não fornecem
  previsões em tempo real; só a Carris Metropolitana o faz.
- Sem backend: não há agregação, cache partilhada nem proteção de chaves.
- Sem sincronização de favoritos entre dispositivos.
