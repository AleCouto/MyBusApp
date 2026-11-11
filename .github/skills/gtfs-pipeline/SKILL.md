---
name: gtfs-pipeline
description: Gerar, validar e publicar os dados estáticos GTFS do MyBusApp com Tools/GtfsDataGenerator (schema v2, manifest.json, horários acima de 24h, Europe/Lisbon). Usar quando é preciso criar ou atualizar wwwroot/data/<operador>/.
---

# Pipeline GTFS

Converte um feed GTFS num conjunto de JSON versionado que a aplicação consome.
Corre sempre fora do browser.

## Quando usar

- Criar dados para um operador novo que publica GTFS.
- Atualizar os dados de CP, Fertagus ou Carris Lisboa.
- Investigar um erro de dados estáticos em runtime.

## Comandos

A partir da raiz do repositório:

```bash
# Feed remoto (o ZIP é temporário e apagado no fim)
dotnet run --project Tools/GtfsDataGenerator/GtfsDataGenerator.csproj -- \
  --gtfs-url https://exemplo.pt/feed.zip \
  --output wwwroot/data/<operador>

# ZIP já descarregado
dotnet run --project Tools/GtfsDataGenerator/GtfsDataGenerator.csproj -- \
  --gtfs /caminho/para/feed.zip \
  --output wwwroot/data/<operador>
```

`--output` é obrigatório e `--gtfs`/`--gtfs-url` são exclusivos. Códigos de
saída: `0` sucesso, `1` erro, `2` argumentos inválidos.

## Saída

```text
wwwroot/data/<operador>/
├── manifest.json
└── lines/
    ├── 714.<hash>.json
    └── ...
```

O manifesto (schema `version: 2`) tem `generatedAtUtc`, `feedHash` e a lista de
ficheiros por linha. A app carrega o manifesto uma vez por sessão e só o
ficheiro da linha consultada.

## Invariantes

- **Nunca** processar GTFS no browser; o browser só lê o JSON gerado.
- Uma linha lógica é identificada por `route_short_name`. Rotas diferentes com
  o mesmo número são agrupadas; `route_short_name` vazio faz o gerador falhar
  (não se inventa uma identidade).
- `stop_desc` é usado como `GtfsStaticStop.Locality` por convenção destes
  datasets, não por regra geral do GTFS.
- Horas superiores a 24h são preservadas; o fuso de referência é
  `Europe/Lisbon`.
- O ZIP não é versionado; o JSON publicado pode ser.
- Não editar os JSON à mão: regenera-se a partir do feed.

## Validação

1. O comando terminou com código `0`.
2. `manifest.json` tem `version: 2`, `feedHash` e pelo menos uma linha.
3. Abre um ficheiro de `lines/` e confirma sentidos, paragens, viagens e
   horários.
4. Corre os testes (incluem o gerador e os dados estáticos):

```bash
dotnet test MyBusApp.Tests/MyBusApp.Tests.csproj
```

5. No browser, consulta uma linha real e confirma datas e horas.

## Publicação

- Desenvolvimento: gerar diretamente em `wwwroot/data/<operador>/` antes de
  correr a app.
- Produção: publicar o mesmo diretório num alojamento estático ou CDN e apontar
  `Apis:<Operador>:StaticDataBaseUrl` para a URL absoluta, terminada em `/`.
- Cache: `manifest.json` curto ou com revalidação (`no-cache`/ETag); ficheiros
  de linha, que têm hash no nome, longos e imutáveis
  (`public, max-age=31536000, immutable`).
- Publicar os dados **antes** da aplicação que os consome.

## Erros comuns

| Sintoma | Causa provável |
|---|---|
| "route_short_name vazio" | Feed com linhas sem número público; corrigir na origem, não no JSON. |
| "unsupported stop time schema" | Dados gerados com schema antigo; regenerar com a versão atual. |
| 404 num ficheiro de linha | Manifesto e ficheiros publicados em momentos diferentes; republicar o diretório completo. |
| Horas erradas por 1h | Fuso ou mudança de hora; confirmar `Europe/Lisbon` e o calendário. |
| Linha não encontrada | O número público não é o `Id` interno; confirma `route_short_name`. |
