# MyBusApp --- Plano de Desenvolvimento

> **Fonte da verdade do roadmap.** Único plano ativo do projeto; planos
> anteriores estão em [`archive/`](archive/README.md) e não devem ser
> seguidos. Atualizar no fecho de cada milestone ou quando uma decisão
> alterar a sequência.
>
> **Estado:** `[X]` concluído; `[ ]` por realizar; `[~]` em curso; `[!]`
> bloqueado/dependente de decisão.
>
> Não marcar tarefas como concluídas apenas porque o código compila.
> Exigir testes e, quando aplicável, validação com dados reais e no
> browser.

## 1. Visão e objetivo

**O MyBusApp é o consultor rápido de horários e partidas de transportes
públicos.**

O objetivo principal é permitir que uma pessoa que já conhece o seu
percurso consulte, com o mínimo de passos possível, quando parte o
próximo autocarro, metro, comboio ou barco numa paragem ou estação que
pretende utilizar.

O MyBusApp não pretende, na primeira fase, descobrir o percurso entre
uma origem e um destino. O utilizador já sabe quais são as paragens,
estações, linhas e ligações que costuma utilizar. Precisa de informação
clara e rápida para decidir se apanha um transporte, espera pelo
seguinte ou utiliza outra ligação.

### 1.1 Experiência principal

-   O utilizador guarda as paragens e estações que utiliza
    frequentemente.
-   Pode organizar os favoritos pela ordem que fizer sentido para a sua
    rotina.
-   Ao abrir a aplicação, vê imediatamente os favoritos e as próximas
    partidas de cada um, sem repetir pesquisas.
-   Pode abrir um favorito para consultar mais partidas, linhas,
    destinos/sentidos e detalhes disponíveis.
-   Pode pesquisar manualmente outros locais, linhas ou operadores.
-   A informação distingue claramente previsões em tempo real de
    horários programados.

### 1.2 Princípios de produto

-   **Rapidez primeiro:** reduzir passos entre abrir a aplicação e ver a
    próxima partida.
-   **Favoritos no centro:** a página inicial dá prioridade aos locais
    guardados.
-   **Consulta, não planeamento:** não calcular rotas nem exigir um
    destino para mostrar horários.
-   **Multimodalidade:** consultar autocarros, metro, comboios e barcos,
    respeitando as diferenças entre operadores e serviços.
-   **Informação honesta:** identificar a origem e atualidade dos
    horários; nunca inventar previsões.
-   **Pesquisa manual disponível:** favoritos não substituem a
    possibilidade de consultar outros locais.
-   **Incrementalidade:** integrar e validar um operador de cada vez.

## 2. Âmbito

### 2.1 Primeira fase --- produto principal

-   [ ] Gestão de paragens e estações favoritas.
-   [ ] Organização/reordenação dos favoritos pelo utilizador.
-   [ ] Página inicial centrada nos favoritos.
-   [ ] Consulta imediata das próximas partidas por favorito.
-   [ ] Atualização dos horários sem obrigar a repetir a pesquisa.
-   [ ] Pesquisa manual de paragens, estações, linhas, destinos/sentidos
    e operadores.
-   [ ] Identificação visível de **Tempo real**, **Agendado** e
    **Indisponível**.
-   [ ] Fallback de realtime para horários programados, quando exista
    fonte programada válida.
-   [ ] Integração incremental dos operadores relevantes, conforme a
    disponibilidade real das fontes.

### 2.2 Fora da primeira fase

-   [ ] GPS e descoberta automática de paragens/estações próximas.
-   [ ] Cálculo de rotas entre origem e destino.
-   [ ] Planeamento de viagens e sugestões automáticas de percursos.
-   [ ] Navegação passo a passo.
-   [ ] Identificação de veículos individuais pela sua posição em tempo
    real, salvo decisão posterior fundamentada.

Estas funcionalidades ficam adiadas para uma segunda fase. Não devem
condicionar a arquitetura nem atrasar o objetivo principal.

## 3. Princípios técnicos e restrições

-   Manter .NET 9, Blazor WebAssembly, Bootstrap e `System.Text.Json`.
-   Ler [`AGENTS.md`](../AGENTS.md) e, conforme a tarefa,
    [`architecture.md`](architecture.md), [`domain.md`](domain.md),
    [`conventions.md`](conventions.md) ou uma skill em
    `.github/skills/`.
-   Fazer alterações pequenas, incrementais e limitadas ao milestone
    atual.
-   Preservar serviços existentes que já funcionam.
-   Manter contratos e modelos específicos por modo; não criar uma
    abstração universal de transportes.
-   `IBusService` permanece no domínio dos autocarros; `ITrainService`
    permanece no domínio dos comboios.
-   `BusServiceRouter` permanece limitado aos autocarros. Não criar
    router multimodal sem necessidade demonstrada.
-   Transitland é uma fonte/agregador técnico no contexto dos
    autocarros, não um operador nem um modo. Não apresentar Transitland
    na interface como empresa transportadora. Apresentar o operador
    real apenas quando essa informação estiver disponível e validada;
    caso contrário, não atribuir nem inventar um nome. Preservar a
    identificação técnica do provider internamente quando necessária
    para resolver consultas e identificar favoritos.
-   Reutilizar `Tools/GtfsDataGenerator` quando a fonte for GTFS
    compatível.
-   Não processar ZIPs GTFS no browser.
-   Separar aquisição, transformação, publicação e consumo de dados.
-   Não assumir que todos os operadores disponibilizam GTFS, API ou
    realtime.
-   Não introduzir dependências, factories, repositories, camadas ou
    abstrações sem necessidade demonstrada.
-   Não criar operadores, horários ou previsões fictícias.
-   Não fazer stage, commit, reset, checkout ou revert sem autorização
    explícita.
-   Os favoritos são persistidos no `localStorage` do navegador. Nesta
    fase, não existe sincronização entre navegadores ou dispositivos.
-   Os favoritos usam identificadores definidos por modo: autocarros
    usam provider, número público da linha, `StopId` e sentido
    (`BusDirection.Id`); comboios usam provider, `StationId` e
    `DestinationId` (`TrainDirection.Id`). Os nomes apresentados não são
    identificadores.

### 3.1 Decisões aprovadas para favoritos (M8)

1. **Identidade dos favoritos de autocarro:** `Provider + número público
   da linha + StopId + DirectionId`. O provider integra a identidade; o
   nome da paragem não é usado como identificador; a mesma paragem pode
   ser guardada em linhas diferentes. O sentido (`BusDirection.Id`) foi
   acrescentado em 2026-11-10 para que cada favorito apresente apenas as
   partidas do destino escolhido; favoritos antigos sem sentido
   permanecem válidos, pedem a escolha do sentido e não consultam todos
   os sentidos até serem regravados.
2. **Renomeação:** não são permitidos aliases nem nomes personalizados.
   Os nomes apresentados são provenientes dos dados da fonte.
3. **Persistência:** manter `localStorage` no navegador. Não está
   prevista sincronização entre navegadores ou dispositivos nesta fase.
4. **Dashboard:** a página inicial `/` continua a ser o Dashboard de
   favoritos e o ponto de entrada principal da aplicação.
5. **Identidade ferroviária preservada:** `Provider + StationId +
   DestinationId`, sendo `DestinationId` o `TrainDirection.Id`. Os
   favoritos CP antigos sem destino continuam identificados como
   legados; não se infere nem atribui automaticamente um destino.
6. **Separação dos serviços preservada:** `IBusService` e
   `ITrainService` permanecem separados; `BusServiceRouter` continua
   limitado aos autocarros.

**Limitação técnica a tratar no M11:** a pesquisa Transitland pode
selecionar o primeiro resultado quando existem várias linhas com o
mesmo número público. Isto não altera a identidade de favorito aprovada;
a resolução ambígua deve ser analisada na revisão dos fluxos manuais de
autocarro, sem presumir que está resolvida.

### 3.2 Indisponibilidade e estados de consulta — decisões aprovadas (M8)

| Situação | Comportamento aprovado |
|---|---|
| Provider responde com partidas | Apresentar as partidas recebidas. |
| Provider responde corretamente sem partidas | Apresentar «Sem partidas disponíveis». |
| Provider indisponível ou falha de comunicação | Apresentar «Serviço temporariamente indisponível». |
| Dataset inexistente, inválido ou inacessível | Apresentar «Dados indisponíveis». |
| Uma fonte falha, mas outras favoritas funcionam | Isolar a falha e manter os resultados das favoritas que funcionaram. |
| A atualização falha, mas existem resultados anteriores válidos | Preservar os resultados anteriores e informar que a atualização falhou. |

