# MyBusApp — Plano de Desenvolvimento

> Fonte da verdade do roadmap. Atualizar este documento no fecho de cada milestone.
>
> **Estado:** `[X]` concluído; `[ ]` por realizar; `[~]` em curso; `[!]` bloqueado/dependente de decisão.
>
> Não marcar tarefas como concluídas apenas porque o código compila. Exigir testes e, quando aplicável, validação com dados reais e no browser.

## 1. Objetivo

Construir o MyBusApp como aplicação web multimodal para consultar transportes públicos em Portugal, integrando operadores incrementalmente.

A aplicação deve:
- Consultar serviços, linhas, estações, paragens, destinos e partidas conforme o modo.
- Apresentar previsões em tempo real quando existirem dados válidos.
- Usar horários programados como fallback quando não existirem previsões realtime válidas e houver fonte programada.
- Identificar claramente cada resultado como **Tempo real** ou **Agendado**.
- Nunca inventar previsões nem apresentar dados antigos como atuais.
- Continuar funcional quando uma fonte externa falhar, recorrendo a outra fonte válida quando possível.

## 2. Princípios

- Manter .NET, Blazor WebAssembly, Bootstrap e `System.Text.Json`.
- Implementar e validar um operador de cada vez.
- Consultar `.github/copilot-instructions.md` e as skills relevantes em `.github/skills/` antes de qualquer tarefa.
- Fazer alterações pequenas, incrementais e limitadas ao milestone.
- Preservar serviços existentes que já funcionam.
- Manter contratos e modelos específicos por modo; não criar uma abstração universal de transportes.
- Reutilizar `Tools/GtfsDataGenerator` quando a fonte for GTFS compatível.
- Não assumir que todos os operadores disponibilizam GTFS, API ou realtime.
- Separar aquisição, transformação, publicação e consumo de dados.
- Não processar ZIPs GTFS no browser.
- Não introduzir dependências, factories, repositories, camadas ou abstrações sem necessidade demonstrada.
- Não criar operadores, horários ou previsões fictícias.
- Não fazer stage, commit, reset, checkout ou revert sem autorização explícita.

## 3. Estado conhecido

### 3.1 Operadores

| Operador/fonte | Modo | Estado |
|---|---|---|
| Carris Metropolitana | Autocarros | Integrado; API própria; pesquisa e chegadas validadas manualmente |
| Carris Lisboa | Autocarros | Integrado; GTFS estático convertido para JSON; validado manualmente |
| CP | Comboios | Integrado via `ITrainService`; GTFS estático; validado manualmente |
| Transitland | Fonte/agregador de autocarros | Integração existente; não é operador nem modo |
| Fertagus | Comboios | Por auditar/integrar |
| Metropolitano de Lisboa | Metro | Por auditar/integrar |
| Transtejo/Soflusa | Barcos | Por auditar/integrar |

A lista é um inventário inicial, não confirmação de que cada operador disponibiliza fontes utilizáveis. Oficialidade, licença, frequência, cobertura e formato devem ser verificados.

### 3.2 Infraestrutura existente

- [X] Blazor WebAssembly em .NET 9.
- [X] Modelos comuns `GtfsStatic*`.
- [X] `Tools/GtfsDataGenerator` genérico, com suporte a ZIP e URL.
- [X] Schema estático versão 2 e manifesto.
- [X] Dados CP em `wwwroot/data/cp/`.
- [X] Dados Carris Lisboa em `wwwroot/data/carris-lisboa/`.
- [X] `IBusService` para autocarros.
- [X] `ITrainService` para comboios.
- [X] `BusServiceRouter` limitado ao domínio dos autocarros.
- [X] Composição ferroviária através de DI.
- [X] Experiência contextual para Autocarros, Metro, Comboios e Barcos.
- [X] Testes automatizados para os fluxos integrados.
- [X] Validação manual de Carris Metropolitana (4725 e 3710), Carris Lisboa (714) e CP.
- [X] Correção da filtragem de chegadas da Carris Metropolitana para IDs prefixados.
- [X] Correção da apresentação do número público da carreira da Carris Metropolitana.
- [X] Build e testes reportados como aprovados após as últimas alterações.

**Nota:** Metro e Barcos existem como opções de navegação, mas ainda não têm integrações reais.

### 3.3 Decisões arquiteturais já tomadas

