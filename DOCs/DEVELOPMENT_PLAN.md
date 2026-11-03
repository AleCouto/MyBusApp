# MyBusApp — Plano de Desenvolvimento

Este documento descreve os milestones do MyBusApp e o estado conhecido do
repositório. Deve ser atualizado quando um milestone for concluído ou quando a
ordem do plano mudar.

**Estado:** `[X]` concluído; `[ ]` por realizar ou pendente.

## Princípios

- Usar .NET, Blazor WebAssembly e Bootstrap nativos.
- Manter alterações pequenas e focadas no milestone atual.
- Não introduzir dependências externas, factories, repositories ou camadas
  adicionais sem uma necessidade demonstrada.
- Preservar o formato dos dados estáticos `GtfsStatic*` enquanto existirem
  consumidores compatíveis.

## Arquitetura alvo

```text
MyBusApp
  |
  +-- BusServiceRouter
        |
        +-- Carris Metropolitana (API e realtime quando disponível)
        +-- Carris Lisboa (GTFS estático)
        +-- CP (GTFS estático)
        +-- Transitland (fallback)
```

## Milestones concluídos

### [X] M1 — Modelos GTFS comuns

- Criados os modelos `GtfsStatic*` em `Models/StaticData/GtfsStaticData.cs`.
- `GtfsStaticStopTime` mantém arrival e departure em segundos desde o início
  do service day.

### [X] M2 — Generator Carris Lisboa

- O generator passou a produzir dados com os modelos `GtfsStatic*`.
- `arrival_time` e `departure_time` são preservados, incluindo horários acima
  de 24 horas.

### [X] M3 — CarrisLisboaService

- O serviço Carris Lisboa passou a consumir o schema GTFS estático versão 2.
- Mantém calendário, exceções, schedules e fallback departure/arrival.

### [X] M4 — Generator GTFS genérico

- O generator foi generalizado para `Tools/GtfsDataGenerator`.
- Aceita `--gtfs <ficheiro.zip>` ou `--gtfs-url <url>`.
- Agrupa por `route_short_name`, falha explicitamente se esse campo estiver
  vazio e deteta colisões de nomes de ficheiro após sanitização.

### [X] M5 — Router e provider explícito

- O router mantém a pesquisa normal por prioridade.
- Foi adicionada a resolução explícita por `BusProvider` e número de linha.
- Exceções de providers são registadas e não impedem o fallback normal.

## Milestones CP concluídos

### [X] M6.1 — Auditoria CP

- Auditoria CP concluída.

### [X] M6.2 — Auditoria de modelação CP

- Auditoria de modelação CP concluída.

### [X] M6.3 — CpService

- `CpService` implementa `IBusService` com os modelos `GtfsStatic*`.
- Usa `data/cp/` por defeito e suporta `Apis:Cp:StaticDataBaseUrl`.
- Expõe categorias/linhas CP, direções por `route_id`, paragens por sequência
  representativa e horários estáticos.
- Realtime CP e o número individual do comboio não fazem parte deste milestone.

### [X] M6.4 — Dados estáticos reais da CP

- Dados reais CP gerados e presentes em `wwwroot/data/cp/`, com `manifest.json`
  versão 2 e 14 linhas.
- A auditoria M6.4 foi `PASS WITH WARNINGS`: confirmou integridade referencial,
  dados reais, preservação de horários GTFS superiores a 24 horas, build sem
  warnings/erros e 30/30 testes aprovados.
- A verificação pontual do feed oficial consultou
  `https://publico.cp.pt/gtfs/gtfs.zip`: `200 OK`, `application/zip`,
  299 850 bytes, com última modificação em 2026-09-29.
- Entity Framework / EF Core não está presente; a integração continua baseada
  em serviços, JSON e `System.Text.Json`.
- Os warnings técnicos identificados na auditoria foram tratados e encerrados
  em M6.5.

## M6.5 concluído

### [X] M6.5 — Fecho técnico da integração CP

**STATUS: CONCLUÍDO**

**Objetivo**

- Fechar os warnings técnicos da integração CP com alterações pequenas de
  robustez, testes e versionamento, antes de trabalho de UI, realtime ou novos
  operadores.

**Resultado**

- M6.5.1 — PASS; validação explícita de arrival/departure rejeita campos
  ausentes, não numéricos ou fora do intervalo `Int32`, mantendo zero válido.
- M6.5.2 — PASS; teste offline carrega pelo manifest e valida o JSON real da
  Linha de Sintra.
- M6.5.3 — PASS; teste comprova empate entre sequências, desempate ordinal e
  resultado determinístico.
- M6.5.4 — PASS; whitespace do bloco CP revisto e decisão de versionamento
  registada abaixo.
