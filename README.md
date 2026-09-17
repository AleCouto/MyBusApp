# 🚌 MyBusApp

**MyBusApp** é uma aplicação web moderna construída com **Blazor WebAssembly** desenhada para unificar a consulta de transportes públicos. A aplicação permite pesquisar linhas, percursos e horários em tempo real, integrando múltiplas operadoras (Carris Metropolitana, Carris Lisboa e Transitland) numa interface única e intuitiva.

## 🚀 Tecnologias

* **Frontend:** [Blazor WebAssembly](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) (.NET 9)
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
* **`CarrisMetropolitanaService`**: Implementação que consome a API da Carris Metropolitana.
* **`CarrisLisboaService`**: Implementação que consome a API da Carris Lisboa.
* **`TransitlandService`**: Implementação que consome a API do Transitland (base de dados GTFS global).

### 4. Injeção de Dependência Polimórfica
A UI utiliza `IEnumerable<IBusService>`, permitindo que a aplicação suporte novas operadoras (como o Metro ou CP) apenas adicionando uma nova classe de serviço, sem alterar o código das páginas Razor.

## 🛠️ Funcionalidades

- [x] **Pesquisa Inteligente:** Identifica a operadora automaticamente pelo número da linha.
- [x] **Gestão de Cache:** Minimização de chamadas de rede através de cache local de linhas, paragens e percursos.
- [x] **Tempo Real:** Diferenciação visual entre horários planeados e tempos reais (Live Broadcast).
- [x] **Filtros Dinâmicos:** Carregamento em cascata (Linha -> Sentido -> Paragem).
- [x] **Filtro por País:** Transitland limitado a Portugal (configurável em `appsettings.json`).
- [x] **Suporte a Horários GTFS:** Parsing de horas noturnas (>24h) para o fuso horário de Lisboa.

## 🌐 APIs Disponíveis

