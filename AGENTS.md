# MyBusApp

Aplicação Blazor WebAssembly (.NET 9) para consultar as próximas partidas de
autocarros e comboios em Portugal. Favoritos no `localStorage`, pesquisa manual
por modo, sem backend: tudo corre no browser.

Este é o ponto de entrada para qualquer agente. Se só puderes ler um ficheiro,
é este.

## Comandos

```bash
dotnet build MyBusApp.sln
dotnet test MyBusApp.Tests/MyBusApp.Tests.csproj
dotnet run
```

Gerar dados GTFS (offline, nunca no browser):

```bash
dotnet run --project Tools/GtfsDataGenerator/GtfsDataGenerator.csproj -- \
  --gtfs-url <url> --output wwwroot/data/<operador>
```

## Regras

- **Código e configuração são a verdade.** Documentação divergente corrige-se
  no mesmo commit.
- **Sem dependências novas** em código de produção (só SDK Blazor + Bootstrap +
  `System.Text.Json`). Testes usam as que já estão em `MyBusApp.Tests`.
- **Alterações cirúrgicas:** nada de refactor de código adjacente.
- **GTFS nunca no browser:** `Tools/GtfsDataGenerator` gera JSON offline.
- **Tempo real ≠ agendado.** Nunca rotular um como o outro, nunca inventar
  operadores, horários ou previsões.
- **Um contrato por modo:** `IBusService` (autocarros), `ITrainService`
  (comboios). Sem router ou interface universal de transportes.
- **Sem `stage`, `commit`, `reset`, `checkout` ou `revert`** sem autorização
  explícita.
- Português (pt-PT) em documentação e texto de UI; identificadores de código em
  inglês.

## Onde ler o quê

| Precisas de… | Lê |
|---|---|
| como o código está montado, DI, fluxos, dados estáticos | [`docs/architecture.md`](docs/architecture.md) |
| o que é um operador, realtime vs agendado, identidade de favoritos | [`docs/domain.md`](docs/domain.md) |
| como escrever C#/Blazor/UI/testes neste projeto | [`docs/conventions.md`](docs/conventions.md) |
| o que está feito, o que vem a seguir, prioridades | [`docs/roadmap.md`](docs/roadmap.md) |
| porque é que uma decisão foi tomada | [`docs/decisions/`](docs/decisions/) |
| integrar um operador novo | [`.github/skills/add-operator/SKILL.md`](.github/skills/add-operator/SKILL.md) |
| gerar ou atualizar dados GTFS | [`.github/skills/gtfs-pipeline/SKILL.md`](.github/skills/gtfs-pipeline/SKILL.md) |
| planos antigos (não seguir) | [`docs/archive/`](docs/archive/README.md) |

Carrega só o que a tarefa pede. Não leias o roadmap inteiro para corrigir um
`<div>`.

## Manter a memória verdadeira

- **Um facto, um lugar.** Se a informação aparece em dois sítios, o segundo é
  um link.
- Os documentos de conhecimento (`architecture`, `domain`, `conventions`)
  começam com `Verificado: <data> @ <commit>`. Se o commit já não for ancestral
  do atual, desconfia do conteúdo.
- Uma decisão estrutural nova ganha um ADR em `docs/decisions/` (numeração
  seguinte, append-only; não se reescreve a história).
- Um plano novo substitui o anterior em `docs/roadmap.md`; o antigo vai para
  `docs/archive/` com uma linha no índice de lá.
- Se um documento não pode ser mantido verdadeiro, apaga-o. Documentação
  genérica que serve para qualquer repositório não pertence a este.