- M6.5.5 — PASS; auditoria final confirmou 37/37 testes, build sem erros ou
  warnings e critérios técnicos concluídos.
- Os dados CP continuam no schema v2, com manifest e 14 linhas; horários iguais
  ou superiores a 24 horas permanecem preservados.
- Sem Entity Framework / EF Core, packages ou abstrações arquiteturais novos.

#### [X] M6.5.1 — Validação explícita de stop times (PASS)

**Objetivo**

- Impedir que o CP aceite silenciosamente JSON v2 com campos obrigatórios de
  stop time ausentes ou malformados.

**Contexto**

- Atualmente o `CpService` verifica a versão, mas a ausência de propriedades
  numéricas pode resultar nos valores por defeito do modelo. A Carris Lisboa já
  inspeciona o schema JSON antes da desserialização. Não criar validação genérica
  nem alterar `GtfsStatic*` sem necessidade demonstrada.

**Ficheiros envolvidos**

- `Services/CpService.cs`
- `MyBusApp.Tests/CpServiceTests.cs`

**Alterações esperadas**

- Validar, antes da desserialização/aceitação da linha, a presença e o tipo
  numérico de `arrivalTimeSeconds` e `departureTimeSeconds` em cada stop time.
- Rejeitar dados inválidos sem os guardar na cache; manter schema v2, serviços,
  contratos públicos e dados válidos inalterados.

**Critérios de validação**

- Dados com qualquer campo ausente ou não numérico são recusados; dados válidos
  continuam a ser aceites.
- Não há abstrações novas nem alterações ao modelo `GtfsStatic*`.

**Testes / Build**

- Testes unitários em `CpServiceTests`, incluindo os dois campos inválidos.
- `dotnet test MyBusApp.Tests/MyBusApp.Tests.csproj --filter FullyQualifiedName~CpServiceTests`

#### [X] M6.5.2 — Teste com JSON CP real (PASS)

**Objetivo**

- Cobrir a estrutura realmente versionada, para além dos JSON sintéticos dos
  testes unitários.

**Contexto**

- Atualmente não há teste que carregue dados CP versionados. O teste deve ser
  local e determinístico, sem download nem novo framework de fixtures.

**Ficheiros envolvidos**

- `MyBusApp.Tests/CpServiceTests.cs`
- `wwwroot/data/cp/manifest.json` (entrada de teste, apenas leitura)
- `wwwroot/data/cp/lines/Linha de Sintra.28cc01ca4140e507.json` (entrada de teste,
  apenas leitura)

**Alterações esperadas**

- Ler o manifest e resolver nele a linha `Linha de Sintra`; carregar o JSON
  indicado pela entrada, sem rede, usando exclusivamente os ficheiros do
  repositório.
- Validar versão e estrutura esperadas da linha, incluindo que cada stop time
  tem `arrivalTimeSeconds` e `departureTimeSeconds` numéricos.
- Usar `Linha de Sintra` por constar do manifest real e oferecer uma linha
  completa com várias direções e paragens; não modificar nem regenerar os dados.

**Critérios de validação**

- O teste falha perante manifest/ficheiro ausente, referência inválida, schema
  inesperado ou campo de stop time inválido; passa offline com os dados atuais.
- Não são introduzidas fixtures ou dependências novas.

**Testes / Build**

- Executar o teste de dados reais e o conjunto focado `CpServiceTests`.

#### [X] M6.5.3 — Teste de empate entre sequências (PASS)

**Objetivo**

- Fixar por teste o resultado determinístico quando duas sequências têm a mesma
  frequência.

**Contexto**

- `CpService` escolhe a sequência empatada pela chave de stop IDs ordenada com
  comparação ordinal. Testar o critério existente sem modificar o algoritmo.

**Ficheiros envolvidos**

- `MyBusApp.Tests/CpServiceTests.cs`

**Alterações esperadas**

- Criar no teste dados sintéticos com pelo menos duas sequências distintas e
  igualmente frequentes para a mesma direção.
- Confirmar que a sequência escolhida corresponde à menor chave ordinal atual
  e que chamadas repetidas produzem o mesmo resultado.

**Critérios de validação**

- O teste prova um empate verdadeiro, a sequência esperada e a determinância,
  sem alterar a implementação de produção para facilitar o teste.

**Testes / Build**

- Executar o teste de empate e o conjunto focado `CpServiceTests`.

#### [X] M6.5.4 — Whitespace e revisão de versionamento (PASS)

**Objetivo**

- Limpar exclusivamente o whitespace final nas linhas CP de `Program.cs` e
  confirmar que só os artefactos necessários seguem para versionamento.

**Contexto**

