# Contribuindo

## Pré-requisitos

- .NET SDK conforme o [`global.json`](global.json)
- (Opcional) Credenciais de staging para rodar os samples/testes de integração manual

## Build e testes

```bash
dotnet restore
dotnet build
dotnet test tests/ParceleMais.UnitTests
```

Os contract tests (`tests/ParceleMais.ContractTests`) fazem uma chamada real ao OpenAPI de staging e não rodam por padrão — veja o workflow `contract-tests.yml`.

## Instalando a partir do código-fonte

Enquanto o pacote não é publicado no NuGet.org, use um `ProjectReference` direto:

```xml
<ItemGroup>
  <ProjectReference Include="../caminho/para/twila-parcelemais-dotnet-sdk/src/ParceleMais/ParceleMais.csproj" />
</ItemGroup>
```

Ou empacote localmente e consuma via feed de arquivo:

```bash
dotnet pack src/ParceleMais/ParceleMais.csproj -c Release -o ./artifacts
dotnet add package ParceleMais --source ./artifacts
```

## Abrindo um PR

1. Crie uma branch a partir de `production`
2. Adicione testes para qualquer mudança de comportamento
3. Rode `dotnet build` e `dotnet test tests/ParceleMais.UnitTests` localmente antes de abrir o PR
4. Abra o PR contra `production` — o CI roda build + testes automaticamente

## Reportando problemas

Abra uma [issue](https://github.com/Twila-Digital/twila-parcelemais-dotnet-sdk/issues) com passos para reproduzir, versão do pacote/TFM e o comportamento esperado vs. observado. Nunca inclua `ClientId`/`ClientSecret` reais no relato.
