# 🚌 MyBusApp

**MyBusApp** é uma aplicação web em **Blazor WebAssembly** para consultar rapidamente as próximas partidas dos transportes públicos usados habitualmente. Os favoritos ficam no centro da página inicial; a pesquisa manual continua disponível para consultar outras linhas, paragens, estações e destinos.

Os favoritos são guardados no browser e podem ser reordenados. Para autocarros, cada favorito representa uma linha numa paragem; para comboios, representa uma estação e apresenta partidas agregadas pelos destinos disponíveis. Os horários identificam informação em tempo real quando o provider a fornece e distinguem os restantes como agendados.

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
* `BusArrival`: Previsões de chegada, com indicação de tempo real ou agendado.
* `TrainStation`, `TrainDirection` e `TrainDeparture`: conceitos ferroviários próprios.
* `TransitFavorite`: contexto de linha/paragem ou estação guardado pelo utilizador.

### 2. Camada de DTOs (Data Transfer Objects)
Localizada em `Models/DTOs/`, contém as classes que representam o JSON bruto de cada API externa. Isto isola as mudanças nas APIs externas do resto da aplicação.

### 3. Camada de Serviços (Abstração)
* **`IBusService`**: Interface que define o contrato dos providers de autocarros.
* **`CarrisMetropolitanaService`**: Consome linhas, paragens e chegadas da API da Carris Metropolitana.
* **`CarrisLisboaService`**: Consulta dados GTFS estáticos publicados como JSON por linha.
* **`CpService`**: Consulta horários GTFS estáticos no modo Comboios.
* **`TransitlandService`**: Fonte/agregador de autocarros de último recurso; a cobertura das pesquisas depende dos endpoints disponíveis.
* **`FavoriteStore`**: Persiste os favoritos no `localStorage` do browser.

### 4. Injeção de Dependência Polimórfica
A resolução de autocarros usa `BusServiceRouter` e providers `IBusService`. A CP implementa separadamente `ITrainService`; não é modelada como autocarro. Metro e Barcos estão presentes apenas como modos indisponíveis, sem providers ou horários simulados.

## 🛠️ Funcionalidades

- [x] **Pesquisa Inteligente:** Identifica a operadora automaticamente pelo número da linha.
- [x] **Gestão de Cache:** Minimização de chamadas de rede através de cache local de linhas, paragens e percursos.
- [x] **Favoritos:** Consulta das próximas partidas das linhas/paragens e estações guardadas; permite reordenar e remover.
- [x] **Horários:** Diferenciação visual entre previsões em tempo real e horários agendados.
- [x] **Filtros Dinâmicos:** Carregamento em cascata (Linha -> Sentido -> Paragem).
- [x] **Filtro por País:** Transitland limitado a Portugal (configurável em `appsettings.json`).
- [x] **Suporte a Horários GTFS:** Parsing de horas noturnas (>24h) para o fuso horário de Lisboa.
- [ ] **Metro e Barcos:** Sem providers reais integrados.
- [ ] **Sincronização de favoritos:** Os favoritos permanecem neste browser e não sincronizam entre dispositivos.

## 🌐 APIs Disponíveis

