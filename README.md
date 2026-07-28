# 🚌 MyBusApp

**MyBusApp** é uma aplicação web moderna construída com **Blazor WebAssembly** desenhada para unificar a consulta de transportes públicos. A aplicação permite pesquisar linhas, percursos e horários em tempo real, integrando múltiplas operadoras (Carris Metropolitana e Carris Lisboa) numa interface única e intuitiva.

## 🚀 Tecnologias

* **Frontend:** [Blazor WebAssembly](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) (.NET 8/9)
* **UI/UX:** Bootstrap 5 & Bootstrap Icons
* **Comunicação:** HttpClient para consumo de APIs REST
* **Serialização:** System.Text.Json
* **Linguagem:** C# 12+

## 🏗️ Arquitetura e Design Patterns

O projeto foi desenvolvido seguindo princípios de **Clean Code** e focado na escalabilidade através do **Adapter Pattern**.

### 1. Camada de Domínio (Domain Models)
Localizada em `Models/Domain/`, esta camada define a "Língua Comum" da aplicação.
* `BusLine`: Representação unificada de uma linha.
* `BusDirection`: Sentidos de circulação (Ida/Volta).
* `BusStop`: Paragens físicas.
* `BusArrival`: Previsões de chegada em tempo real.

### 2. Camada de DTOs (Data Transfer Objects)
Localizada em `Models/DTOs/`, contém as classes que representam o JSON bruto de cada API externa. Isto isola as mudanças nas APIs externas do resto da aplicação.

### 3. Camada de Serviços (Abstração)
* **`IBusService`**: Uma interface unificada que define o contrato para qualquer provedor de transporte.
* **`CarrisMetropolitanaService`**: Implementação que consome a API da Carris Metropolitana, gere caches em memória e mapeia os dados para os modelos de domínio.

### 4. Injeção de Dependência Polimórfica
A UI utiliza `IEnumerable<IBusService>`, permitindo que a aplicação suporte novas operadoras (como o Metro ou CP) apenas adicionando uma nova classe de serviço, sem alterar o código das páginas Razor.

## 🛠️ Funcionalidades

- [x] **Pesquisa Inteligente:** Identifica a operadora automaticamente pelo número da linha.
- [x] **Gestão de Cache:** Minimização de chamadas de rede através de cache local de linhas e paragens.
- [x] **Tempo Real:** Diferenciação visual entre horários planeados e tempos reais (Live Broadcast).
- [x] **Filtros Dinâmicos:** Carregamento em cascata (Linha -> Sentido -> Paragem).

## 📂 Estrutura de Pastas

```text
MyBusApp/
├── Models/
│   ├── Domain/             # Modelos unificados (BusLine, etc.)
│   └── DTOs/               # Objetos de transferência de dados (JSON)
│       └── CarrisMetropolitana/   # Classes específicas da Carris Metropolitana
├── Services/
│   ├── IBusService.cs      # Interface comum
│   └── CarrisMetropolitanaService.cs # Implementação do serviço
└── Pages/
    └── Home.razor          # Interface de utilizador agnóstica
```