- A revisão deve preservar alterações existentes no working tree e não pode
  reorganizar o DI, formatar o ficheiro globalmente ou alterar `.gitignore`
  automaticamente.

**Ficheiros envolvidos**

- `Program.cs` (alteração apenas nas linhas CP com whitespace final)
- `Services/CpService.cs`, `MyBusApp.Tests/CpServiceTests.cs`,
  `Models/Domain/BusModels.cs`, `Program.cs` e `wwwroot/data/cp/` (revisão do
  conjunto que deve ser versionado; sem alterações artificiais)
- `.gitignore` (revisão apenas)
- `tmp/gtfs.zip`, `.DS_Store`, `bin/`, `obj/`, `TestResults/`, relatórios e ZIPs
  temporários (verificação de exclusão; não versionar)

**Alterações esperadas**

- Remover trailing whitespace somente das linhas de registo CP em `Program.cs`.
- Versionar `Services/CpService.cs`, `MyBusApp.Tests/CpServiceTests.cs`,
  `Models/Domain/BusModels.cs`, `Program.cs` e `wwwroot/data/cp/`; não criar
  alterações artificiais em ficheiros sem diferenças necessárias.
- Não versionar `bin/`, `obj/`, `TestResults/`, `tmp/`, `gtfs.zip` ou
  `.DS_Store`.
- Decisão documental fechada: a verificação pontual foi resumida em M6.4; o
  ficheiro `audit_summary.md` foi removido por ser redundante com as auditorias
  M6.4/M6.5.
- O `.gitignore` atual cobre `bin/`, `obj/` e `.DS_Store`, mas não cobre
  `TestResults/` nem `tmp/gtfs.zip` (`*.gtfs.zip` não corresponde a esse nome).
  A cobertura de relatórios/ZIPs temporários depende dos nomes e padrões
  concretos. Registar estas lacunas para decisão; não editar `.gitignore` neste
  milestone sem aprovação.

**Critérios de validação**

- O diff de `Program.cs` contém somente remoção de whitespace nas linhas CP.
- A revisão confirma os caminhos a versionar e nenhum artefacto regenerável ou
  temporário é incluído; lacunas de ignore ficam explicitamente reportadas.

**Testes / Build**

- Rever `git status --short` e os diffs dos ficheiros alvo; usar
  `git check-ignore -v` nos artefactos para os quais a cobertura é aplicável.
- Não executar generator nem alterar dados CP durante esta subtarefa.

#### [X] M6.5.5 — Build, testes e auditoria final (PASS)

**Objetivo**

- Confirmar o fecho técnico sem alargar o escopo da integração CP.

**Contexto**

- A auditoria final cobre validação, dados reais locais, comportamentos
  temporais/variantes e higiene do conjunto versionado.

**Ficheiros envolvidos**

- `Services/CpService.cs`, `MyBusApp.Tests/CpServiceTests.cs`,
  `Program.cs` e `wwwroot/data/cp/` (verificação, sem novas alterações fora dos
  critérios anteriores)
- `DEVELOPMENT_PLAN.md` (registo do estado final)

**Alterações esperadas**

- Nenhuma alteração funcional adicional; registar M6.5 concluído apenas após
  todos os critérios e gates passarem.

**Critérios de validação**

- Código: validação correta de stop times, sem abstrações/packages novos,
  Entity Framework/EF Core ou alterações fora do escopo.
- Dados: os dados CP reais e o manifest permanecem válidos e não são
  regenerados; nenhum artefacto temporário é versionado.
- Testes cobrem dados CP reais, stop times, horários superiores a 24 horas,
  variantes de stops e empate determinístico.
- Build sem erros nem warnings e todos os testes aprovados.
- Resultado final: 37/37 testes aprovados, 0 erros e 0 warnings.
- A recursão ocorreu porque `Tools/**` continuava como `Content` do app web,
  enquanto o generator referencia o projeto principal. Foi corrigida em
  `MyBusApp.csproj` com `<Content Remove="Tools/**" />`. Os outputs recursivos
  antigos foram removidos; builds posteriores mantêm três diretórios `bin` e
  três `obj`, sem `bin/bin` nem cópias recursivas do generator. O workspace
  estabilizou em aproximadamente 1,4 GB após build/teste.

**Testes / Build**

- Na raiz da solução: `dotnet build` e `dotnet test`.
- Esperado: 0 errors, 0 warnings e 100% dos testes aprovados.

**Fora do escopo de M6.5**

- Alterar `IBusService`, `BusServiceRouter`, UI ou seleção de provider.
- Implementar número individual do comboio, realtime CP ou novos operadores.
- Alterar generators ou serviços Carris/Transitland; criar factories,
  repositories, interfaces, abstrações ou packages.