- CP está separada do domínio dos autocarros e implementa `ITrainService`.
- Não criar `TrainServiceRouter`, `TransportServiceRouter` ou interface universal sem necessidade comprovada.
- Transitland permanece uma fonte técnica no contexto dos autocarros.
- O generator GTFS já existe; avaliar e reutilizar, não criar outro.
- Os dados estáticos atuais usam manifestos e JSON preparados para consumo pela aplicação.
- A decisão antiga de adiar GitHub Actions e atualização automática é substituída por este roadmap.

## 4. Política central: realtime com fallback programado

Esta regra deve orientar a integração de cada operador.

### 4.1 Comportamento

1. Consultar a fonte realtime do operador, quando existir.
2. Validar se existem previsões utilizáveis para linha, direção, paragem e período relevante.
3. Se existirem previsões válidas, apresentá-las como **Tempo real**.
4. Se não existirem previsões válidas, recorrer ao horário programado disponível.
5. Apresentar horários programados como **Agendado**.
6. Se nenhuma fonte tiver dados válidos, apresentar indisponibilidade/ausência de partidas.

### 4.2 Regras de integridade

- Nunca identificar horário programado como realtime.
- Nunca apresentar dados realtime expirados como atuais.
- Distinguir falha técnica, resposta vazia, ausência de serviço e falta de previsões.
- Uma falha realtime não deve apagar horários programados válidos.
- Respeitar calendário, exceções, data, fuso horário, paragem, direção e serviço aplicável.
- Preservar horários GTFS superiores a 24 horas.
- Considerar `Europe/Lisbon`, mudanças de hora e exceções de calendário.
- Evitar duplicar partidas ao combinar fontes.
- Se não existir realtime, usar diretamente o horário programado quando disponível.
- Se não existir fonte programada, não inventar fallback.

### 4.3 Três fallbacks diferentes

- **Fallback de previsões:** realtime → horário programado para o mesmo serviço.
- **Fallback entre providers:** tentar outra fonte quando o provider selecionado não resolve a pesquisa.
- **Fallback de distribuição:** manter a última versão estática válida quando uma atualização falha.

São problemas diferentes. Não criar uma abstração única para os três sem necessidade comprovada.

### 4.4 Aceitação por operador

Documentar:
- Fonte programada e fonte realtime, se existirem.
- Relação dos IDs de linha, percurso, direção, paragem e viagem entre fontes.
- Critérios de validade e expiração realtime.
- Condições para ativar fallback.
- Identificação visual da origem do horário.
- Comportamento quando ambas as fontes falham ou não têm partidas.

## 5. Roadmap

A sequência privilegia descoberta das fontes, definição do fallback, automação dos dados e integração incremental. A ordem dos novos operadores pode mudar após a auditoria.

### [ ] M8 — Auditoria das fontes e estratégia por operador

**Objetivo:** substituir suposições por uma matriz verificada de fontes.

- [ ] Consultar documentação oficial da Fertagus.
- [ ] Consultar documentação oficial do Metropolitano de Lisboa.
- [ ] Consultar documentação oficial da Transtejo/Soflusa.
- [ ] Rever as fontes atuais de Carris Metropolitana, Carris Lisboa e CP.
- [ ] Identificar GTFS estático, GTFS-RT, API REST, outras fontes oficiais ou ausência de fonte pública.
- [ ] Confirmar URLs, autenticação, limites, licença/condições de uso e frequência de atualização.
- [ ] Confirmar se os dados podem ser utilizados e redistribuídos.
- [ ] Avaliar cobertura: linhas, viagens, calendário, paragens, acessibilidade e identificadores.
- [ ] Avaliar compatibilidade de cada feed com `Tools/GtfsDataGenerator`.
- [ ] Identificar operadores que exigem API própria em vez de GTFS.
- [ ] Criar matriz por operador: fonte programada, realtime, atualização, riscos e limitações.
- [ ] Recomendar a ordem de integração com base na qualidade das fontes.

**Entregáveis:** matriz com links/evidências, estratégia por operador e lista de bloqueios.

**Critérios:** nenhuma fonte é considerada disponível sem evidência verificável; cada operador tem estratégia proposta para horários programados e realtime.

**Fora do âmbito:** alterar código, criar serviços ou automatizar downloads.

### [ ] M9 — Especificação e validação do fallback realtime → programado