Regras complementares aprovadas:

- Não apresentar dados fictícios ou inventar partidas.
- Não transformar falhas numa lista vazia.
- Não apagar favoritos devido a falhas do provider.
- Não substituir automaticamente dados válidos por dados incompletos.
- Quando não existir fonte alternativa válida, informar claramente a
  indisponibilidade.

| Estado funcional | Significado |
|---|---|
| A carregar | Consulta em execução; mostrar indicador de carregamento. |
| Partidas disponíveis | Apresentar partidas, horários e destino, indicando a origem temporal dos dados. |
| Sem partidas | Consulta bem-sucedida sem partidas para o contexto selecionado. |
| Serviço indisponível | Falha de comunicação ou fonte indisponível. |
| Dados indisponíveis | Dataset inexistente, inválido ou inacessível. |
| Horário agendado | Dados provenientes de horários programados, não de informação em tempo real. |
| Atualização falhou | Preservar resultados anteriores válidos e informar que não foi possível atualizá-los. |

Regras semânticas aprovadas:

- «Sem partidas» não é um erro.
- «Horário agendado» não é um erro nem um estado de carregamento;
  identifica a natureza dos dados.
- Não apresentar dados antigos como atuais sem indicar que são
  resultados anteriores.
- Distinguir sempre informação em tempo real de informação agendada.
- Não expor mensagens técnicas brutas ao utilizador.

## 4. Estado conhecido do projeto

### 4.1 Operadores e fontes

  -----------------------------------------------------------------------
  Operador/fonte          Modo                    Estado conhecido
  ----------------------- ----------------------- -----------------------
  Carris Metropolitana    Autocarros              Integrado; API própria;
                                                  pesquisa e chegadas
                                                  validadas manualmente

  Carris Lisboa           Autocarros              Integrado; GTFS
                                                  estático convertido
                                                  para JSON; validado
                                                  manualmente

  CP                      Comboios                Integrado via
                                                  `ITrainService`; GTFS
                                                  estático; validado
                                                  manualmente

  Transitland             Fonte/agregador de      Integração existente;
                          autocarros              não é operador nem modo

  Fertagus                Comboios                Integrado via
                                                  `ITrainService`; GTFS
                                                  oficial; horários
                                                  agendados; validado
                                                  manualmente

  Metropolitano de Lisboa Metro                   Por auditar/integrar

  Transtejo/Soflusa       Barcos                  Por auditar/integrar
  -----------------------------------------------------------------------

A lista é um inventário inicial, não confirmação de que cada operador
disponibiliza fontes públicas utilizáveis. Oficialidade, licença,
frequência, cobertura, formato e redistribuição devem ser verificados.

### 4.2 Infraestrutura e trabalho concluído

-   [x] Aplicação Blazor WebAssembly em .NET 9.
-   [x] Modelos comuns `GtfsStatic*`.
-   [x] `Tools/GtfsDataGenerator` genérico, com suporte a ZIP e URL.
-   [x] Schema estático versão 2 e manifesto.
-   [x] Dados CP em `wwwroot/data/cp/`.
-   [x] Dados Carris Lisboa em `wwwroot/data/carris-lisboa/`.
-   [x] `IBusService` para autocarros.
-   [x] `ITrainService` para comboios.
-   [x] `BusServiceRouter` limitado ao domínio dos autocarros.
-   [x] Composição ferroviária através de DI.
-   [x] Experiência contextual para Autocarros, Metro, Comboios e
    Barcos.
-   [x] Testes automatizados para os fluxos integrados.
-   [x] Validação manual de Carris Metropolitana (4725 e 3710), Carris
    Lisboa (714) e CP.
-   [x] Correção da filtragem de chegadas da Carris Metropolitana para
    IDs prefixados.
-   [x] Correção da apresentação do número público da carreira da Carris
    Metropolitana.
-   [x] Build e testes reportados como aprovados após as últimas
    alterações.
-   [x] M7 --- arquitetura multimodal e validação funcional concluídas.

**Registo de 2026-11-03 --- uniformização visual dos favoritos e da navegação**

-   [x] Autocarros e Comboios usam a mesma estrela acessível para
    adicionar/remover favoritos, com estado consultado e persistido pelo
    `FavoriteStore`.
-   [x] O menu apresenta os ícones dos cinco modos e mantém navegação
    ativa e responsiva.
-   [x] Testes automatizados, build e cenários de browser de adicionar,
    recarregar, remover e consultar favoritos validados.
-   **Decisão:** manter formato/chave e identidade existentes dos
    favoritos; centralizar a leitura e alteração do estado no store.
-   **Estado dos milestones:** M8--M18 permanecem por realizar; esta
    alteração visual não satisfaz os critérios completos de nenhum
    desses milestones.
-   **Ajuste visual subsequente:** estrela reduzida para 36 px com glifo
    menor; emojis dos modos também apresentados nos títulos das cinco
    páginas. Build e confirmação visual no browser aprovados; os testes
    automatizados não foram repetidos para este ajuste apenas visual.

**Registo --- correção dos estados de consulta dos favoritos**

-   [x] Os serviços usados pelos favoritos distinguem respostas válidas
    sem partidas de falhas de fonte, de rede e de dados inválidos.
-   [x] O dashboard isola falhas por favorito; testes confirmam que
    resultados dos restantes favoritos continuam visíveis.
-   [x] Teste de regressão confirma que uma falha de atualização mantém
    partidas válidas anteriores, apresenta indisponibilidade em vez de
    ausência de partidas e não impede a atualização dos restantes
    favoritos.
-   [x] Testes automatizados (63) e `dotnet build` concluídos sem falhas.
-   **Limitações:** não foi feita validação manual no browser; a
    apresentação visual dos estados durante uma consulta continua por
    validar manualmente. M8 e M10 permanecem incompletos.

**Registo --- M10.1, atualização periódica do dashboard**

-   [x] Atualização periódica configurada em
    `FavoritesDashboard:RefreshIntervalSeconds`, com intervalo inicial de
    300 segundos (5 minutos), para limitar chamadas a fontes com
    capacidades e limites diferentes; ajustar após validar limites
    específicos de cada fonte.
-   [x] A atualização manual e a automática reutilizam a mesma consulta
    e uma proteção comum impede sobreposição.
-   [x] Atualizações automáticas param com o documento oculto ou o
    componente descartado e retomam quando o documento volta a estar
    visível; não há timer ativo sem favoritos.
-   [x] Testes do dashboard com tempo controlado, suite completa (66) e
    `dotnet build` concluídos sem falhas; módulo de visibilidade carregado
    no browser.
-   **Limitação:** os contratos dos serviços não aceitam tokens de
    cancelamento; uma consulta ao provider já em curso não pode ser
    interrompida. Após descarte, a sua conclusão é ignorada e não
    agenda novas consultas. M10 permanece incompleto; faltam validar
    partidas reais no dashboard e estados no browser.

**Registo --- alinhamento da estrela de favoritos em Comboios**

-   A estrela CP foi movida para o cabeçalho do cartão de partidas,
    alinhada à direita junto ao nome da estação, seguindo a estrutura
    do cabeçalho de Autocarros. Antes de escolher um destino, mantém-se
    junto à identificação da estação para preservar a disponibilidade
    atual de adicionar/remover o favorito.
-   O componente partilhado `FavoriteStar` e os handlers existentes
    continuam a controlar o estado e a operação do favorito.
-   Testes automatizados (66) e build concluídos sem falhas; a
    validação visual no browser ficou limitada ao carregamento inicial,
    pois não foi possível concluir o carregamento das estações CP no
    ambiente local.
-   M8 e M10 permanecem incompletos.

**Registo de 2026-11-05 --- identificação dos favoritos de autocarros**

-   O dashboard identifica separadamente **Linha** (`TransitFavorite.LineNumber`)
    e **Paragem** (`TransitFavorite.StopName`); cada partida identifica
    **Destino** com `BusArrival.Destination`, sem inferir o destino a
    partir da linha.