- Introduzir Entity Framework/EF Core, `DbContext`, `DbSet`, migrations ou ORM.
- Regenerar dados CP sem necessidade, ou alterar `.gitignore` sem decisão.

## Milestones multimodais e futuros

### [ ] M7 — Arquitetura multimodal

**Objetivo**

- Evoluir MyBusApp de uma experiência centrada em autocarros para consulta de
  transportes públicos multimodais, dando contexto de modo antes da pesquisa.
- Manter contratos e modelos específicos por modo; não obrigar comboios, metro
  ou barcos a serem representados como autocarros.

**Decisões arquiteturais**

- Modos de navegação: `Autocarros`, `Metro`, `Comboios` e `Barcos`. A navegação
  representa o modo de transporte, não o provider técnico.
- Serviços de operador implementam o contrato do modo: Carris Metropolitana,
  Carris Lisboa e Transitland em `IBusService`; `CpService` em `ITrainService`.
- No modo Comboios, os providers selecionáveis são operadores ferroviários:
  CP primeiro e Fertagus futuramente, apenas quando existir fonte real.
  Transitland permanece uma fonte/agregador interna do modo Autocarros e não é
  um operador ferroviário selecionável.
- A experiência Comboios é contextual e orientada a estação: operador → estação
  → destino/sentido → próximas partidas. A pesquisa não usa um textbox universal
  para consultar indiscriminadamente linhas de todos os modos.
- `IBusService` permanece contrato de autocarros. Não criar classe base de
  transportes nem um `ITransportService` universal.
- Durante M7.1 e M7.2, `CpService` continua temporariamente em `IBusService` e
  `BusProvider.Cp` permanece por compatibilidade técnica com a implementação
  atual. Isso não classifica CP semanticamente como autocarro.
- A migração de `CpService` para `ITrainService` e a remoção de `BusProvider.Cp`
  ficam reservadas a M7.3. Não criar `TrainProvider.Cp` durante M7.1 ou M7.2;
  definir a identificação ferroviária da CP no contexto concreto dessa migração.
- Metro e Barcos ficam representados como modos, mas não ganham serviços,
  providers ou datasets fictícios enquanto não existir uma fonte real.
- Manter `BusLine`, `BusDirection`, `BusStop` e `BusArrival` específicos de
  autocarros. Introduzir tipos ferroviários próprios para serviço/linha,
  direção, estação e partida/horário. Não generalizar estes modelos para
  `TransportLine`, `TransportStop` ou `TransportArrival` sem casos reais que
  demonstrem semântica comum.
- Os contratos de metro e barco pertencem aos respetivos modos e devem usar
  conceitos próprios: linha/estação/chegada no metro; terminal/destino/partida
  nos barcos. Definir operações concretas quando existir fonte que as imponha;
  não criar contratos vazios nem hierarquia artificial.
- Transitland permanece agregador/fonte de dados dentro do modo Autocarros; não
  aparece como operadora nem como quarto modo. A presença de `agency` no DTO não
  significa que o adapter atual exponha a operadora; não inventar essa
  identidade na migração.
- A pesquisa manual observada (`4725`, `3710`, `714`, `Linha de Sintra`) é a
  referência de compatibilidade. O 404 de `route_stop_patterns` do Transitland
  permanece um comportamento a tratar separadamente, não um objetivo de M7.

#### [X] M7.1 — Limites de domínio e modelos por modo

**Objetivo**

- Separar a identidade de modo, operador e fonte técnica nos tipos de domínio.

**Contexto**

- `BusProvider` inclui atualmente Metropolitana, Carris Lisboa, Transitland e
  CP. Transitland é agregador e CP é ferroviária; a presença temporária de
  `BusProvider.Cp` é compatibilidade técnica, não uma decisão de domínio. Os
  modelos `Bus*` também não expressam conceitos ferroviários.

**Ficheiros envolvidos**

- `Models/Domain/BusModels.cs`
- novo ficheiro de domínio ferroviário, por exemplo
  `Models/Domain/TrainModels.cs`
- testes de domínio em `MyBusApp.Tests/` já existentes

**Alterações esperadas**

- Manter `BusLine`, `BusDirection`, `BusStop` e `BusArrival` específicos de
  Autocarros.
- Definir apenas modelos ferroviários mínimos para serviço/linha, direção,
  estação e horário/partida, com vocabulário próprio.
- Não migrar `CpService`; manter `IBusService` e `BusServiceRouter` inalterados.
- Manter temporariamente `BusProvider.Cp` porque `CpService` ainda implementa
  `IBusService`. Não criar `TrainProvider.Cp` nesta fase; a remoção de
  `BusProvider.Cp` será feita em M7.3, quando já não houver consumidores.
- Não criar modelos de Metro/Barcos antes de haver um provider concreto e os
  respetivos requisitos de dados.