**Objetivo:** garantir comportamento correto sem uniformizar artificialmente serviços diferentes.

- [ ] Auditar o comportamento atual de cada serviço integrado.
- [ ] Identificar como cada serviço representa `IsRealTime` e a origem do horário.
- [ ] Verificar respostas realtime vazias, indisponíveis, inválidas ou expiradas.
- [ ] Definir critérios de validade e expiração por fonte.
- [ ] Definir associação entre previsões realtime e serviços/paragens/viagens programados, quando necessária.
- [ ] Definir regra de fallback por operador com base nas fontes confirmadas.
- [ ] Verificar se os modelos existentes suportam a distinção; alterar apenas se houver lacuna real.
- [ ] Definir indicadores de interface para Tempo real, Agendado e Indisponível.
- [ ] Criar testes para realtime válido, fallback programado, resposta vazia, erro, dados expirados e ausência de serviço.

**Critérios:** comportamento documentado por operador; nenhum horário programado rotulado como realtime; testes cobrem as transições; serviços existentes preservados.

**Nota:** a implementação pode ser faseada por operador. Não criar motor universal de fallback por antecipação.

### [ ] M10 — Pipeline de aquisição e atualização GTFS

**Objetivo:** automatizar obtenção e transformação dos feeds estáticos confirmados em M8.

- [ ] Auditar capacidades e limitações de `Tools/GtfsDataGenerator`.
- [ ] Escolher feeds existentes e estáveis (CP e/ou Carris Lisboa) como pilotos.
- [ ] Definir configuração por operador: URL, nome, opções e destino.
- [ ] Implementar download seguro, timeouts e tratamento de falhas.
- [ ] Executar o generator existente, sem duplicar a lógica.
- [ ] Validar ZIP e ficheiros GTFS obrigatórios antes da geração.
- [ ] Validar integridade referencial dos dados gerados.
- [ ] Validar manifesto, schema e ficheiros de saída.
- [ ] Detetar feeds sem alterações e evitar publicações desnecessárias.
- [ ] Registar logs e resultado por execução.
- [ ] Garantir que uma falha não substitui dados válidos existentes.
- [ ] Documentar execução local e recuperação de falhas.

**Critérios:** pelo menos um feed real processado de ponta a ponta; processo repetível; generator compatível com datasets existentes; temporários excluídos da publicação.

### [ ] M11 — Publicação e distribuição dos dados estáticos

**Objetivo:** disponibilizar datasets processados num local estável para consumo pelo MyBusApp.

- [ ] Comparar GitHub Pages, GitHub Releases, GitHub Raw e alojamento estático externo.
- [ ] Avaliar CORS, cache, limites, tamanho, disponibilidade e manutenção.
- [ ] Escolher repositório/estrutura de publicação.
- [ ] Definir organização por operador e versão.
- [ ] Definir manifesto global ou por operador, preservando compatibilidade do schema.
- [ ] Definir política de cache e atualização do manifesto.
- [ ] Definir publicação atómica: validar primeiro, publicar depois.
- [ ] Manter a última versão válida se uma atualização falhar.
- [ ] Adaptar um serviço piloto para consumir dados publicados fora de `wwwroot`.
- [ ] Testar carregamento, cache, atualização e indisponibilidade da origem.
- [ ] Documentar como adicionar um dataset.

**Critérios:** MyBusApp consome pelo menos um dataset publicado externamente; falhas não removem a última versão válida; CORS/cache funcionam no browser; dados locais só são removidos após validação remota.

### [ ] M12 — Automatização com GitHub Actions

**Objetivo:** executar automaticamente aquisição, transformação, validação e publicação.

- [ ] Criar workflow programado e execução manual.
- [ ] Configurar secrets quando necessários; nunca guardar credenciais em ficheiros versionados.
- [ ] Executar feeds por operador de forma isolada.
- [ ] Executar generator e validações.
- [ ] Publicar apenas datasets aprovados.
- [ ] Evitar commits/publicações sem alterações.
- [ ] Gerar logs e resumo por operador.
- [ ] Tornar falhas de atualização visíveis.
- [ ] Definir frequência conforme a fonte oficial.
- [ ] Testar falhas de rede, feed inválido e erro de publicação.

**Critérios:** workflow manual e programado funcionam; falha num operador não corrompe datasets dos restantes; última versão válida permanece disponível; execução é auditável.

