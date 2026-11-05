# MyBusApp --- Plano de Desenvolvimento

> **Fonte da verdade do roadmap.** Atualizar no fecho de cada milestone
> ou quando uma decisão alterar a sequência.
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
-   Consultar `.github/copilot-instructions.md` e as skills relevantes
    em `.github/skills/` antes de qualquer tarefa de desenvolvimento.
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
    usam provider, número público da linha e `StopId`; comboios usam
    provider, `StationId` e `DestinationId` (`TrainDirection.Id`). Os
    nomes apresentados não são identificadores.

### 3.1 Decisões aprovadas para favoritos (M8)

1. **Identidade dos favoritos de autocarro:** `Provider + número público
   da linha + StopId`. O provider integra a identidade; o nome da
   paragem não é usado como identificador; a mesma paragem pode ser
   guardada em linhas diferentes.
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

  Fertagus                Comboios                Por auditar/integrar

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
    provider, linha e identificador da paragem; para comboios, exigir
    igualdade de provider, ID da estação e `TrainDirection.Id` do
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
    provider, número público da linha e `StopId`; comboios CP por
    provider, `StationId` e `DestinationId` (`TrainDirection.Id`).
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

### \[ \] M10 --- Página inicial com próximas partidas dos favoritos

**Objetivo:** ao abrir a aplicação, apresentar imediatamente os locais
habituais e os próximos horários disponíveis.

-   [ ] Criar a experiência inicial centrada nos favoritos.
-   [ ] Mostrar cada favorito com nome e modo; apresentar o operador real
    apenas quando estiver identificado e validado.
-   [ ] Consultar automaticamente as próximas partidas de cada favorito.
-   [ ] Para cada favorito CP com destino, consultar apenas as partidas
    do `TrainDirection.Id` guardado e apresentar claramente estação,
    destino, partidas e estado dos horários. Não agregar destinos.
-   [ ] Identificar favoritos CP antigos sem destino como necessitando
    de nova seleção; não consultar todos os sentidos nem inferir um
    destino.
-   [ ] Mostrar partidas sem misturar incorretamente serviços ou
    direções.
-   [ ] Mostrar hora prevista/programada e tempo restante quando os
    dados permitirem calculá-lo.
-   [ ] Identificar cada resultado como Tempo real ou Agendado.
-   [x] Disponibilizar atualização manual e atualização periódica
    automática configurável (300 segundos por omissão), sem assumir que
    todos os providers suportam realtime.
-   [x] Evitar consultas de atualização simultâneas ou sobrepostas.
-   [x] Suspender a atualização periódica enquanto a página não estiver
    ativa ou estiver em segundo plano.
-   [x] Isolar falhas: um favorito indisponível não deve impedir os
    restantes de carregar.
-   [ ] Mostrar estados distintos para carregamento, sem partidas,
    indisponibilidade e erro.
-   [x] Permitir abrir o favorito para consultar mais partidas e
    detalhes.
-   [ ] Garantir comportamento responsivo em dispositivos móveis e
    desktop.
-   [ ] Validar manualmente favoritos de autocarros e comboios já
    integrados.

**Evidências do critério de navegação:** «Ver mais partidas» abre a
pesquisa de autocarros preservando fornecedor, linha e paragem, e a
pesquisa CP preservando estação e destino. Favoritos CP legados abrem
com a estação selecionada, sem inferência de destino. Foram aprovados
16 testes focados e 95 testes no total; o build terminou com 0 avisos e
0 erros. A validação manual foi aprovada em desktop (1280 x 800) e
mobile (390 x 844); os favoritos permaneceram guardados ao regressar ao
Dashboard. Não foram observados erros nas sequências monitorizadas.

**Critérios:** abrir a aplicação permite ver próximas partidas sem
repetir pesquisas; falhas num favorito não bloqueiam os restantes;
origem dos horários inequívoca; fluxo validado no browser.

### \[ \] M11 --- Consulta manual rápida e consistência multimodal

**Objetivo:** manter a pesquisa manual como complemento dos favoritos,
com acesso direto às próximas partidas.

-   [ ] Rever os fluxos atuais de pesquisa de autocarros e comboios à
    luz do objetivo de consulta rápida.
-   [ ] Analisar a resolução de linhas Transitland quando vários
    resultados partilham o mesmo número público; preservar a identidade
    aprovada dos favoritos de autocarro.
-   [ ] Reduzir passos desnecessários entre selecionar linha/estação,
    direção/destino, paragem e ver partidas.
-   [ ] Permitir adicionar aos favoritos uma paragem/estação selecionada
    sem repetir a pesquisa.
-   [ ] Manter consistência de apresentação entre modos sem forçar
    contratos técnicos universais.
-   [ ] Manter modos sem provider real claramente indisponíveis, sem
    dados simulados.
-   [ ] Validar regressões nos fluxos Carris Metropolitana, Carris
    Lisboa e CP.