### Carris Metropolitana
- **URL Base:** `https://api.carrismetropolitana.pt/v2/`
- **Documentação:** [api.carrismetropolitana.pt](https://api.carrismetropolitana.pt/)
- **Autenticação:** Pública (sem chave)
- **Dados:** Linhas, rotas, percursos, paragens, previsões e horários de chegada
- **Serviço:** `Services/CarrisMetropolitanaService.cs`
- **DTOs:** `Models/DTOs/CarrisMetropolitana/`

### Carris Lisboa — GTFS estático
- **Dados:** Horários planeados publicados como JSON estático por linha; o GTFS ZIP é processado fora do browser.
- **Serviço:** `Services/CarrisLisboaService.cs`
- **Dados locais:** `wwwroot/data/carris-lisboa/`
- **Estado:** Horários agendados; não são previsões em tempo real.

#### Gerar dados estáticos GTFS

O gerador offline aceita um ZIP GTFS local ou um URL explícito e cria um manifesto mais um ficheiro JSON por número de linha. O formato é versionado (`version: 2`) e inclui sentidos, paragens, viagens, horários e regras de `calendar.txt`/`calendar_dates.txt`.

Para um feed remoto, indique o URL explicitamente. O ZIP é temporário e apagado no fim:

```bash
dotnet run --project Tools/GtfsDataGenerator/GtfsDataGenerator.csproj -- \
  --gtfs-url https://example.org/feed.zip \
  --output wwwroot/data/carris-lisboa
```

Para usar um ZIP já descarregado:

```bash
dotnet run --project Tools/GtfsDataGenerator/GtfsDataGenerator.csproj -- \
  --gtfs /caminho/para/feed.zip \
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

O manifesto contém a versão do formato, `generatedAtUtc`, `feedHash` e os nomes concretos dos ficheiros de linha. O caminho usado pela Carris Lisboa é configurado em `Apis:Carris:StaticDataBaseUrl` e, por defeito, é `data/carris-lisboa/`.

O número público da linha é determinado por `route_short_name`. Rotas diferentes com o mesmo valor são agrupadas na mesma linha lógica; um `route_short_name` vazio faz o generator falhar para evitar uma identidade inventada. Nos feeds suportados, `stop_desc` é usado como `GtfsStaticStop.Locality` por convenção do dataset, não por uma regra geral do GTFS.

Em desenvolvimento, gere os ficheiros diretamente em `wwwroot/data/carris-lisboa/` antes de iniciar/publicar a aplicação. Em produção, publique o mesmo diretório num CDN ou alojamento estático e configure `StaticDataBaseUrl` com a URL absoluta, terminada em `/`. O manifesto deve ter cache curto ou revalidação (`no-cache`/ETag); os ficheiros de linha com hash podem ter cache longo e imutável (`public, max-age=31536000, immutable`). Os ficheiros gerados devem ser publicados antes da aplicação.

A aplicação descarrega o manifesto uma vez por sessão e apenas o ficheiro da linha consultada. As chegadas da Carris Lisboa são sempre devolvidas como agendadas (`IsRealTime = false`). O ZIP GTFS não deve ser versionado; os JSON estáticos publicados podem ser versionados quando fizerem parte dos dados da aplicação.

### CP — GTFS estático
- **Dados:** Manifesto e horários GTFS estáticos versionados em `wwwroot/data/cp/`.
- **Serviço:** `Services/CpService.cs`, no contrato ferroviário `ITrainService`.
- **Consulta:** Operador → estação → destino/sentido → próximas partidas.
- **Estado:** Horários agendados; não há integração realtime CP.

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

> ⚠️ A aplicação é executada no browser. Qualquer chave configurada em `wwwroot/appsettings.json` fica visível ao utilizador e não pode ser tratada como segredo. Use apenas credenciais apropriadas para exposição pública ou uma camada de servidor autorizada.

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
      "ApiKey": "<SUA_API_KEY>",
      "Country": "PT"
    },
    "Cp": {
      "StaticDataBaseUrl": "data/cp/"
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
│   ├── Domain/                    # Modelos Bus/Train e favoritos
│   └── DTOs/                     # Objetos de transferência de dados (JSON)
│       ├── CarrisMetropolitana/  # DTOs específicos da Carris Metropolitana
│       ├── CarrisLisboa/         # DTOs específicos da Carris Lisboa
│       └── Transitland/          # DTOs específicos do Transitland
├── Services/
│   ├── IBusService.cs            # Contrato dos providers de autocarros
│   ├── ITrainService.cs          # Contrato ferroviário
│   ├── FavoriteStore.cs          # Persistência browser dos favoritos
│   ├── BusServiceRouter.cs
│   ├── CarrisMetropolitanaService.cs
│   ├── CarrisLisboaService.cs
│   ├── CpService.cs
│   └── TransitlandService.cs
├── Pages/
│   └── Home.razor                # Pesquisa contextual por modo
├── Components/
│   └── FavoritesDashboard.razor  # Partidas dos favoritos
├── Layout/
│   ├── MainLayout.razor
│   └── NavMenu.razor
├── wwwroot/
│   ├── appsettings.json          # Configuração das APIs
│   └── css/app.css               # Estilos globais
└── Program.cs                    # Registo de serviços e DI
```

## 🔮 Adicionar um Novo Provedor

Para adicionar um provider:

1. Criar DTOs em `Models/DTOs/{NovoProvedor}/`
2. Implementar o contrato específico do modo (`IBusService` ou `ITrainService`) num serviço
3. Registar o serviço no `Program.cs` no modo correspondente.

Um novo provider de autocarros pode ser resolvido pelo router. A experiência de favoritos ou os fluxos de outros modos podem exigir alterações explícitas na interface.