### [ ] M13 — Integração de operadores adicionais

**Objetivo:** integrar operadores individualmente, segundo a estratégia decidida em M8.

Candidatos:
- [ ] Fertagus — Comboios.
- [ ] Metropolitano de Lisboa — Metro.
- [ ] Transtejo/Soflusa — Barcos.

**Ciclo obrigatório para cada operador:**
- [ ] Confirmar fonte oficial e condições de utilização.
- [ ] Confirmar cobertura e qualidade dos dados.
- [ ] Decidir GTFS estático, API própria ou combinação.
- [ ] Implementar/estender processamento estático, se aplicável.
- [ ] Implementar serviço específico e contratos do respetivo modo.
- [ ] Implementar realtime, se existir fonte utilizável.
- [ ] Implementar fallback programado, se existir fonte.
- [ ] Adicionar testes com dados reais ou fixtures fiéis.
- [ ] Validar build e testes.
- [ ] Validar fluxo completo no browser.
- [ ] Confirmar ausência de regressões nos operadores existentes.
- [ ] Atualizar este plano com decisões, limitações e estado.

**Critérios por operador:** fonte documentada; pesquisa contextual funcional; horários coerentes; origem realtime/agendada correta; fallback funcional quando aplicável; build, testes e validação manual aprovados.

A ordem dos operadores será decidida após M8, não assumida antecipadamente.

### [ ] M14 — Consolidação, observabilidade e qualidade

- [ ] Rever logs e tratamento de falhas por serviço.
- [ ] Identificar tempos de carregamento e chamadas redundantes.
- [ ] Rever cache e invalidação por fonte.
- [ ] Rever fusos horários, mudanças de hora e horários GTFS superiores a 24 horas.
- [ ] Rever consistência de IDs entre feeds estáticos e realtime.
- [ ] Rever mensagens de indisponibilidade e ausência de serviço.
- [ ] Rever acessibilidade e responsividade.
- [ ] Executar regressão completa dos operadores integrados.
- [ ] Documentar manutenção e diagnóstico.

**Critérios:** fluxos principais cobertos por testes; falhas externas distinguíveis de ausência real de serviço; dados programados ou antigos nunca apresentados como realtime.

## 6. Ficha obrigatória de decisão por operador

Preencher antes de implementar cada operador:

| Informação | Resultado |
|---|---|
| Operador e modo | Pendente |
| Fonte oficial | Pendente |
| Fonte de horário programado | Pendente |
| Fonte realtime | Pendente |
| Formato dos dados | Pendente |
| Frequência de atualização | Pendente |
| Licença/condições de uso | Pendente |
| Compatibilidade com generator | Pendente |
| Estratégia de fallback | Pendente |
| Identificadores e mapeamentos | Pendente |
| Riscos e limitações | Pendente |
| Decisão de implementação | Pendente |

Não iniciar desenvolvimento enquanto as fontes essenciais e a estratégia de fallback não estiverem compreendidas. Se não existir fonte realtime, o operador pode ser integrado apenas com horários programados, desde que isso fique explícito.

## 7. Checklist de fecho de milestone

- [ ] Consultar instruções e skills relevantes.
- [ ] Confirmar que o trabalho respeitou o âmbito.
- [ ] Rever diff e ficheiros alterados.
- [ ] Confirmar preservação das alterações preexistentes.
- [ ] Adicionar/atualizar testes relevantes.
- [ ] Executar `dotnet test MyBusApp.Tests/MyBusApp.Tests.csproj`.
- [ ] Executar `dotnet build`.
- [ ] Validar dados reais quando aplicável.
- [ ] Validar manualmente no browser quando houver alteração funcional.
- [ ] Confirmar ausência de erros relevantes na consola.
- [ ] Atualizar checkboxes e relatório do milestone.
- [ ] Não fazer stage nem commit sem autorização explícita.

## 8. Próximo passo autorizado

**M8 — Auditoria das fontes e estratégia por operador.**

A primeira execução de M8 é exclusivamente de investigação:
- Ler instruções e skills do projeto.
- Inspecionar implementação e dados existentes.
- Consultar documentação oficial das fontes.
- Produzir matriz comparativa e recomendações.
- Não modificar código, configurações, datasets ou workflows.

A implementação só começa depois de rever e aprovar os resultados da auditoria.