-   Um destino em falta é apresentado como **Indisponível**. Os horários,
    os indicadores **Tempo real**/**Agendado**, e as ações de ordenar e
    remover favoritos foram preservados. Os campos e ações adaptam-se a
    larguras menores; não foi necessário CSS adicional.
-   Testes do dashboard (5), suite completa (66) e `dotnet build`
    concluídos sem falhas; build com 0 avisos e 0 erros.
-   Validação visual local em desktop e mobile confirmou os rótulos e a
    adaptação dos controlos. A consulta do favorito temporário de
    verificação recebeu HTTP 400 devido ao ID de paragem de teste; por
    isso, a apresentação visual de partidas reais não foi comprovada.
    Testes com serviço controlado cobrem destino presente e ausente.
-   M8 e M10 permanecem incompletos.

**Registo de 2026-11-05 --- identificação dos controlos dos favoritos**

-   Os controlos que pareciam caixas vazias são botões HTML de mover
    para cima (`MoveAsync`), mover para baixo (`MoveAsync`) e remover
    (`RemoveAsync`), presentes no cabeçalho dos cartões de todos os
    modos. Não são checkboxes e não estão duplicados.
-   Os botões mantêm os eventos e labels acessíveis; o conteúdo visual
    foi tornado explícito como **↑ Subir**, **↓ Descer** e **Remover**.
    A causa era o uso de classes `bi-*` sem stylesheet Bootstrap Icons
    carregado pela aplicação. A estrela (`FavoriteStar`) não foi
    alterada.
-   Testes do dashboard (5), suite completa (66) e build concluídos sem
    falhas; build com 0 avisos e 0 erros. Revisão visual local confirmou
    a presença dos controlos identificados em desktop e mobile; a
    consulta real de partidas não foi validada nesta tarefa.
-   M8 e M10 permanecem incompletos.

**Esclarecimento de 2026-11-05 --- controlo branco no resultado de Autocarros**

-   No screenshot do resultado da pesquisa, o controlo branco imediatamente
    depois da estrela é um botão HTML funcional de atualização das chegadas,
    associado a `RefreshBusArrivalsAsync`; não é um checkbox nem um controlo
    residual. A função foi mantida e o botão recebeu um ícone acessível de
    duas setas circulares, com dimensões iguais às da estrela, substituindo
    o botão retangular. O rótulo acessível e tooltip continuam a identificar
    a ação; a estrela não foi alterada.
-   O build passou sem avisos nem erros. A validação visual no browser
    não foi concluída nesta alteração porque a página carregou, mas o
    controlo de pesquisa não atualizou o estado da aplicação. A suite
    completa não foi executada.

**Registo de 2026-11-05 --- favoritos CP por estação e destino**

-   A identidade dos favoritos CP passou a ser provider + ID da estação
    + `TrainDirection.Id` do destino. A identidade dos favoritos de
    autocarros não foi alterada. O ID usado é o mesmo aceite por
    `GetDeparturesAsync(stationId, directionId)`; o texto apresentado
    nunca é usado como identidade.
-   `TransitFavorite.DestinationId` é opcional para manter a leitura de
    JSON antigo. A chave `mybusapp:favorites:v1` não mudou. Favoritos CP
    antigos sem destino permanecem guardados, são identificados como
    necessitando de nova seleção e não disparam consultas agregadas nem
    recebem um destino inferido. O utilizador pode removê-los ou seguir
    para `/trains` e guardar uma combinação específica; favoritos de
    autocarro são preservados.
-   Em Comboios, a estrela só aparece após a seleção de estação e
    destino, e representa essa combinação. Foi posicionada no cabeçalho
    azul das partidas, junto à estação e ao destino, seguindo o padrão
    visual da página Autocarros, em vez de ficar junto aos dropdowns. A
    lista e o cabeçalho apresentam o destino terminal da direção; o
    dashboard resolve o nome da direção guardada e consulta partidas
    apenas pelo ID persistido.
    Destinos indisponíveis são indicados sem consultar outros sentidos.
-   Testes de store, página e dashboard (15 focados; 71 na suite
    completa) passaram. `dotnet build` passou com 0 avisos e 0 erros.
    `git -c core.whitespace=cr-at-eol diff --check` passou, respeitando
    as terminações CRLF existentes.
-   Validação manual em browser com dados CP: Abrantes → Entroncamento
    e Abrantes → Lisboa Santa Apolonia foram guardados e exibidos como
    favoritos independentes, com partidas agendadas; remover Lisboa
    manteve Entroncamento. Os favoritos temporários foram removidos no
    fim. A lista real apresenta “Lisboa Santa Apolonia” como destino
    (não o rótulo literal “Abrantes → Lisboa”); há mais de uma direção
    com esse destino e cada uma tem ID próprio. Não foi feita validação
    em dispositivo físico nem verificação independente da consola após
    o build final.
-   M8 e M10 permanecem incompletos.

**Registo de 2026-11-10 --- favoritos de autocarro por sentido**

-   O sentido (`BusDirection.Id`) passou a integrar a identidade do
    favorito de autocarro: provider + número público da linha + `StopId`
    + sentido. A mesma paragem e linha passam a poder ser guardadas em
    sentidos diferentes.
-   `IBusService.GetArrivalsAsync` aceita o sentido opcional. A Carris
    Metropolitana filtra pelo `pattern_id` e a Carris Lisboa pela rota e
    direção do GTFS; o Transitland não expõe o pattern nas partidas e
    continua a devolver todos os sentidos da linha.
-   A página Autocarros envia o sentido selecionado e o dashboard envia
    o sentido guardado; «Ver mais partidas» reabre a pesquisa com
    `direction`, que pré-seleciona o sentido e a paragem.
-   Favoritos antigos sem sentido permanecem válidos, pedem a escolha
    do sentido e não consultam todos os sentidos; ao guardar a partir da
    pesquisa, o favorito antigo é atualizado em vez de duplicado.
-   Testes focados de store, serviços e páginas; suite com 104 testes e
    build com 0 avisos e 0 erros.
-   Validação em browser com dados isolados: favorito 4725 / Várzea
    (Terminal) P1 com sentido para Lisboa apresenta apenas «Lisboa (Sete
    Rios)»; o mesmo favorito sem sentido continua a apresentar os dois
    sentidos; a página Autocarros com
    `direction=4725_0_2` apresenta apenas as partidas desse sentido.
    Zero erros de consola.
-   **Limitação:** o Transitland não permite filtrar por sentido com os
    dados atuais; a validação foi feita em browser com emulação de
    viewport, não em dispositivos físicos.

**Registo de 2026-11-10 --- M14, integração da Fertagus**

-   Fonte oficial confirmada: GTFS em
    `https://www.fertagus.pt/GTFSTMLzip/Fertagus_GTFS.zip` (HTTP 200,
    87 KB; 3 rotas, 14 estações, 260 viagens e 3 355 stop_times;
    calendário de 2026-09-14 a 2027-03-14). Sem realtime identificado.
-   Dados gerados com `Tools/GtfsDataGenerator` para
    `wwwroot/data/fertagus/`, no formato estático versão 2. O
    `route_short_name` uniforme agrega a operação numa linha lógica
    «Fertagus».
-   O motor estático foi extraído para `GtfsTrainService`; `CpService` e
    `FertagusService` são serviços por operador sobre esse motor,
    registados em `Program.cs`. A Fertagus segue o padrão estático do
    CP: o feed é normalizado fora do browser pelo
    `Tools/GtfsDataGenerator` e a aplicação consome o formato comum
    `GtfsStatic*`, sem DTOs por operador no cliente. A identidade de
    direção passou a incluir o sentido quando a rota tem mais de uma
    direção; os identificadores CP existentes mantêm-se válidos.
-   Favoritos: `FavoriteProvider.Fertagus`, identidade provider +
    estação + destino; dashboard e página Comboios generalizados para
    resolver o serviço pelo provider e aceitar `provider` no URL.
-   Testes: 111 aprovados; build com 0 avisos e 0 erros; fluxo manual
    validado no browser (Fertagus → Roma-Areeiro → Setúbal, cinco
    partidas), favorito apresentado no dashboard e CP sem regressões;
    zero erros de consola.
-   **Limitações:** a lista de destinos mostra rótulos repetidos quando
    dois sentidos terminam na mesma estação (ex.: dois «Roma-Areeiro»),
    tal como já acontecia no CP; sem realtime; validação em browser
    headless.

**Limitação atual:** Metro e Barcos existem como opções de navegação,
mas ainda não têm integrações reais. Não apresentar a opção na interface
como disponibilidade de horários.

## 5. Política de horários e fallback

### 5.1 Precedência

1.  Consultar a fonte realtime do operador, quando exista e esteja
    disponível.
2.  Validar previsões para serviço, linha, direção, paragem/estação e
    período relevante.
3.  Se existirem previsões válidas, apresentá-las como **Tempo real**.
4.  Se não existirem previsões válidas, recorrer ao horário programado
    aplicável, quando exista.
5.  Apresentar horários programados como **Agendado**.
6.  Se nenhuma fonte tiver dados válidos, distinguir uma resposta
    bem-sucedida sem partidas (**Sem partidas disponíveis**), uma falha
    de provider/comunicação (**Serviço temporariamente indisponível**) e
    um dataset inexistente, inválido ou inacessível (**Dados
    indisponíveis**), conforme as decisões aprovadas na secção 3.2.

### 5.2 Integridade dos dados

-   Nunca identificar horário programado como realtime.
-   Nunca apresentar dados realtime expirados como atuais.
-   Distinguir falha técnica, resposta vazia, ausência de serviço e
    falta de previsões.
-   Uma falha realtime não deve apagar horários programados válidos.
-   Respeitar calendário, exceções, data, fuso horário, paragem, direção
    e serviço aplicável.
-   Preservar horários GTFS superiores a 24 horas.
-   Considerar `Europe/Lisbon`, mudanças de hora e exceções de
    calendário.
-   Evitar duplicar partidas ao combinar fontes.
-   Se não existir realtime, usar diretamente o horário programado
    quando disponível.
-   Se não existir fonte programada, não inventar fallback.

### 5.3 Três problemas diferentes de fallback

-   **Fallback de previsões:** realtime → horário programado para o
    mesmo serviço.
-   **Fallback entre providers:** recorrer a outra fonte quando o
    provider selecionado não resolve a pesquisa.
-   **Fallback de distribuição:** manter a última versão estática válida
    quando uma atualização falha.

Não criar uma abstração única para estes três problemas sem necessidade
comprovada.

## 6. Roadmap

A prioridade é entregar primeiro a experiência de favoritos e consulta
rápida usando os operadores já integrados. A expansão de fontes,
realtime e automação deve apoiar essa experiência.

### \[X\] M8 --- Auditoria funcional e desenho da experiência de favoritos

**Objetivo:** perceber como os serviços e páginas atuais podem alimentar
uma página inicial centrada nos favoritos, sem alterar prematuramente os
contratos existentes.

-   [x] Ler instruções e skills do projeto.
-   [x] Inspecionar navegação, páginas, componentes e fluxos atuais de
    autocarros e comboios.
-   [x] Mapear como cada serviço resolve uma paragem/estação e obtém
    próximas partidas.
-   [x] Identificar IDs estáveis disponíveis para provider, número
    público da linha e `StopId` em autocarros, e provider, `StationId` e
    `TrainDirection.Id` em comboios. A ambiguidade de resolução
    Transitland permanece como limitação técnica registada acima.
-   [x] Identificar diferenças entre favoritos de autocarros e comboios.
-   [x] Definir conteúdo mínimo de um favorito: modo, provider, IDs de
    identidade e nome apresentado pela fonte; autocarros guardam também
    o número público da linha e comboios o `DestinationId`.
-   [x] Avaliar a persistência compatível: `localStorage` no navegador,
    sem sincronização nesta fase e sem backend.
-   [x] Definir e aprovar o comportamento para favoritos cujo provider
    ou dataset esteja indisponível, incluindo isolamento de falhas e
    preservação de resultados anteriores válidos.
-   [x] Definir a página inicial `/` como Dashboard principal, com
    favoritos ordenáveis e próximas partidas por local; os detalhes de
    navegação para consulta contextual permanecem no M10.
-   [x] Definir como adicionar, remover e reordenar favoritos e aprovar
    que não há renomeação nem aliases personalizados.
-   [x] Definir e aprovar os estados de consulta, incluindo carregamento,
    partidas disponíveis, ausência de partidas, indisponibilidade,
    dados indisponíveis, horário agendado e falha de atualização.
-   [x] Registar as decisões aprovadas, as lacunas e a proposta de
    continuidade neste plano e no relatório de auditoria.

**Critérios de conclusão:** IDs necessários identificados e estratégia
de persistência justificada; decisões de produto, indisponibilidade e
estados de consulta aprovadas e registadas. Estes critérios de auditoria
e especificação estão satisfeitos e M8 fica concluído. Este fecho não
significa que as decisões estejam implementadas ou visualmente
validadas; essas verificações pertencem aos marcos de implementação
correspondentes. A resolução ambígua Transitland está registada para
análise no M11. A auditoria não altera código.

**Fora do âmbito:** GPS, rotas, backend ou abstrações universais.

### \[X\] M9 --- Modelo e persistência dos favoritos

**Objetivo:** permitir guardar e recuperar paragens e estações
frequentes.

-   [x] Implementar o modelo mínimo de favorito aprovado em M8.
-   [x] Implementar a persistência escolhida em M8, privilegiando a
    solução mais simples compatível.
-   [x] Permitir adicionar um local aos favoritos a partir dos fluxos de
    pesquisa existentes.
-   [x] Permitir remover favoritos.
-   [x] Preservar os nomes fornecidos pela fonte; não permitir aliases
    nem nomes personalizados.
-   [x] Permitir reordenar favoritos.
-   [x] Preservar ordem e dados após recarregar/reabrir a aplicação.
-   [x] Tratar dados guardados inválidos, versões antigas ou referências
    inexistentes.
-   [x] Evitar duplicados em favoritos de autocarro quando coincidirem
    provider, linha, identificador da paragem e sentido; para comboios,
    exigir igualdade de provider, ID da estação e `TrainDirection.Id` do
    destino. Favoritos CP antigos sem `DestinationId` são legados e não
    recebem destino por inferência.
-   [x] Permitir guardar a mesma paragem associada a linhas diferentes.
-   [x] Não usar apenas o nome apresentado para identificar um favorito.
-   [x] Adicionar testes de persistência, ordenação, duplicados, remoção
    e dados inválidos.

**Critérios:** o utilizador consegue guardar, organizar e recuperar
locais favoritos; testes cobrem os casos principais.

#### \[X\] M9.1 --- Integridade e unicidade dos favoritos

-   [x] Centralizar a comparação em
    `TransitFavorite.HasSameIdentityAs`, ignorando nomes e outros campos
    descritivos.
-   [x] Preservar as identidades aprovadas em M8: autocarros por
    provider, número público da linha, `StopId` e sentido; comboios CP
    por provider, `StationId` e `DestinationId` (`TrainDirection.Id`).
-   [x] Serializar, com `SemaphoreSlim`, `AddAsync`, `SetFavoriteAsync`,
    `SaveAsync`, `LoadAsync` e `IsFavoriteAsync` dentro da mesma
    instância de `FavoriteStore`.
-   [x] Fazer `SaveAsync` rejeitar listas com favoritos duplicados.
-   [x] Fazer `LoadAsync` detetar duplicados existentes e falhar
    explicitamente sem eliminar nem modificar o JSON original.
-   [x] Preservar compatibilidade com favoritos CP antigos sem
    `DestinationId`, sem inferir destino.
-   [x] Confirmar `FavoriteStore` registado como Scoped em `Program.cs`.
-   [x] Testes focados de persistência: 14 aprovados.
-   [x] Suite completa: 77 aprovados.
-   [x] `dotnet build`: 0 avisos e 0 erros.
-   [x] `git diff --check`: aprovado.

**Limitação:** favoritos duplicados já existentes no `localStorage`
impedem o carregamento normal até serem resolvidos; o JSON original é
preservado. O fluxo visual de recuperação ainda não foi implementado.

#### \[X\] M9.2 --- Tratamento de erros de persistência

-   [x] O Dashboard distingue carregamento, lista vazia e falha de
    leitura; a falha apresenta uma mensagem adequada e permite nova
    tentativa.
-   [x] Uma falha de leitura não apaga nem sobrescreve os dados
    existentes.
-   [x] Remoção e reordenação só atualizam o estado visual depois de
    `SaveAsync` concluir com sucesso; uma falha de escrita preserva a
    lista anterior e informa o utilizador.
-   [x] A sincronização de operações implementada em M9.1 foi
    preservada.
-   [x] Testes focados: 27 aprovados.
-   [x] Suite completa: 84 aprovados.
-   [x] `dotnet build`: 0 avisos e 0 erros.
-   [x] Check de whitespace dos três ficheiros alterados: aprovado.
-   [x] `git diff --check` global com `core.whitespace=cr-at-eol`:
    aprovado.

**Nota de validação:** o `git diff --check` global sem a opção
`core.whitespace=cr-at-eol` assinalou a terminação CRLF de uma linha
preexistente em `Program.cs`. `Program.cs` não foi alterado na tarefa
M9.2; a verificação com a configuração compatível com CRLF foi aprovada.

**Limitações:** dados inválidos ou duplicados continuam preservados no
`localStorage`, mas não são carregados. Não foi introduzida recuperação
automática nem eliminação de dados; o fluxo visual de recuperação de
favoritos duplicados ainda não foi implementado.

#### \[X\] M9.3 --- Testes de identidade e compatibilidade

-   [x] Cobrir as regras de identidade dos favoritos de autocarro e
    comboio CP, incluindo nomes descritivos diferentes e destinos
    distintos.
-   [x] Testar favoritos CP legados sem `DestinationId` em conjunto com
    favoritos com destino específico.
-   [x] Testar dados inválidos, JSON inválido, enums desconhecidos e
    preservação do conteúdo persistido perante falhas de validação.
-   [x] Confirmar que favoritos permanecem guardados quando as
    referências de linha, paragem ou destino deixam de estar disponíveis.
-   [x] Testes focados: 34 aprovados.
-   [x] Suite completa: 90 aprovados.
-   [x] `dotnet build`: 0 avisos e 0 erros.

**Limitação:** não existe teste dedicado à página Buses; os
comportamentos relevantes foram cobertos através do `FavoriteStore` e
do Dashboard.

#### \[X\] M9.4 --- Interações do dashboard e persistência

-   [x] Testar remoção e reordenação de favoritos através do Dashboard.
-   [x] Confirmar a persistência da ordem e a recuperação da lista ao
    recarregar o Dashboard.
-   [x] Confirmar a preservação dos restantes favoritos, incluindo
    favoritos CP legados sem destino.
-   [x] Confirmar que a interface só reflete a remoção após a gravação
    bem-sucedida e preserva o estado anterior enquanto a gravação está
    pendente ou falha.
-   [x] Testes focados: 35 aprovados.
-   [x] Suite completa: 92 aprovados.
-   [x] `dotnet build`: 0 avisos e 0 erros.

**Limitação de validação:** o `git diff --check` global sem configuração
específica assinala a terminação CRLF preexistente em `Program.cs`; a
verificação global com `core.whitespace=cr-at-eol` foi aprovada.

#### \[X\] M9.5 --- Validação funcional manual no browser

-   [x] Adicionar favoritos de autocarro e comboio CP a partir dos
    respetivos fluxos de pesquisa e confirmar a sua apresentação no
    Dashboard.
-   [x] Reordenar favoritos e confirmar a persistência da ordem após
    recarregar a página.
-   [x] Remover favoritos e confirmar que os restantes são preservados
    após recarga.
-   [x] Confirmar a compatibilidade com favorito CP legado sem destino:
    apresentado como necessitando de seleção, sem inferência automática.
-   [x] Executar os cenários em browser com viewport desktop de
    1280 x 800 e mobile de 390 x 844, usando uma origem local isolada
    para os dados de teste.

**Limitação:** a monitorização da consola e dos pedidos HTTP não cobriu
continuamente todos os passos iniciais. Nos passos monitorizados não
foram observados erros de consola, falhas de pedidos ou respostas HTTP
com estado igual ou superior a 400.

### \[X\] M10 --- Página inicial com próximas partidas dos favoritos

**Objetivo:** ao abrir a aplicação, apresentar imediatamente os locais
habituais e os próximos horários disponíveis.

-   [x] Criar a experiência inicial centrada nos favoritos.
-   [x] Mostrar cada favorito com nome e modo; apresentar o operador real
    apenas quando estiver identificado e validado.
-   [x] Consultar automaticamente as próximas partidas de cada favorito.
-   [x] Para cada favorito CP com destino, consultar apenas as partidas
    do `TrainDirection.Id` guardado e apresentar claramente estação,
    destino, partidas e estado dos horários. Não agregar destinos.
-   [x] Identificar favoritos CP antigos sem destino como necessitando
    de nova seleção; não consultar todos os sentidos nem inferir um
    destino.
-   [x] Mostrar partidas sem misturar incorretamente serviços ou
    direções.
-   [x] Mostrar hora prevista/programada e tempo restante quando os
    dados permitirem calculá-lo.
-   [x] Identificar cada resultado como Tempo real ou Agendado.
-   [x] Disponibilizar atualização manual e atualização periódica
    automática configurável (300 segundos por omissão), sem assumir que
    todos os providers suportam realtime.
-   [x] Evitar consultas de atualização simultâneas ou sobrepostas.
-   [x] Suspender a atualização periódica enquanto a página não estiver
    ativa ou estiver em segundo plano.
-   [x] Isolar falhas: um favorito indisponível não deve impedir os
    restantes de carregar.
-   [x] Mostrar estados distintos para carregamento, sem partidas,
    indisponibilidade e erro.
-   [x] Permitir abrir o favorito para consultar mais partidas e
    detalhes.
-   [x] Garantir comportamento responsivo em dispositivos móveis e
    desktop.
-   [x] Validar manualmente favoritos de autocarros e comboios já
    integrados.

**Evidências dos critérios 1 a 3 (dashboard inicial, identificação e
consulta automática, 2026-11-10):**

-   A rota `/` é o Dashboard de favoritos: abrir a aplicação apresenta o
    título «Favoritos» e as partidas dos locais guardados sem qualquer
    pesquisa nem interação.
-   Cada cartão identifica o nome e o modo: «Linha 714 / Paragem Cais
    Sodré» com «Carris Lisboa · Autocarros» e a estação com «CP ·
    Comboios»; o Transitland é apresentado como «Fonte técnica:
    Transitland», nunca como operador, e os operadores só aparecem
    quando identificados e validados.
-   A consulta é automática no carregamento: 17 pedidos de dados ou
    partidas observados sem interação, cinco partidas por favorito e
    zero erros de consola.
-   Validado em Chrome headless com Blazor WebAssembly, viewport de
    1280 x 800 e favoritos isolados no `localStorage`.

**Evidências do critério de navegação:** «Ver mais partidas» abre a
pesquisa de autocarros preservando fornecedor, linha e paragem, e a
pesquisa CP preservando estação e destino. Favoritos CP legados abrem
com a estação selecionada, sem inferência de destino. Foram aprovados
16 testes focados e 95 testes no total; o build terminou com 0 avisos e
0 erros. A validação manual foi aprovada em desktop (1280 x 800) e
mobile (390 x 844); os favoritos permaneceram guardados ao regressar ao
Dashboard. Não foram observados erros nas sequências monitorizadas.

**Evidências dos critérios 15 e 16 (validação no browser, 2026-11-10):**

-   Validação em Chrome headless com Blazor WebAssembly, viewports de
    1280 x 800 (desktop) e 390 x 844 (mobile), com os favoritos isolados
    no `localStorage` e verificação independente contra as fontes; os
    cabeçalhos dos favoritos ficaram empilhados em mobile e inalterados
    em desktop.
-   Carris Lisboa: linha 714, paragem Cais Sodré; partidas iguais às do
    GTFS local, todas **Agendado**.
-   Carris Metropolitana: linha 4725, paragem Várzea (Terminal) P1;
    distinção correta entre `estimated_arrival` (**Tempo real**) e
    `scheduled_arrival` (**Agendado**) na mesma lista.
-   CP: estação Cais do Sodre com o destino guardado; partidas iguais às
    do dataset estático e sem agregação da direção oposta.
-   «Ver mais partidas» preservou fornecedor, linha e paragem em
    autocarros, e estação e destino em comboios.
-   A atualização manual preservou os favoritos, a ordem e o conteúdo do
    `localStorage` (valor byte-idêntico), com novo pedido de partidas
    registado.
-   Zero erros de consola ou exceções nos cenários monitorizados.
-   Build com 0 avisos e 0 erros; suite com 96 testes aprovados.

**Limitação destes critérios:** a validação foi feita em browser com
emulação de viewport, não em dispositivos físicos. A atualização
periódica automática, os estados de indisponibilidade e a compatibilidade
com favoritos CP legados não foram exercitados nesta sessão.

**Evidências dos critérios 4 a 8 e 13 (favoritos CP, sentidos,
horários e estados, 2026-11-10):**

-   Favorito CP com destino guardado: consulta apenas o
    `TrainDirection.Id` persistido e apresenta estação, destino, cinco
    partidas e o estado «Agendado»; a direção oposta não é consultada
    nem agregada.
-   Favorito CP antigo sem destino: identificado como «Destino por
    selecionar (favorito antigo)», sem consultar sentidos nem inferir um
    destino; «Selecionar estação e destino» abre a estação sem destino.
-   Favorito de autocarro com sentido guardado (4725, Várzea): apenas as
    partidas desse sentido, sem misturar os dois sentidos; na mesma
    lista, quatro partidas «Tempo real» (estimativa do operador) e uma
    «Agendado».
-   Cada partida mostra a hora e o tempo restante («daqui a N min» ou
    «em breve»), com o rótulo temporal correspondente à origem do dado.
-   Estados distintos: uma linha indisponível apresenta «O serviço ou a
    fonte está indisponível.» sem alterar favoritos; carregamento,
    ausência de partidas e erro estão cobertos pelos testes do
    dashboard.
-   Zero erros de consola; Chrome headless com Blazor WebAssembly,
    viewport de 1280 x 800 e favoritos isolados no `localStorage`.

**Critérios:** abrir a aplicação permite ver próximas partidas sem
repetir pesquisas; falhas num favorito não bloqueiam os restantes;
origem dos horários inequívoca; fluxo validado no browser.

**Estado:** concluído em 2026-11-10. Limitações registadas: o
Transitland não permite filtrar por sentido; favoritos de autocarro
antigos sem sentido pedem a escolha do sentido em vez de mostrarem
todos os sentidos.

### \[X\] M11 --- Consulta manual rápida e consistência multimodal

**Objetivo:** manter a pesquisa manual como complemento dos favoritos,
com acesso direto às próximas partidas.

-   [x] Rever os fluxos atuais de pesquisa de autocarros e comboios à
    luz do objetivo de consulta rápida.
-   [x] Analisar a resolução de linhas Transitland quando vários
    resultados partilham o mesmo número público; preservar a identidade
    aprovada dos favoritos de autocarro. Análise concluída; a correção
    foi adiada para M12 por decisão de 2026-11-10.
-   [x] Manter o fluxo de seleção atual (linha → sentido → paragem e
    operador → estação → destino), por decisão de produto em
    2026-11-10; não reduzir passos.
-   [x] Permitir adicionar aos favoritos uma paragem/estação selecionada
    sem repetir a pesquisa.
-   [x] Manter consistência de apresentação entre modos sem forçar
    contratos técnicos universais.
-   [x] Manter modos sem provider real claramente indisponíveis, sem
    dados simulados.
-   [x] Validar regressões nos fluxos Carris Metropolitana, Carris
    Lisboa e CP.

**Evidências da auditoria e do primeiro lote (2026-11-10):**

-   Auditoria aos fluxos manuais: autocarros em 4 interações (linha →
    Pesquisar → sentido → paragem) e comboios em 3 (operador → estação →
    destino); as partidas carregam automaticamente após a última
    seleção.
-   Sentidos da Carris Lisboa desambiguados pelos extremos do percurso
    («Outurela → Cais Sodré» / «Cais Sodré → Outurela»), em vez de dois
    rótulos iguais.
-   Filtro de pesquisa na estação CP: as 455 estações passam a poder ser
    filtradas por nome, mantendo a estação selecionada na lista.
-   Partidas de comboios passam a mostrar «Agendado»; «Próximas
    passagens» foi unificado para «Próximas partidas».
-   Favorito a partir da pesquisa: estrela presente em Autocarros e
    Comboios após a seleção da paragem ou do destino.
-   Metro e Barcos continuam a apresentar indisponibilidade explícita,
    sem dados simulados.
-   Testes: 106 aprovados; build com 0 avisos e 0 erros; validação em
    Chrome headless (1280 x 800) sem erros de consola.

**Pendente:** o Transitland mantém-se inalterado por decisão de
2026-11-10; a avaliação do seu futuro passa para a auditoria de fontes
(M12), incluindo a chave da API exposta no cliente. A lista de destinos
CP continua a apresentar rótulos repetidos quando várias direções
partilham o mesmo destino, cada uma com ID próprio; a desambiguação
dessa lista fica para o lote seguinte.

**Regressão final (2026-11-10):** os três fluxos foram executados no
browser sobre o build final: Carris Lisboa 714 (sentido «Cais Sodré →
Outurela», paragem Cais Sodré) com cinco partidas, Carris Metropolitana
4725 (sentido «Lisboa (Sete Rios)», paragem Várzea (Terminal) P1) com
cinco partidas e CP (Cais do Sodre → Cascais) com cinco partidas, todas
com o rótulo «Agendado». Zero erros de consola.

**Estado:** concluído em 2026-11-10. Pendências registadas para o lote
seguinte: desambiguação dos destinos CP repetidos e avaliação do
Transitland em M12.

**Critérios:** fluxos existentes funcionais; pesquisa manual conduz
rapidamente às partidas; favoritos criados a partir da pesquisa;
regressão manual aprovada.

### \[X\] M12 --- Auditoria das fontes e estratégia por operador

**Objetivo:** substituir suposições por uma matriz verificada de fontes
programadas e realtime, priorizando a consulta de partidas.

-   [x] Rever fontes atuais de Carris Metropolitana, Carris Lisboa e CP.
-   [x] Avaliar o futuro do Transitland como agregador técnico face às
    fontes oficiais, incluindo a exposição da chave da API no cliente.
-   [x] Consultar documentação oficial da Fertagus.
-   [x] Consultar documentação oficial do Metropolitano de Lisboa.
-   [x] Consultar documentação oficial da Transtejo/Soflusa.
-   [x] Identificar GTFS estático, GTFS-RT, API REST, outras fontes
    oficiais ou ausência de fonte pública.
-   [x] Confirmar URLs, autenticação, limites, licença/condições de uso
    e frequência de atualização.
-   [x] Confirmar se os dados podem ser utilizados e redistribuídos.
-   [x] Avaliar cobertura de linhas, viagens, calendários, paragens e
    identificadores.
-   [x] Avaliar compatibilidade de cada feed com
    `Tools/GtfsDataGenerator`.
-   [x] Identificar operadores que exigem API própria em vez de GTFS.
-   [x] Criar matriz por operador: fonte programada, realtime,
    atualização, cobertura, riscos e limitações.
-   [x] Recomendar ordem de integração com base na qualidade das fontes
    e utilidade para o produto.

**Matriz de fontes (auditada em 2026-11-10):**

| Operador/fonte | Programado | Realtime | Licença/termos | Cobertura e identificadores | Generator | Riscos e limitações |
|---|---|---|---|---|---|---|
| Carris Metropolitana | GTFS oficial em `https://api.carrismetropolitana.pt/gtfs` (74 MB, HTTP 200) | API REST v2 própria, já integrada | dados.gov.pt indica «não especificada»; catálogo externo indica CC BY 4.0 — por confirmar | Rede da AML; IDs técnicos com prefixo de operador | Compatível; feed grande | Limites da API por confirmar; feed pesado |
| Carris Lisboa | GTFS oficial no gateway `v2.11` (em uso) | GTFS-RT listado em catálogo externo (`mdb-2930`), endpoint não verificado | Por confirmar | Rede de Lisboa; IDs estáveis | Compatível (em uso) | Licença e realtime por confirmar |
| CP | Sem feed oficial em dados.gov.pt; registo NAP 176 referido pelo Transitland | Não identificado | Por confirmar (NAP 176) | Nacional; dataset atual sem proveniência documentada | Por confirmar | Risco de proveniência dos dados em uso |
| Fertagus | GTFS oficial em `https://www.fertagus.pt/GTFSTMLzip/Fertagus_GTFS.zip` (87 KB, HTTP 200; 3 rotas, 14 paragens, 260 viagens; calendário 2026-09-14 a 2027-03-14) | Não identificado | CC BY 4.0 via NAP 185 (indicada por catálogo, confirmar termos) | Linha Pragal–Setúbal | Compatível; `route_short_name` uniforme («Fertagus») agrupa numa linha lógica | Sem realtime; mapeamento da linha a decidir |
| Metropolitano de Lisboa | GTFS oficial em dados.gov.pt (660 KB, HTTP 200; 4 rotas, 74 paragens, 2 542 viagens; calendário 2026) | Não identificado | CC BY 4.0 | Rede do Metro de Lisboa | Compatível | Atualização pontual; modo novo |
| Transtejo/Soflusa | URL publicada `https://api.transtejo.pt/files/GTFS.zip` não operacional (HTTP 404 e erro de certificado); organização no dados.gov.pt sem datasets | Não identificado | CC0 indicada por catálogo externo, por confirmar | 5 rotas referidas por catálogo externo | Por confirmar | Bloqueado até existir URL válida |
| Transitland | Agregador (não oficial) | Sem realtime | Termos do agregador | Rotas de vários países | Não aplicável | Filtros ignorados; escolha arbitrária; chave exposta no cliente |

**Ordem de integração recomendada:** 1.º Fertagus (feed oficial
verificado e pequeno, alarga o modo ferroviário); 2.º Metropolitano de
Lisboa (GTFS oficial, mas exige modo e contrato novos); 3.º
Transtejo/Soflusa (bloqueado até existir URL válida). Antes do M14,
confirmar a proveniência do dataset CP (registo NAP 176) e decidir o
futuro do Transitland; com as fontes oficiais cobertas, o Transitland
deve ser desativado e a chave removida do cliente.

**Estado:** concluído em 2026-11-10. Pendências de confirmação: licença
da Carris Lisboa, limites da API da Carris Metropolitana, proveniência
do feed CP e URL válida da Transtejo/Soflusa.

**Critérios:** nenhuma fonte considerada disponível sem evidência
verificável; estratégia documentada para horários programados e
realtime, quando possível.

**Fora do âmbito:** alterar código, criar serviços ou automatizar
downloads.

### \[~\] M13 --- Fallback realtime → programado

**Objetivo:** garantir que a consulta continua útil e correta quando
previsões realtime não estão disponíveis.

-   [x] Auditar comportamento atual de cada serviço integrado.
-   [x] Identificar como cada serviço representa `IsRealTime` e a origem
    do horário.
-   [x] Verificar respostas realtime vazias, indisponíveis, inválidas ou
    expiradas.
-   [x] Definir critérios de validade e expiração por fonte.
-   [x] Definir associação entre previsões realtime e
    serviços/paragens/viagens programados, quando necessária.
-   [x] Definir regra de fallback por operador com base nas fontes
    confirmadas em M12.
-   [x] Verificar se os modelos existentes suportam a distinção; alterar
    apenas se houver lacuna real.
-   [x] Definir indicadores de interface para Tempo real, Agendado e
    Indisponível.
-   [x] Criar testes para realtime válido, fallback programado, resposta
    vazia, erro, dados expirados e ausência de serviço.
-   [x] Implementar por operador, sem criar motor universal
    antecipadamente.

**Critérios:** comportamento documentado e testado por operador; nenhum
horário programado rotulado como realtime; serviços existentes
preservados.

**Registo de 2026-11-11 --- implementação inicial**

-   Auditoria: a Carris Metropolitana é a única fonte com tempo real e
    devolve `scheduled_arrival` e `estimated_arrival` no mesmo registo,
    em hora local (`HH:mm:ss`, sem offset). Numa amostra real de cinco
    paragens, só 1 a 11 registos (de 94 a 268) tinham estimativa: o
    fallback por registo é o caso normal, não a exceção. Carris Lisboa,
    Transitland, CP e Fertagus só têm horário programado e já devolvem
    `IsRealTime = false`. Os comboios não têm flag e são sempre
    "Agendado"; não se acrescentou uma flag sem fonte realtime.
-   Regra aplicada na Carris Metropolitana: previsão legível manda; se
    já passou, o registo é descartado (não se ressuscita a partida);
    previsão ausente ou ilegível usa o programado, se futuro. O "agora"
    passou a ser `Europe/Lisbon` em vez do relógio do dispositivo
    (`DateTime.Now`).
-   Indicadores de interface: sem alterações. "Tempo Real"/"Agendado"
    já existiam para autocarros; erro, vazio e indisponível já são
    estados distintos.
-   Modelos: `BusArrival.IsRealTime` chegou; não foi preciso mudar
    contratos.
-   Testes: 3 casos novos (previsão válida, previsão ilegível com
    fallback, previsão expirada descartada); suite em 117.
-   Pendente: validação manual no browser com dados reais. M13
    permanece em curso.

### \[ \] M14 --- Integração de operadores adicionais

**Objetivo:** disponibilizar partidas de mais operadores, um de cada
vez, segundo a auditoria M12.

**Candidatos:**

-   [x] Fertagus --- Comboios.
-   [ ] Metropolitano de Lisboa --- Metro.
-   [ ] Transtejo/Soflusa --- Barcos.

**Ciclo obrigatório por operador:**

-   [ ] Confirmar fonte oficial e condições de utilização.
-   [ ] Confirmar cobertura e qualidade dos dados.
-   [ ] Decidir GTFS estático, API própria ou combinação.
-   [ ] Implementar/estender processamento estático, se aplicável.
-   [ ] Implementar serviço específico e contrato do respetivo modo.
-   [ ] Implementar realtime, se existir fonte utilizável.
-   [ ] Implementar fallback programado, se existir fonte.
-   [ ] Permitir guardar paragens/estações desse operador nos favoritos.
-   [ ] Adicionar testes com dados reais ou fixtures fiéis.
-   [ ] Validar build e testes.
-   [ ] Validar fluxo completo no browser.
-   [ ] Confirmar ausência de regressões nos operadores existentes.
-   [ ] Atualizar o plano com decisões, limitações e estado.

#### \[X\] M14.1 --- Fertagus (concluído em 2026-11-10)

-   [x] Fonte oficial e condições: GTFS em
    `https://www.fertagus.pt/GTFSTMLzip/Fertagus_GTFS.zip` (HTTP 200);
    licença CC BY 4.0 indicada via NAP 185, termos a confirmar.
-   [x] Cobertura e qualidade: 3 rotas, 14 estações, 260 viagens e
    3 355 stop_times; calendário de 2026-09-14 a 2027-03-14.
-   [x] GTFS estático escolhido; sem fonte realtime identificada.
-   [x] Processamento estático com `Tools/GtfsDataGenerator` para
    `wwwroot/data/fertagus/`.
-   [x] Serviço específico: `FertagusService` sobre o motor
    `GtfsTrainService`, no contrato `ITrainService`.
-   [ ] Realtime: sem fonte identificada; não aplicável nesta fase.
-   [x] Fallback programado: partidas apresentadas como «Agendado».
-   [x] Favoritos: provider Fertagus + estação + destino; dashboard e
    página Comboios.
-   [x] Testes com dados reais e fixtures: serviço, store, página e
    dashboard.
-   [x] Build com 0 avisos e 0 erros; 111 testes aprovados.
-   [x] Fluxo completo validado no browser: Fertagus → Roma-Areeiro →
    Setúbal, cinco partidas; favorito no dashboard; zero erros de
    consola.
-   [x] Sem regressões no CP (fluxo manual revalidado).
-   [x] Plano atualizado.
-   **Limitações:** rótulos de destino repetidos (dois «Roma-Areeiro»),
    como no CP; sem realtime; validação em browser headless.

**Critérios por operador:** fonte documentada; consulta contextual
funcional; próximas partidas coerentes; origem realtime/agendada
correta; integração com favoritos; fallback funcional quando aplicável;
build, testes e validação manual aprovados.

A ordem dos operadores restantes segue a auditoria M12: Metropolitano de
Lisboa e, por último, Transtejo/Soflusa (bloqueada por URL inválida).

### \[ \] M15 --- Pipeline de aquisição e atualização GTFS

**Objetivo:** automatizar obtenção e transformação dos feeds estáticos
confirmados.

-   [ ] Auditar capacidades e limitações de `Tools/GtfsDataGenerator`.
-   [ ] Escolher feeds reais e estáveis como pilotos.
-   [ ] Definir configuração por operador: URL, nome, opções e destino.
-   [ ] Implementar download seguro, timeouts e tratamento de falhas.
-   [ ] Executar o generator existente, sem duplicar lógica.
-   [ ] Validar ZIP e ficheiros GTFS obrigatórios antes da geração.
-   [ ] Validar integridade referencial dos dados gerados.
-   [ ] Validar manifesto, schema e ficheiros de saída.
-   [ ] Detetar feeds sem alterações e evitar publicações
    desnecessárias.
-   [ ] Registar logs e resultado por execução.
-   [ ] Garantir que uma falha não substitui dados válidos existentes.
-   [ ] Documentar execução local e recuperação de falhas.

**Critérios:** pelo menos um feed real processado de ponta a ponta;
processo repetível; generator compatível com datasets existentes;
temporários excluídos da publicação.

### \[ \] M16 --- Publicação e distribuição dos dados estáticos

**Objetivo:** disponibilizar datasets processados num local estável para
consumo pelo MyBusApp.

-   [ ] Comparar GitHub Pages, GitHub Releases, GitHub Raw e alojamento
    estático externo.
-   [ ] Avaliar CORS, cache, limites, tamanho, disponibilidade e
    manutenção.
-   [ ] Escolher repositório/estrutura de publicação.
-   [ ] Definir organização por operador e versão.
-   [ ] Definir manifesto global ou por operador, preservando
    compatibilidade do schema.
-   [ ] Definir política de cache e atualização do manifesto.
-   [ ] Definir publicação atómica: validar primeiro, publicar depois.
-   [ ] Manter a última versão válida se uma atualização falhar.
-   [ ] Adaptar um serviço piloto para consumir dados publicados
    externamente.
-   [ ] Testar carregamento, cache, atualização e indisponibilidade da
    origem.
-   [ ] Documentar como adicionar um dataset.

**Critérios:** MyBusApp consome pelo menos um dataset publicado
externamente; falhas não removem a última versão válida; CORS/cache
funcionam no browser.

### \[ \] M17 --- Automatização com GitHub Actions

**Objetivo:** executar automaticamente aquisição, transformação,
validação e publicação.

-   [ ] Criar workflow programado e execução manual.
-   [ ] Configurar secrets quando necessários; nunca guardar credenciais
    em ficheiros versionados.
-   [ ] Executar feeds por operador de forma isolada.
-   [ ] Executar generator e validações.
-   [ ] Publicar apenas datasets aprovados.
-   [ ] Evitar publicações sem alterações.
-   [ ] Gerar logs e resumo por operador.
-   [ ] Tornar falhas de atualização visíveis.
-   [ ] Definir frequência conforme a fonte oficial.
-   [ ] Testar falhas de rede, feed inválido e erro de publicação.

**Critérios:** workflow manual e programado funcionam; falha num
operador não corrompe datasets dos restantes; última versão válida
permanece disponível; execução auditável.

### \[ \] M18 --- Consolidação e qualidade

-   [ ] Rever logs e tratamento de falhas por serviço.
-   [ ] Identificar tempos de carregamento e chamadas redundantes na
    página de favoritos.
-   [ ] Rever cache e invalidação por fonte.
-   [ ] Rever atualização automática e consumo de rede.
-   [ ] Rever fusos horários, mudanças de hora e horários GTFS
    superiores a 24 horas.
-   [ ] Rever consistência de IDs entre feeds estáticos e realtime.
-   [ ] Rever mensagens de indisponibilidade e ausência de serviço.
-   [ ] Rever acessibilidade e responsividade.
-   [ ] Executar regressão completa dos operadores integrados.
-   [ ] Documentar manutenção e diagnóstico.

**Critérios:** página inicial rápida e previsível; falhas externas
distinguíveis de ausência real de serviço; horários programados ou
antigos nunca apresentados como realtime; fluxos principais cobertos por
testes.

## 7. Segunda fase --- funcionalidades adiadas

Só planear após a primeira fase cumprir o seu objetivo principal e após
decisão explícita de prioridade.

-   [ ] GPS e localização do utilizador.
-   [ ] Descoberta automática de paragens/estações próximas.
-   [ ] Visualização de partidas por proximidade.
-   [ ] Planeamento de rotas entre origem e destino, se existir
    necessidade diferenciadora.
-   [ ] Identificação de veículos individuais em circulação, caso
    existam fontes fiáveis.
-   [ ] Outras funcionalidades de navegação ou planeamento.

A segunda fase deve responder a uma necessidade validada, não ser
iniciada apenas porque existem dados geográficos ou APIs disponíveis.

## 8. Ficha obrigatória de decisão por operador

Preencher antes de implementar cada operador:

  Informação                      Resultado
  ------------------------------- -----------
  Operador e modo                 Pendente
  Fonte oficial                   Pendente
  Fonte de horário programado     Pendente
  Fonte realtime                  Pendente
  Formato dos dados               Pendente
  Frequência de atualização       Pendente
  Licença/condições de uso        Pendente
  Cobertura                       Pendente
  Identificadores e mapeamentos   Pendente
  Compatibilidade com generator   Pendente
  Estratégia de fallback          Pendente
  Integração com favoritos        Pendente
  Riscos e limitações             Pendente
  Decisão de implementação        Pendente

**Ficha preenchida --- Fertagus (2026-11-10):**

| Informação | Resultado |
|---|---|
| Operador e modo | Fertagus --- Comboios |
| Fonte oficial | GTFS em `https://www.fertagus.pt/GTFSTMLzip/Fertagus_GTFS.zip` (HTTP 200) |
| Fonte de horário programado | GTFS estático (3 rotas, 14 estações, 260 viagens; calendário de 2026-09-14 a 2027-03-14) |
| Fonte realtime | Não identificada |
| Formato dos dados | GTFS schedule; sem GTFS-RT |
| Frequência de atualização | Não documentada; ficheiro com `last-modified` de 2026-09-14 |
| Licença/condições de uso | CC BY 4.0 indicada via NAP 185; termos a confirmar |
| Cobertura | Linha Lisboa (Roma-Areeiro) --- Setúbal, via Coina |
| Identificadores e mapeamentos | Estações `FTStop*`; rotas `A_0` e `A_1`; direções `route|direction` |
| Compatibilidade com generator | Sim (ZIP GTFS standard; `route_short_name` uniforme) |
| Estratégia de fallback | Sem realtime: apresentar horários agendados como «Agendado» |
| Integração com favoritos | Provider Fertagus + estação + destino |
| Riscos e limitações | Rótulos de destino repetidos; sem realtime; atualização não documentada |
| Decisão de implementação | Integrado em 2026-11-10 |

Não iniciar desenvolvimento de um operador enquanto as fontes essenciais
e a estratégia de fallback não estiverem compreendidas. Se não existir
fonte realtime, o operador pode ser integrado apenas com horários
programados, desde que isso fique explícito.

## 9. Checklist de fecho de milestone

-   [ ] Consultar instruções e skills relevantes.
-   [ ] Confirmar que o trabalho respeitou o âmbito.
-   [ ] Rever diff e ficheiros alterados.
-   [ ] Confirmar preservação das alterações preexistentes.
-   [ ] Adicionar/atualizar testes relevantes.
-   [ ] Executar `dotnet test MyBusApp.Tests/MyBusApp.Tests.csproj`.
-   [ ] Executar `dotnet build`.
-   [ ] Validar dados reais quando aplicável.
-   [ ] Validar manualmente no browser quando houver alteração
    funcional.
-   [ ] Confirmar ausência de erros relevantes na consola.
-   [ ] Atualizar checkboxes e relatório do milestone.
-   [ ] Não fazer stage nem commit sem autorização explícita.

## 10. Próximo passo

**M13 em curso — regra de validade e fallback implementada na Carris
Metropolitana e coberta por testes; falta validar no browser com dados
reais.**

**M14.1 concluído — Fertagus integrada via `ITrainService` com GTFS
oficial, favoritos por estação e destino e fluxo validado no browser.**

**Depois de fechar o M13, o próximo passo é o M14 — Metropolitano de
Lisboa.** A Transtejo/Soflusa
continua bloqueada até existir uma URL de GTFS válida. Pendências do
M12: licença da Carris Lisboa, limites da API da Carris Metropolitana,
proveniência do feed CP e termos da Fertagus (NAP 185). Limitações ainda
abertas: favoritos de autocarro antigos sem sentido pedem a escolha do
sentido; a recuperação visual de
favoritos duplicados permanece por implementar (M9.1/M9.2); a
desambiguação dos destinos repetidos (CP e Fertagus) fica para o lote
seguinte.