### Carris Metropolitana
- **URL Base:** `https://api.carrismetropolitana.pt/v2/`
- **Documentação:** [api.carrismetropolitana.pt](https://api.carrismetropolitana.pt/)
- **Autenticação:** Pública (sem chave)
- **Dados:** Linhas, rotas, percursos, paragens e chegadas em tempo real
- **Serviço:** `Services/CarrisMetropolitanaService.cs`
- **DTOs:** `Models/DTOs/CarrisMetropolitana/`

### Carris Lisboa ⚠️
- **URL Base:** `https://api.carris.pt/v2.7/`
- **Documentação:** [api.carris.pt](https://api.carris.pt/swagger/)
- **Autenticação:** Pública (sem chave)
- **Estado:** Os dados planeados são publicados como JSON estático por linha; o GTFS ZIP é processado fora do browser
- **Serviço:** `Services/CarrisLisboaService.cs`
- **DTOs:** `Models/DTOs/CarrisLisboa/`

#### Gerar dados estáticos da Carris Lisboa

O gerador offline descarrega o GTFS configurado, ou aceita um ZIP local, e cria um manifesto mais um ficheiro JSON por número de linha. O formato é versionado (`version: 1`) e inclui sentidos, paragens, viagens, horários e regras de `calendar.txt`/`calendar_dates.txt`.

```bash
dotnet run --project Tools/CarrisLisboaDataGenerator/CarrisLisboaDataGenerator.csproj -- \
  --output wwwroot/data/carris-lisboa
```

O comando normal lê `Apis:Carris:GtfsSourceUrl` em `wwwroot/appsettings.json`, descarrega o GTFS para um ficheiro temporário, gera os JSON e apaga o ZIP no fim. Para usar um ZIP já descarregado, indique-o opcionalmente:

```bash
dotnet run --project Tools/CarrisLisboaDataGenerator/CarrisLisboaDataGenerator.csproj -- \
  --gtfs /caminho/para/carris-gtfs.zip \
  --output wwwroot/data/carris-lisboa
```

Publica o diretório gerado exatamente em `wwwroot/data/carris-lisboa/` (ou num alojamento estático/CDN) com esta estrutura:

```text
data/carris-lisboa/
├── manifest.json
└── lines/
    ├── 714.0123abcd4567ef89.json
    └── ...
```

O manifesto contém a versão do formato, `generatedAtUtc`, `feedHash` e os nomes concretos dos ficheiros de linha. O caminho é configurado em `Apis:Carris:StaticDataBaseUrl` e, por defeito, é `data/carris-lisboa/`.

Em desenvolvimento, gere os ficheiros diretamente em `wwwroot/data/carris-lisboa/` antes de iniciar/publicar a aplicação. Em produção, publique o mesmo diretório num CDN ou alojamento estático e configure `StaticDataBaseUrl` com a URL absoluta, terminada em `/`. O manifesto deve ter cache curto ou revalidação (`no-cache`/ETag); os ficheiros de linha com hash podem ter cache longo e imutável (`public, max-age=31536000, immutable`). Os ficheiros gerados devem ser publicados antes da aplicação.

A aplicação descarrega o manifesto uma vez por sessão e apenas o ficheiro da linha consultada. As chegadas da Carris Lisboa são sempre devolvidas como agendadas (`IsRealTime = false`). O ZIP GTFS e os ficheiros estáticos gerados não devem ser versionados.

### Transitland 🗺️
- **URL Base:** `https://transit.land/api/v2/rest/`
- **Documentação:** [transit.land/documentation](https://www.transit.land/documentation/)
- **Autenticação:** API Key obrigatória (gratuita)
- **Dados:** Base de dados GTFS global (horários planeados, não tempo real)

#### Como obter uma API Key do Transitland

1. Aceda a [www.transit.land](https://www.transit.land)
2. Registe uma conta gratuita em [transit.land/signup](https://www.transit.land/signup)
3. Após login, vá a **Dashboard → API Keys** (ou [transit.land/api_keys](https://www.transit.land/api_keys))
4. Crie uma nova chave — terá algo como: `YR...E1`
5. Coloque a chave no ficheiro `wwwroot/appsettings.json`:

```json
"Transitland": {
  "BaseUrl": "https://transit.land/api/v2/rest/",
  "ApiKey": "COLE_A_SUA_CHAVE_AQUI",
  "Country": "PT"
}
```

> ⚠️ **Nunca partilhe a sua API Key publicamente.** O ficheiro `.gitignore` já inclui `appsettings.json`, mas tenha cuidado ao fazer commits.

#### Parâmetro Country

O parâmetro `Country` no `appsettings.json` filtra as rotas do Transitland por país (código ISO 3166-1 alpha-2):
- `"PT"` — Portugal
- `"ES"` — Espanha
- `"BR"` — Brasil
- vazio `""` — todos os países

Pode alterar este valor em:
- **`Configuration/Configuration.cs`** — valor predefinido na classe `ApiEndpoint`
- **`wwwroot/appsettings.json`** — override por ambiente

#### Endpoints Transitland utilizados

| Método | Endpoint | Uso |
|---|---|---|
| `GET` | `/routes?route_number={num}&country=PT` | Pesquisar linha |
| `GET` | `/routes?onestop_id={id}&country=PT` | Obter rota por ID |
| `GET` | `/route_stop_patterns?route_onestop_id={id}&country=PT` | Obter percursos/sentidos |
| `GET` | `/stops?route_stop_pattern_onestop_id={id}` | Obter paragens |
| `GET` | `/schedules?stop_onestop_id={id}&route_onestop_id={id}&date={hoje}` | Obter horários |

## ⚙️ Configuração

Toda a configuração das APIs está centralizada em dois ficheiros:

1. **`Configuration/Configuration.cs`** — Classe `ApiSettings` com as propriedades mapeadas
2. **`wwwroot/appsettings.json`** — Ficheiro JSON com os valores por ambiente

```json
{
  "Apis": {
    "CarrisMetropolitana": {
      "BaseUrl": "https://api.carrismetropolitana.pt/v2/"
    },
    "Carris": {
      "BaseUrl": "https://api.carris.pt/v2.7/",
      "StaticDataBaseUrl": "data/carris-lisboa/",
      "GtfsSourceUrl": "https://gateway.carris.pt/gateway/gtfs/api/v2.11/GTFS"
    },
    "Transitland": {
      "BaseUrl": "https://transit.land/api/v2/rest/",
      "ApiKey": "YRh2Ch5ZC9OZTd4Trly0AnFI30frUv1E",
      "Country": "PT"
    },
    "Cp": {
      "BaseUrl": "https://api.cp.pt/"
    }
  }
}
```

> **Nota:** A chave Transitland incluída no repositório é de desenvolvimento/teste. Para produção, obtenha a sua própria chave gratuita em [transit.land](https://www.transit.land).

## 📂 Estrutura de Pastas

```text
MyBusApp/
├── Configuration/
│   └── Configuration.cs          # Configuração tipada (ApiSettings)
├── Models/
│   ├── Domain/                    # Modelos unificados (BusLine, BusDirection, BusStop, BusArrival)
│   └── DTOs/                     # Objetos de transferência de dados (JSON)
│       ├── CarrisMetropolitana/  # DTOs específicos da Carris Metropolitana
│       ├── CarrisLisboa/         # DTOs específicos da Carris Lisboa
│       └── Transitland/          # DTOs específicos do Transitland
├── Services/
│   ├── IBusService.cs            # Interface comum a todos os provedores
│   ├── CarrisMetropolitanaService.cs
│   ├── CarrisLisboaService.cs
│   └── TransitlandService.cs
├── Pages/
│   └── Home.razor                # Interface de utilizador agnóstica
├── Layout/
│   ├── MainLayout.razor
│   └── NavMenu.razor
├── wwwroot/
│   ├── appsettings.json          # Configuração das APIs
│   └── css/app.css               # Estilos globais
└── Program.cs                    # Registo de serviços e DI
```

## 🔮 Adicionar um Novo Provedor

Para adicionar uma nova operadora (ex: CP, Metro de Lisboa, etc.):

1. Criar DTOs em `Models/DTOs/{NovoProvedor}/`
2. Implementar a interface `IBusService` num novo serviço
3. Registar no `Program.cs`: `builder.Services.AddScoped<IBusService, NovoProvedorService>();`

A UI reage automaticamente — sem necessidade de alterar as páginas Razor.