**Validação**

- Tipos e enumerações não classificam CP como autocarro nem Transitland como
  operador semanticamente; `BusProvider.Cp` é identificado no código/plano como
  compatibilidade transitória até M7.3.
- Os modelos de autocarro continuam compatíveis com os três adapters de bus.

**Testes**

- Testes de domínio para associar cada fonte ao modo correspondente.
- Compilação dos adapters e testes existentes como regressão.

**Fora do escopo**

- Renomear todos os modelos para nomes multimodais genéricos.
- Migrar `CpService`, alterar `IBusService`/`BusServiceRouter` ou remover
  `BusProvider.Cp`.
- Criar `TrainProvider.Cp` antes da migração M7.3.
- Número individual do comboio (`trip_short_name`).

#### [X] M7.2 — Contratos específicos por modo

**Objetivo**

- Estabelecer interfaces cujo vocabulário corresponde às operações reais de
  cada modo.

**Contexto**

- `IBusService` representa linha, direção, paragem e chegada; esse contrato não
  deve ser imposto a estação ferroviária ou partida de barco. `CpService`
  permanece em `IBusService` até M7.3, mesmo depois de definido o contrato
  ferroviário.

**Ficheiros envolvidos**

- `Services/IBusService.cs`
- novos contratos `Services/ITrainService.cs`, `Services/IMetroService.cs` e
  `Services/IFerryService.cs`, conforme os limites decididos acima
- modelos de domínio do modo correspondente

**Alterações esperadas**

- Preservar `IBusService` e as operações bus existentes.
- Definir `ITrainService` com pesquisa de serviço/linha, direções, estações e
  horários/partidas estáticos, sem apenas copiar os tipos `Bus*`.
- Validar se `TrainLine`, `TrainDirection`, `TrainStation` e `TrainDeparture`
  representam adequadamente o fluxo ferroviário estação → destino/sentido →
  partidas. Confirmar em particular se `TrainDeparture.ServiceName` é necessário
  ou se outro dado corresponde melhor à saída real do `CpService`.
- O contrato deverá permitir obter estações disponíveis, destinos/sentidos a
  partir de uma estação e partidas para uma estação e destino/sentido. Não fixar
  assinaturas antes de validar o fluxo e os dados CP existentes.
- A introdução de `ITrainService` define o contrato, mas não migra `CpService`,
  não cria `TrainProvider.Cp` e não remove `BusProvider.Cp`; estes passos
  pertencem a M7.3.
- Registar como alvo os contratos `IMetroService` e `IFerryService`, com nomes
  semânticos de linha/estação/chegada e terminal/destino/partida respetivamente.
  A implementação de serviços concretos continua condicionada à existência de
  providers reais.
- Não criar `BaseTransportService`, interfaces genéricas ou operações que
  nenhum provider atual possa implementar.

**Validação**

- Cada interface é específica do modo e pode ser implementada sem adaptar
  artificialmente um domínio a outro.
- `IBusService` continua a compilar sem alterar os seus callers atuais.

**Testes**

- Contratos exercitados por fakes nos testes dos routers/fluxos por modo.
- Compilação confirma que não há acoplamento de `CpService` a `IBusService`.
- Testar a semântica do fluxo estação → destino/sentido → partidas com os dados
  que `CpService` já expõe, sem definir testes de UI neste milestone.

**Fora do escopo**

- Classes base de serviços, factories ou repositories.
- Implementações Metro/Barcos sem provider/dataset existente.

#### [X] M7.3 — Migração da CP para Comboios

**STATUS: CONCLUÍDO**

**Objetivo**

- Integrar `CpService` diretamente no modo ferroviário sem alterar a semântica
  dos dados CP estáticos.

**Contexto**

- A CP já lê manifest v2 e JSON locais com `route_short_name` como serviço/linha,
  `route_id` como percurso e `GtfsStatic*` para horários.

**Ficheiros envolvidos**

- `Services/CpService.cs`
- `Services/ITrainService.cs`
- `Services/IBusService.cs` (somente se a compatibilidade da migração exigir)
- `Models/Domain/BusModels.cs` e modelos ferroviários
- `Services/BusServiceRouter.cs`
- `Program.cs`
- `MyBusApp.Tests/CpServiceTests.cs`

**Alterações esperadas**

- Migrar `CpService` de `IBusService` para `ITrainService`, mapeando
  serviço/linha, direção, estação e horário para tipos ferroviários próprios.
- Adaptar os testes CP ao contrato e provider ferroviários; remover as
  dependências CP em `IBusService` e `BusProvider`.
- Definir a identificação ferroviária necessária para distinguir operadores no
  modo Comboios; só então remover `BusProvider.Cp`, após confirmar que não
  existem consumidores.