**Critérios:** fluxos existentes funcionais; pesquisa manual conduz
rapidamente às partidas; favoritos criados a partir da pesquisa;
regressão manual aprovada.

### \[ \] M12 --- Auditoria das fontes e estratégia por operador

**Objetivo:** substituir suposições por uma matriz verificada de fontes
programadas e realtime, priorizando a consulta de partidas.

-   [ ] Rever fontes atuais de Carris Metropolitana, Carris Lisboa e CP.
-   [ ] Consultar documentação oficial da Fertagus.
-   [ ] Consultar documentação oficial do Metropolitano de Lisboa.
-   [ ] Consultar documentação oficial da Transtejo/Soflusa.
-   [ ] Identificar GTFS estático, GTFS-RT, API REST, outras fontes
    oficiais ou ausência de fonte pública.
-   [ ] Confirmar URLs, autenticação, limites, licença/condições de uso
    e frequência de atualização.
-   [ ] Confirmar se os dados podem ser utilizados e redistribuídos.
-   [ ] Avaliar cobertura de linhas, viagens, calendários, paragens e
    identificadores.
-   [ ] Avaliar compatibilidade de cada feed com
    `Tools/GtfsDataGenerator`.
-   [ ] Identificar operadores que exigem API própria em vez de GTFS.
-   [ ] Criar matriz por operador: fonte programada, realtime,
    atualização, cobertura, riscos e limitações.
-   [ ] Recomendar ordem de integração com base na qualidade das fontes
    e utilidade para o produto.

**Critérios:** nenhuma fonte considerada disponível sem evidência
verificável; estratégia documentada para horários programados e
realtime, quando possível.

**Fora do âmbito:** alterar código, criar serviços ou automatizar
downloads.

### \[ \] M13 --- Fallback realtime → programado

**Objetivo:** garantir que a consulta continua útil e correta quando
previsões realtime não estão disponíveis.

-   [ ] Auditar comportamento atual de cada serviço integrado.
-   [ ] Identificar como cada serviço representa `IsRealTime` e a origem
    do horário.
-   [ ] Verificar respostas realtime vazias, indisponíveis, inválidas ou
    expiradas.
-   [ ] Definir critérios de validade e expiração por fonte.
-   [ ] Definir associação entre previsões realtime e
    serviços/paragens/viagens programados, quando necessária.
-   [ ] Definir regra de fallback por operador com base nas fontes
    confirmadas em M12.
-   [ ] Verificar se os modelos existentes suportam a distinção; alterar
    apenas se houver lacuna real.
-   [ ] Definir indicadores de interface para Tempo real, Agendado e
    Indisponível.
-   [ ] Criar testes para realtime válido, fallback programado, resposta
    vazia, erro, dados expirados e ausência de serviço.
-   [ ] Implementar por operador, sem criar motor universal
    antecipadamente.

**Critérios:** comportamento documentado e testado por operador; nenhum
horário programado rotulado como realtime; serviços existentes
preservados.

### \[ \] M14 --- Integração de operadores adicionais

**Objetivo:** disponibilizar partidas de mais operadores, um de cada
vez, segundo a auditoria M12.

Candidatos: - \[ \] Fertagus --- Comboios. - \[ \] Metropolitano de
Lisboa --- Metro. - \[ \] Transtejo/Soflusa --- Barcos.

**Ciclo obrigatório por operador:** - \[ \] Confirmar fonte oficial e
condições de utilização. - \[ \] Confirmar cobertura e qualidade dos
dados. - \[ \] Decidir GTFS estático, API própria ou combinação. - \[ \]
Implementar/estender processamento estático, se aplicável. - \[ \]
Implementar serviço específico e contrato do respetivo modo. - \[ \]
Implementar realtime, se existir fonte utilizável. - \[ \] Implementar
fallback programado, se existir fonte. - \[ \] Permitir guardar
paragens/estações desse operador nos favoritos. - \[ \] Adicionar testes
com dados reais ou fixtures fiéis. - \[ \] Validar build e testes. - \[
\] Validar fluxo completo no browser. - \[ \] Confirmar ausência de
regressões nos operadores existentes. - \[ \] Atualizar o plano com
decisões, limitações e estado.

**Critérios por operador:** fonte documentada; consulta contextual
funcional; próximas partidas coerentes; origem realtime/agendada
correta; integração com favoritos; fallback funcional quando aplicável;
build, testes e validação manual aprovados.

A ordem dos operadores será decidida com base na auditoria M12, não
assumida antecipadamente.

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

**M9 concluído — M9.1, integridade e unicidade; M9.2, tratamento de
erros de persistência; M9.3, testes de identidade e compatibilidade;
M9.4, interações do Dashboard e persistência; e M9.5, validação
funcional manual no browser, concluídos.**

**Próximo passo do roadmap: M10 — Página inicial com próximas partidas
dos favoritos.** M10 permanece por iniciar nesta atualização. As
limitações de recuperação de favoritos duplicados permanecem registadas
nas secções M9.1 e M9.2.
