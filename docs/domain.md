# Domínio

> Verificado: 2026-11-10 @ `5549968`
>
> Semântica dos conceitos que o código modela: o que é um operador, o que
> distingue tempo real de agendado e o que identifica um favorito. O estado
> concreto de cada operador e a política normativa de horários estão no
> [`roadmap.md`](roadmap.md); as decisões, em [`decisions/`](decisions/).

## 1. Vocabulário

| Termo | Significado neste projeto |
|---|---|
| **Modo** | Autocarro, comboio, metro, barco. Determina o contrato (`IBusService` ou `ITrainService`) e a experiência da página. |
| **Operador** | Empresa que presta o serviço (Carris Metropolitana, Carris Lisboa, CP, Fertagus). |
| **Fonte** | Origem técnica dos dados: API, feed GTFS, agregador. Pode não ser um operador. |
| **Provider** | Identificador técnico interno usado para roteamento e para a identidade dos favoritos. Não é, por si, um nome para mostrar ao utilizador. |

Transitland é uma **fonte/agregador** no contexto dos autocarros, não um
operador nem um modo. Internamente continua a ser um provider
(`BusProvider.Transitland`); na interface não deve ser apresentado como empresa
transportadora. Quando o operador real não está disponível e validado, não se
inventa um nome.

## 2. Contratos por modo

- `IBusService` — autocarros: resolver linha, sentidos, paragens e chegadas.
- `ITrainService` — comboios: estações, destinos e partidas.
- Não existe contrato universal de transportes, nem router multimodal. Um modo
  novo acrescenta o seu contrato ou reutiliza um existente; não cria uma camada
  que una tudo.

O porquê está em [`decisions/0001-arquitetura-por-modo.md`](decisions/0001-arquitetura-por-modo.md).

## 3. Tempo real e agendado

A distinção é uma promessa ao utilizador e não pode ser aproximada.

| Modo | Como se distingue | Efeito na UI |
|---|---|---|
| Autocarro | `BusArrival.IsRealTime` | "Tempo Real" (verde) ou "Agendado" |
| Comboio | `TrainDeparture` não tem flag | Sempre "Agendado" |

- Na Carris Metropolitana, `IsRealTime` vem da previsão da API.
- Carris Lisboa, CP, Fertagus e Transitland servem horários planeados; as
  chegadas são devolvidas como agendadas.
- Um horário programado **nunca** pode ser rotulado como tempo real, e uma
  previsão expirada nunca pode ser apresentada como atual.
- Não há previsão: mostra-se o horário programado quando existe. Não há fonte
  programada: não se inventa nada.
- As três falhas de fallback são problemas diferentes e não partilham
  abstração: previsões (realtime → programado), entre providers (outra fonte
  resolve a pesquisa) e de distribuição (manter a última versão estática
  válida). A precedência normativa e os critérios de aceitação estão na
  secção 5 do [`roadmap.md`](roadmap.md#5-política-de-horários-e-fallback).

Ao ler GTFS, respeitar calendário, exceções, data, fuso `Europe/Lisbon` e
horas superiores a 24h. Ao combinar fontes, não duplicar partidas.

## 4. Favoritos

Um favorito é uma **consulta guardada**, não um nome. A identidade usa apenas
identificadores, porque os nomes mudam e não são únicos.

| Modo | Identidade | O que representa |
|---|---|---|
| Autocarro | `Provider` + `StopId` + `LineNumber` (número público) + `DirectionId` | Uma linha numa paragem, num sentido |
| Comboio | `Provider` + `StopId` (estação) + `DestinationId` | Uma estação, com partidas agregadas por destino |

- `FavoriteProvider` tem cinco valores (os três providers de autocarro mais CP
  e Fertagus); `BusProvider` tem três. São enums diferentes de propósito: um
  identifica o contrato, o outro identifica a origem guardada.
- O número público da linha é o que o utilizador escreve; o `Id` interno do
  provider não serve para isso.
- Persistência em `localStorage`, sem sincronização entre dispositivos. Ver
  [`decisions/0004-favoritos-em-localstorage.md`](decisions/0004-favoritos-em-localstorage.md).
- Nomes apresentados (`StopName`, destinos) são para leitura; nunca entram na
  comparação de identidade.

## 5. Ao acrescentar um operador ou modo

- Autocarro com API ou GTFS → implementa `IBusService` e entra na prioridade do
  `BusServiceRouter`.
- Comboio com GTFS → herda `GtfsTrainService` e regista-se como
  `ITrainService`.
- Um modo sem fonte real (metro, barco) fica como indisponível; não se cria
  serviço, horário nem previsão fictícia.
- A decisão de integrar exige uma ficha por operador (oficialidade, licença,
  cobertura, formato, realtime). O modelo está na secção 8 do
  [`roadmap.md`](roadmap.md#8-ficha-obrigatória-de-decisão-por-operador).

O procedimento completo está em
[`.github/skills/add-operator/SKILL.md`](../.github/skills/add-operator/SKILL.md).