- Remover CP do `BusServiceRouter` e registá-la como serviço ferroviário pelo
  mecanismo de DI apropriado.
- Manter schema v2, dados locais, horários superiores a 24 horas e
  `IsRealTime = false`.
- Manter os testes reais, de stop times, variantes e empate adaptados à
  semântica ferroviária; não regenerar os datasets.

**Validação**

- `Linha de Sintra`, `Linha de Cascais`, `AP`, `IC` e outras categorias
  continuam resolvidas no contexto Comboios.
- CP deixa de surgir como candidato de `BusServiceRouter` e não restam
  referências a `BusProvider.Cp` antes de remover o enum.
- Os fluxos de autocarro mantêm a prioridade e os resultados existentes.

**Testes**

- Atualizar `CpServiceTests` para o contrato e tipos ferroviários.
- Testar pesquisa, direções, estações, horários estáticos e horários >24h com
  os dados versionados/offline.

**Fora do escopo**

- Realtime CP, GTFS-RT ou identificação do comboio individual.
- Alterar generator, schema ou dados estáticos.

#### [X] M7.4 — DI e resolução por modo

**STATUS: CONCLUÍDO**

**Objetivo**

- Compor serviços por modo sem misturar providers de transportes diferentes.

**Contexto**

- `BusServiceRouter` ordena atualmente serviços de `IBusService`; não deve
  receber CP depois da migração. CP é o primeiro `ITrainService`; o modo poderá
  ter múltiplas implementações ferroviárias quando providers reais adicionais,
  como Fertagus, forem integrados.

**Ficheiros envolvidos**

- `Program.cs`
- `Services/BusServiceRouter.cs`
- novos testes de composição/routing em `MyBusApp.Tests/`

**Alterações esperadas**

- Manter `BusServiceRouter` como router de `IBusService`, preservando prioridade
  implícita e resolução explícita entre fontes de autocarros.
- Registar CP como `ITrainService` e consumi-lo no modo Comboios.
- Compor as implementações ferroviárias disponíveis para apresentar os
  operadores reais; escolher a estratégia específica em M7.4, após conhecer o
  contrato e os providers. Usar a composição DI mais simples que suporte CP e
  futuros `ITrainService`.
- Não decidir antecipadamente que é necessário um router ferroviário. Não criar
  `TrainServiceRouter`, `TransportServiceRouter`, factories ou classes base sem
  necessidade demonstrada.
- Não registar services inexistentes para Metro ou Barcos.

**Validação**

- A seleção de modo limita a procura às interfaces desse modo.
- O router bus mantém os resultados e a prioridade de Metropolitana, Carris
  Lisboa e Transitland; a CP não participa nessa ordenação.

**Testes**

- Testes para DI por modo, prioridade bus e isolamento CP no modo Comboios.
- Testes confirmam que falha/ausência numa fonte de autocarro não inicia
  pesquisa ferroviária.

**Fora do escopo**

- Router multimodal genérico ou alteração de prioridade para mascarar
  incompatibilidades de domínio.
- Realtime ou novos providers.

#### [X] M7.5 — Navegação e pesquisa contextual por modo

**STATUS: CONCLUÍDO**

**Objetivo**

- Tornar o modo selecionado o contexto explícito da pesquisa.

**Contexto**

- `Pages/Home.razor` é atualmente a única experiência de pesquisa e pesquisa
  globalmente por texto, o que permite ao Transitland resolver `Linha de Sintra`
  antes de CP.

**Ficheiros envolvidos**

- `Layout/NavMenu.razor`
- `Pages/Home.razor` e páginas/componentes de modo somente se forem necessários
  para manter fluxos separados
- estilos scoped associados, se existirem alterações visuais necessárias

**Alterações esperadas**

- A navegação principal apresenta Autocarros, Metro, Comboios e Barcos.
- Autocarros mantém a experiência `IBusService`.
- Comboios segue o fluxo operador → estação → destino/sentido → próximas
  partidas. O utilizador escolhe primeiro CP; Fertagus só aparece depois de
  existir provider/fonte real. A estação e os destinos disponíveis dependem do
  operador escolhido.
- A pesquisa ferroviária é contextual ao modo; `Linha de Sintra` em Comboios
  consulta CP e não `TransitlandService`.
- A entrada em Metro e Barcos é preparada sem inventar resultados: sem provider
  implementado, informar indisponibilidade e não simular dados.
- Dentro de Autocarros, manter a resolução implícita atual quando só há um
  resultado; permitir escolha entre fontes técnicas se a ambiguidade continuar
  a existir. Transitland é identificado como agregador, não como operadora.

**Validação**

