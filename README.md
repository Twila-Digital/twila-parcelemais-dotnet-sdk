<p align="center">
  <img src="assets/logo-light.svg" alt="Parcele+" width="180" style="max-width: 100%;">
</p>

<p align="center">
  <a href="LICENSE"><img alt="License" src="https://img.shields.io/github/license/Twila-Digital/twila-parcelemais-dotnet-sdk"></a>
  <a href="https://github.com/Twila-Digital/twila-parcelemais-dotnet-sdk/actions/workflows/ci.yml"><img alt="CI" src="https://github.com/Twila-Digital/twila-parcelemais-dotnet-sdk/actions/workflows/ci.yml/badge.svg"></a>
  <img alt="Target Frameworks" src="https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0-512BD4">
</p>

# ParceleMais

SDK .NET oficial para a API do [Parcele+](https://www.cartaosimples.com.br) — crédito direto ao consumidor (CDC) e parcelamento no momento da compra.

> Uso restrito a server-side. O `ClientSecret` nunca deve ser embarcado em um app mobile, SPA ou qualquer código que rode no dispositivo do usuário final.

## Compatibilidade

Publica dois target frameworks: `netstandard2.0` e `net8.0` (build nativo).

| Runtime | Versões aceitas |
| --- | --- |
| .NET / .NET Core | 2.0 até a versão mais recente (5, 6, 7, 8, 9, 10) |
| .NET Framework | 4.6.2 ou superior |
| Xamarin / Mono / UWP | Qualquer versão compatível com .NET Standard 2.0 |

> .NET Core 1.0/1.1 **não são suportados** — só implementam .NET Standard 1.6.

## Instalação

```bash
dotnet add package ParceleMais
```

> O pacote ainda não foi publicado no NuGet.org — veja [CONTRIBUTING.md](CONTRIBUTING.md) para instalar a partir do código-fonte enquanto isso.

## Quick start

```csharp
using ParceleMais.Configuration;
using ParceleMais.DependencyInjection;

services.AddParceleMais(options =>
{
    options.ClientId = "<client-id>";
    options.ClientSecret = "<client-secret>";
    options.Environment = ParceleMaisEnvironment.Staging;
});
```

`AddParceleMais` registra o `IHttpClientFactory`, autenticação (obtenção e renovação de token), política de retry/circuit breaker (Polly.Core) e o `IParceleMaisClient` singleton.

### Simulando parcelas

```csharp
var client = provider.GetRequiredService<IParceleMaisClient>();

var parcelas = await client.Simulations.SimulateInstallmentsAsync(
    new SimulateInstallmentsRequest(valor: 1500.00m));

foreach (var parcela in parcelas)
    Console.WriteLine($"{parcela.Term}x de {parcela.InstallmentAmount:C} (total {parcela.TotalAmount:C})");
```

Resposta (uma das parcelas simuladas):

```csharp
InstallmentSimulation
{
    Term = 12,
    InstallmentAmount = 145.32m,
    TotalAmount = 1743.84m
}
```

### Criando um pedido

```csharp
var pedidoId = await client.Orders.CreateAsync(new CreateOrderRequest(
    Cpf: "12345678901",
    PhoneNumber: "+5511999998888",
    EstablishmentDocument: "12345678000195",
    RequestedAmount: 1500.00m,
    Name: "Maria Souza",
    Email: "maria.souza@exemplo.com.br",
    DateOfBirth: new DateTimeOffset(1990, 5, 20, 0, 0, 0, TimeSpan.FromHours(-3)),
    Address: new Address(
        Street: "Av. Paulista",
        Number: "1578",
        Neighborhood: "Bela Vista",
        City: "São Paulo",
        State: "SP",
        PostalCode: "01311000")));
```

`CreateAsync` retorna só o `Guid` do pedido (`pedidoId`) — a API não devolve o pedido completo na criação; use `client.Orders.GetAsync(pedidoId)` se precisar dos dados completos logo em seguida.

## Clientes por recurso

| Cliente | Métodos |
| --- | --- |
| `client.Orders` | `CreateAsync`, `GetAsync`, `ListAsync`, `StartCdcSaleAsync`, `ImportInvoiceAsync` |
| `client.Simulations` | `SimulateInstallmentsAsync`, `SimulateValuesAsync` |
| `client.Customers` | `GetAsync`, `ListAsync` |
| `client.Webhooks` | `CreateAsync`, `ListAsync`, `UpdateAsync`, `DeleteAsync` |

## Paginação

`Orders.ListAsync` e `Customers.ListAsync` retornam `PagedResult<T>` — sem auto-paginação; você controla explicitamente o avanço de página:

```csharp
var page = await client.Orders.ListAsync(new ListOrdersRequest(Page: 1, PageSize: 20));

foreach (var order in page.Items)
    Console.WriteLine(order.Id);

if (page.HasNext)
{
    var next = await client.Orders.ListAsync(new ListOrdersRequest(Page: 2, PageSize: 20));
}
```

## Tratamento de erros

| Exceção | Quando |
| --- | --- |
| `ParceleMaisConfigurationException` | `ParceleMaisOptions` inválidas (ex: `ClientId`/`ClientSecret` ausentes) |
| `ParceleMaisAuthenticationException` | Falha ao gerar/renovar o token de acesso |
| `ParceleMaisValidationException` | `400`/`422` — erro de validação, com `Errors` por campo |
| `ParceleMaisRateLimitException` | `429` |
| `ParceleMaisTimeoutException` | Timeout de rede ou do circuit breaker |
| `ParceleMaisApiException` | Qualquer outro erro de API (`404`, `409`, `5xx`) |
| `ParceleMaisWebhookSignatureException` | Assinatura de webhook inválida ou expirada |

```csharp
catch (ParceleMaisApiException ex)
{
    Console.WriteLine($"{ex.StatusCode} {ex.ErrorCode}: {ex.Message}");
}
```

## Validando webhooks

```csharp
using ParceleMais.Webhooks;

var evento = ParceleMaisWebhookEvent.Parse(rawJson, signatureHeader, signingSecret);
```

Verifica a assinatura HMAC-SHA256 do cabeçalho e a janela de replay (5 minutos) antes de expor o evento. Lança `ParceleMaisWebhookSignatureException` se a assinatura for inválida ou o evento estiver fora da janela.

## Samples

- `samples/ParceleMais.Sample.Console` — .NET 8, DI standalone
- `samples/ParceleMais.Sample.AspNetCore` — .NET 8, minimal API
- `samples/ParceleMais.Sample.NetFramework` — net472, `ServiceCollection` standalone

## Documentação completa

[docs.parcelemais.com.br](https://docs.parcelemais.com.br) — referência de todos os endpoints, autenticação, webhooks e mais.

## Contribuindo

Veja [CONTRIBUTING.md](CONTRIBUTING.md).

## Código de conduta

Este projeto segue o [Código de Conduta](CODE_OF_CONDUCT.md).

## Licença

[MIT](LICENSE)
