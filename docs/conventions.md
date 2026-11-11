# Convenções de código

> Verificado: 2026-11-10 @ `5549968`
>
> Regras estáveis de escrita. Não descrevem arquitetura (ver
> [`architecture.md`](architecture.md)) nem semântica de domínio (ver
> [`domain.md`](domain.md)).

## 1. Princípios

1. **Framework-first.** .NET 9, Blazor WebAssembly, Bootstrap 5 e
   `System.Text.Json` resolvem o problema antes de qualquer biblioteca.
2. **Cirúrgico.** Alterar só o necessário. Preservar estrutura, nomes e
   formatação do código em volta; não refatorar métodos que não causam o bug.
3. **Sem abstrações prematuras.** Uma interface só se justifica com duas ou mais
   implementações reais — é o caso de `IBusService` (três) e `ITrainService`
   (duas). Um `switch` para três casos é aceitável.
4. **Configuração em vez de hardcode.** Endpoints e flags vivem em
   `ApiSettings`/`appsettings.json`, não no código.
5. **O código é a verdade.** Se este documento divergir do código, o código
   ganha e este documento é corrigido no mesmo commit.

## 2. C#

- Namespaces file-scoped (`namespace MyBusApp.Services;`).
- `record` para DTOs (com `[property: JsonPropertyName("...")]`) e para modelos
  de domínio imutáveis; `sealed` quando não há herança prevista.
- Nullable e implicit usings já estão ligados no `.csproj`; não os desligues.
- Assíncrono: sufixo `Async`, retorno `Task`/`ValueTask`, nunca `.Result` nem
  `.Wait()`, nunca `async void` fora de event handlers de UI.
- Devolver coleções vazias (`[]`), não `null`, em métodos que devolvem listas.
- Mapear DTO → domínio dentro do serviço; DTOs nunca chegam aos componentes.
- Um tipo por ficheiro; DTOs fortemente relacionados podem partilhar ficheiro
  (como em `Models/DTOs/`), mas não misture domínio com DTO.
- Sem `dynamic`, `ArrayList`, `Newtonsoft.Json` ou `System.Data`.

## 3. Blazor

- `@page` para rotas, `@inject` para dependências; o bloco `@code` fica no fim
  do ficheiro.
- Nenhum componente faz HTTP diretamente: injeta o serviço.
- Componentes que subscrevem eventos implementam `IDisposable` e
  desinscrevem-se.
- `StateHasChanged()` só no thread da UI; não o uses para compensar falta de
  fluxo de estado.
- Não usar constructor injection em componentes.
- JS interop só para o que o C# não alcança (por exemplo `localStorage` e
  `document.visibilityState`); encapsula-o num serviço.
- Páginas grandes existem (`FavoritesDashboard.razor` tem ~800 linhas). Não é o
  modelo: ao mexer numa página com mais de uma responsabilidade, extrai
  componentes em vez de a aumentar.

## 4. UI

- Bootstrap 5 primeiro: grid, utilitários de flex, espaçamento, `btn`, `card`,
  `table`, `form-*`.
- CSS específico em `Componente.razor.css` (scoped). Estilos globais só em
  `wwwroot/css/app.css`, para variáveis e tema.
- Nunca `style="..."` inline.
- Estados obrigatórios: `spinner-border` durante o carregamento, mensagem para
  resultado vazio, alerta para erro, `disabled` durante a operação.
- Acessibilidade: HTML semântico, `role`/`aria-label` nos elementos
  interactivos, contraste WCAG AA (atenção às cores por operador).
- Ícones: emojis decorativos dentro de `<span aria-hidden="true">`, como no
  `NavMenu`. Não uses classes `bi bi-*` nem carregues a fonte do Bootstrap
  Icons. O aspeto varia com o sistema operativo — é uma escolha aceite.
- Sem frameworks de UI externos (MudBlazor, Radzen, Syncfusion, AntDesign,
  Telerik) e sem Tailwind.

## 5. Testes

- xUnit, no projeto `MyBusApp.Tests`; dependências permitidas são as que já
  estão no `.csproj` (test SDK, xUnit, coverlet). A regra de "sem dependências"
  aplica-se ao código de produção.
- Testar comportamento observável, não detalhes de implementação.
- Testes determinísticos e sem rede; usar fixtures para JSON/GTFS.
- Serviços e router: testes unitários. Páginas: o renderer de teste existente,
  sem bUnit. Dados estáticos: fixtures com manifesto e linha.
- Lógica dependente do tempo recebe `TimeProvider` (já registado em DI) em vez
  de ler `DateTime.Now`.

```bash
dotnet test MyBusApp.Tests/MyBusApp.Tests.csproj
```

## 6. Nunca

- Acrescentar um pacote NuGet para funcionalidade que o SDK já tem.
- Reescrever ficheiros ou refatorar código adjacente sem pedido.
- Processar ZIP GTFS no browser.
- Inventar operadores, horários, previsões ou nomes de operador.
- Rotular horário programado como tempo real (ou o inverso).
- Expor segredos em `wwwroot/` — tudo o que lá está é público.
- Introduzir `TrainServiceRouter`, `TransportServiceRouter` ou uma interface
  universal de transportes.
- Fazer `stage`, `commit`, `reset`, `checkout` ou `revert` sem autorização
  explícita.

## 7. Checklist antes de decidir

1. Resolve-se com .NET 9 / Blazor nativo? → faz.
2. Resolve-se com classes Bootstrap? → faz.
3. Resolve-se com um helper simples? → faz.
4. Já existe um padrão no projeto? → segue-o.
5. Todas as respostas foram "não"? → só então considera uma dependência, e
   justifica-a.

## 8. Quando o código e a convenção divergem

Se a mudança foi deliberada, atualiza o documento **no mesmo commit**. Se não
foi, o código é que está errado. Nunca deixes a divergência para depois: é
assim que a documentação começa a mentir.