- `714` em Autocarros continua a encontrar Carris Lisboa; `4725` e `3710`
  continuam a encontrar Metropolitana.
- Comboios: selecionar CP, depois uma estação, um destino/sentido e visualizar
  as próximas partidas dessa combinação.
- `Linha de Sintra` em Comboios chega a CP sem ser capturada por Transitland.
- O modo vazio Metro/Barcos não chama serviços inexistentes.

**Testes**

- Testes automatizados de roteamento contextual onde possível; validar interação
  visual no gate manual M7.7, sem adicionar framework de componentes.

**Fora do escopo**

- Realtime ou alteração de adapters para corrigir o 404 Transitland.
- State Container/persistência partilhada sem necessidade demonstrada.

#### [X] M7.6 — Regressão automatizada multimodal

**STATUS: CONCLUÍDO**

**Objetivo**

- Garantir compatibilidade dos providers existentes após a separação de modos.

**Contexto**

- A suite atual usa xUnit e fakes; os dados CP reais já têm cobertura offline.

**Ficheiros envolvidos**

- `MyBusApp.Tests/BusServiceRouterTests.cs`
- `MyBusApp.Tests/CpServiceTests.cs`
- `MyBusApp.Tests/TransitlandServiceTests.cs` e
  `MyBusApp.Tests/TransitlandServiceUnitTests.cs`
- testes de domínio/DI por modo necessários

**Alterações esperadas**

- Preservar testes de resolução implícita/explicita bus e acrescentar isolamento
  entre Bus e Train.
- Testar CP através de `ITrainService`, composição/seleção do operador
  ferroviário, estações disponíveis, destinos/sentidos por estação e partidas.
- Confirmar que Transitland permanece no contexto Bus e não é chamado numa
  pesquisa Comboios.
- Manter testes de schemas/dados reais, variantes, stop times e empate CP.

**Validação**

- Nenhum fluxo bus existente regrede; CP funciona por contrato ferroviário.
- Nenhum modo sem implementação produz resultados simulados.

**Testes**

- Testes focados dos contratos/routers e de cada adapter migrado.
- `dotnet test MyBusApp.Tests/MyBusApp.Tests.csproj` e `dotnet build`.

**Fora do escopo**

- Framework/packages novos, realtime e novos datasets.

#### [ ] M7.7 — Gate funcional manual

**Objetivo**

- Validar no browser o contexto de modo e a continuidade dos percursos de
  consulta.

**Contexto**

- Testes unitários não confirmam navegação nem a sequência real linha/serviço,
  direção, paragem/estação e horário.

**Ficheiros envolvidos**

- `Layout/NavMenu.razor` e páginas/componentes de modo sob teste
- `DEVELOPMENT_PLAN.md` para registar o resultado após aprovação

**Alterações esperadas**

- Executar e registar os cenários manuais abaixo; só concluir M7 depois de todos
  passarem.

**Validação**

- Autocarros: `4725`, `3710` e `714`, verificando linha, direção, paragem e
  chegada.
- Comboios: selecionar Comboios, selecionar CP, selecionar estação, escolher
  destino/sentido e visualizar próximas partidas. Incluir `Linha de Sintra` e
  confirmar que Transitland não participa nessa pesquisa.
- Metro e Barcos: verificar estado indisponível até existir provider real.
- Confirmar que o modo Comboios não consulta Transitland para `Linha de Sintra`.
- Verificar troca de modo, retorno à pesquisa, resultados vazios e pesquisa com
  nome ambíguo/localidade contextualizada.

**Testes**

- Suite M7.6, builds da aplicação/generator e os cenários de browser acima.

**Fora do escopo**

- Iniciar realtime, implementar providers Metro/Barcos ou corrigir endpoints
  Transitland.

**Fora do escopo de M7**

- Realtime, GTFS-RT, posições, atrasos e número individual do comboio.
- Novos operadores, datasets, generator ou backend.
- EF/EF Core/ORM, packages externos, factories, repositories ou grandes
  refatorações.
- Correções não relacionadas do Transitland e alterações arquiteturais sem
  necessidade demonstrada.

### [X] M8 — Auditoria funcional e desenho da experiência de favoritos

**Estado:** auditoria concluída antes da implementação.

**Arquitetura e reutilização**

- `Pages/Home.razor` mantém as pesquisas manuais de autocarros e comboios.
- `BusServiceRouter` resolve autocarros por provider; `IBusService` carrega
  chegadas através de `stopId` e `lineId`.
- Carris Metropolitana usa IDs de API para a consulta e a linha pública para
  apresentação. Carris Lisboa carrega o JSON estático da linha e filtra pelo
  ID GTFS da paragem. Transitland consulta schedules por paragem e rota.
