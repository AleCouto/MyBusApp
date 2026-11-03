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
-   A persistência dos favoritos deve ser decidida após inspeção da
    aplicação. Não introduzir backend ou autenticação apenas para
    guardar favoritos sem necessidade demonstrada.
-   Os favoritos devem guardar identificadores estáveis do provider ou
    fonte técnica e da paragem/estação, não depender apenas do nome
    apresentado.

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
6.  Se nenhuma fonte tiver dados válidos, apresentar **Indisponível** ou
    mensagem adequada à ausência de partidas.

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

### \[ \] M8 --- Auditoria funcional e desenho da experiência de favoritos

**Objetivo:** perceber como os serviços e páginas atuais podem alimentar
uma página inicial centrada nos favoritos, sem alterar prematuramente os
contratos existentes.

-   [ ] Ler instruções e skills do projeto.
-   [ ] Inspecionar navegação, páginas, componentes e fluxos atuais de
    autocarros e comboios.
-   [ ] Mapear como cada serviço resolve uma paragem/estação e obtém
    próximas partidas.
-   [ ] Identificar IDs estáveis disponíveis para operador,
    paragem/estação, linha e direção.
-   [ ] Identificar diferenças entre favoritos de autocarros e comboios.
-   [ ] Definir conteúdo mínimo de um favorito: modo, operador/provider,
    identificador estável, nome apresentado e metadados necessários para
    reabrir a consulta.
-   [ ] Avaliar persistência compatível com a arquitetura atual,
    incluindo armazenamento local no browser, sem decidir
    antecipadamente por dependência ou backend.
-   [ ] Definir comportamento para favoritos cujo operador ou dataset
    esteja temporariamente indisponível.
-   [ ] Desenhar a página inicial: favoritos ordenáveis e próximas
    partidas por local.
-   [ ] Definir como adicionar, remover, renomear e reordenar favoritos.
-   [ ] Definir estados de carregamento, atualização, erro, ausência de
    partidas e dados agendados.
-   [ ] Registar decisões, lacunas e proposta de implementação.

**Critérios:** especificação funcional e técnica aprovada; IDs
necessários identificados; estratégia de persistência justificada;
nenhuma alteração de código nesta auditoria.

**Fora do âmbito:** GPS, rotas, backend ou abstrações universais.

### \[ \] M9 --- Modelo e persistência dos favoritos

**Objetivo:** permitir guardar e recuperar paragens e estações
frequentes.

-   [ ] Implementar o modelo mínimo de favorito aprovado em M8.
-   [ ] Implementar a persistência escolhida em M8, privilegiando a
    solução mais simples compatível.
-   [ ] Permitir adicionar um local aos favoritos a partir dos fluxos de
    pesquisa existentes.
-   [ ] Permitir remover favoritos.
-   [ ] Permitir alterar o nome apresentado, se aprovado.
-   [ ] Permitir reordenar favoritos.
-   [ ] Preservar ordem e dados após recarregar/reabrir a aplicação.
-   [ ] Tratar dados guardados inválidos, versões antigas ou referências
    inexistentes.
-   [ ] Evitar duplicados em favoritos de autocarro quando coincidirem
    provider, linha e identificador da paragem; para comboios, considerar
    provider e identificador da estação.
-   [ ] Permitir guardar a mesma paragem associada a linhas diferentes.
-   [ ] Não usar apenas o nome apresentado para identificar um favorito.
-   [ ] Adicionar testes de persistência, ordenação, duplicados, remoção
    e dados inválidos.

**Critérios:** o utilizador consegue guardar, organizar e recuperar
locais favoritos; testes cobrem os casos principais.

### \[ \] M10 --- Página inicial com próximas partidas dos favoritos

**Objetivo:** ao abrir a aplicação, apresentar imediatamente os locais
habituais e os próximos horários disponíveis.

-   [ ] Criar a experiência inicial centrada nos favoritos.
-   [ ] Mostrar cada favorito com nome e modo; apresentar o operador real
    apenas quando estiver identificado e validado.
-   [ ] Consultar automaticamente as próximas partidas de cada favorito.
-   [ ] Manter a consulta agregada das partidas CP por estação e
    apresentar claramente o destino em cada partida, sem agrupar por
    direção nesta fase nem alterar desnecessariamente o serviço CP.
-   [ ] Mostrar partidas sem misturar incorretamente serviços ou
    direções.
-   [ ] Mostrar hora prevista/programada e tempo restante quando os
    dados permitirem calculá-lo.
-   [ ] Identificar cada resultado como Tempo real ou Agendado.
-   [ ] Disponibilizar atualização manual e atualização periódica
    automática com intervalo prudente, definido durante a implementação
    conforme as capacidades e limitações das fontes; não assumir que
    todos os providers suportam realtime.
-   [ ] Evitar consultas de atualização simultâneas ou sobrepostas.
-   [ ] Suspender a atualização periódica enquanto a página não estiver
    ativa ou estiver em segundo plano.
-   [x] Isolar falhas: um favorito indisponível não deve impedir os
    restantes de carregar.
-   [ ] Mostrar estados distintos para carregamento, sem partidas,
    indisponibilidade e erro.
-   [ ] Permitir abrir o favorito para consultar mais partidas e
    detalhes.
-   [ ] Garantir comportamento responsivo em dispositivos móveis e
    desktop.
-   [ ] Validar manualmente favoritos de autocarros e comboios já
    integrados.

**Critérios:** abrir a aplicação permite ver próximas partidas sem
repetir pesquisas; falhas num favorito não bloqueiam os restantes;
origem dos horários inequívoca; fluxo validado no browser.

### \[ \] M11 --- Consulta manual rápida e consistência multimodal

**Objetivo:** manter a pesquisa manual como complemento dos favoritos,
com acesso direto às próximas partidas.

-   [ ] Rever os fluxos atuais de pesquisa de autocarros e comboios à
    luz do objetivo de consulta rápida.
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

**M8 --- Auditoria funcional e desenho da experiência de favoritos.**

A primeira execução de M8 é exclusivamente de investigação e
especificação: - Ler instruções e skills do projeto. - Inspecionar os
fluxos e serviços atuais. - Identificar IDs estáveis necessários para
guardar paragens e estações. - Avaliar persistência e estratégia de
carregamento das partidas. - Produzir proposta de experiência e
critérios de aceitação.

Não modificar código, configurações, datasets ou workflows durante M8. A
implementação começa após rever e aprovar os resultados da auditoria.