- `ITrainService` separa a CP dos contratos de autocarros. `CpService` carrega
  estações por ID e disponibiliza destinos e partidas agendadas.
- Não existia persistência de favoritos nem uso de `localStorage` no código
  antes deste milestone.

**Decisões**

- Um favorito de autocarro representa uma consulta contextual: provider,
  número público da linha e ID/nome da paragem. Esta identidade preserva os
  parâmetros necessários à interface `IBusService` sem alterar os serviços.
- Um favorito CP representa provider e ID/nome da estação. O dashboard carrega
  os destinos da estação e agrega as partidas disponíveis.
- A ordem da lista é a ordem guardada pelo utilizador; mover para cima/baixo
  é suficiente e não requer dependências ou drag-and-drop.
- Persistência em `localStorage` versionado via `IJSRuntime`: adequada à
  aplicação Blazor WASM, não requer backend, autenticação ou package e limita
  os dados ao browser. Não sincroniza favoritos entre dispositivos.
- Metro e Barcos continuam indisponíveis e não aceitam favoritos sem providers
  reais.

**Experiência, limitações e testes**

- A página inicial mostra os próximos horários de cada favorito, provider/modo,
  destino, horário e estado realtime/agendado quando disponível; inclui
  carregamento, vazio, indisponibilidade, erro, atualização, remoção e ordem.
- Uma falha inesperada de um favorito é isolada e registada sem impedir os
  restantes. Os contratos bus atuais podem devolver lista vazia tanto para
  ausência de partidas como para algumas falhas HTTP; nesses casos a UI indica
  explicitamente que o serviço também pode estar indisponível, sem alterar os
  contratos neste escopo.
- Os IDs de paragens/estações dependem dos datasets dos providers; IDs
  desativados podem deixar de resolver. A linha bus é re-resolvida pelo número
  público para não persistir um ID técnico de linha.
- Testes cobrem serialização, ordem e validação de dados persistidos; testes dos
  providers existentes protegem as pesquisas e horários. A validação browser
  de Carris Metropolitana, Carris Lisboa e CP foi concluída em M10/M11.

### [X] M9 — Modelo e persistência dos favoritos

- Criados modelos para modo, provider, paragem/estação e contexto da linha.
- Favoritos persistidos em `localStorage` sob chave versionada; JSON inválido
  é comunicado, não convertido silenciosamente numa lista vazia.
- Sem backend, autenticação, dependências ou alteração dos contratos de
  providers.

### [X] M10 — Página inicial com próximas partidas dos favoritos

**STATUS: CONCLUÍDO**

- Apresentar as partidas dos favoritos ao abrir a aplicação, sem bloquear a
  consulta manual existente.
- Carregar cada favorito independentemente; mostrar modo, provider, destino,
  horário e rótulo realtime/agendado.
- Tratar carregamento, vazio, erro e provider indisponível; atualizar horários
  manualmente.
- Validar que favoritos de comboio agregam partidas dos destinos da estação.
- Browser: favoritos de Carris Metropolitana e CP carregaram partidas futuras,
  ordenadas; CP agregou destinos da estação e mostrou horários agendados.

### [X] M11 — Consulta manual rápida e consistência multimodal

**STATUS: CONCLUÍDO**

- Permitir guardar um favorito a partir da paragem selecionada (linha +
  paragem) ou estação selecionada (CP).
- Permitir remover e reordenar favoritos sem apagar a pesquisa manual.
- Preservar os fluxos existentes de autocarros e comboios; Metro e Barcos
  continuam sem dados simulados.
- Validar persistência após reload e regressões nos fluxos dos providers.
- Browser: guardou Carris Metropolitana 4725/paragem e Carris Lisboa
  714/Outurela, com horários futuros agendados; guardou a estação CP Lisboa
  Rossio, com partidas futuras agregadas. A reordenação persistiu após reload e
  a remoção funcionou. Metro e Barcos mostraram indisponibilidade sem dados
  fictícios. Não foram observados erros de browser após corrigir os problemas
  encontrados durante a validação.

## Fora do plano imediato

- Novos operadores além dos já registados.
- GitHub Actions e atualização automática de feeds.
- Generalização do Transitland.
- Realtime por provider para Carris Lisboa e CP; avaliar fontes oficiais antes
  de misturar previsões com schedules estáticos.
- Alterações arquiteturais amplas, novas dependências ou abstrações prematuras.

## Checklist de fecho de milestone

1. Confirmar que as alterações ficam dentro do escopo.
2. Adicionar ou atualizar testes relevantes.
3. Executar `dotnet build` e `dotnet test`.
4. Validar dados estáticos, quando aplicável.
5. Rever ficheiros não rastreados e versionar apenas os artefactos necessários.
6. Atualizar este documento com o estado final.
