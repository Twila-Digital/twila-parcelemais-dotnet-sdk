# ParceleMais

SDK .NET oficial para a API do [Parcele +](https://www.cartaosimples.com.br) — crédito e parcelamento no momento da compra.

> Uso restrito a server-side. O `ClientSecret` nunca deve ser embarcado em um app mobile, SPA ou qualquer código que rode no dispositivo do usuário final.

Compatível com `netstandard2.0` (.NET Framework 4.6.2+, .NET Core 2.0+) e `net8.0`.

## Instalação

```bash
dotnet add package ParceleMais
```

## Quickstart

```csharp
services.AddParceleMais(options =>
{
    options.ClientId = "<client-id>";
    options.ClientSecret = "<client-secret>";
    options.Environment = ParceleMaisEnvironment.Staging;
});

var client = provider.GetRequiredService<IParceleMaisClient>();
var parcelas = await client.Simulations.SimulateInstallmentsAsync(new SimulateInstallmentsRequest(1500.00m));
```

`AddParceleMais` registra o `IHttpClientFactory`, autenticação (obtenção e renovação de token), política de retry/circuit breaker (Polly.Core) e o `IParceleMaisClient` singleton.

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
var page = await client.Orders.ListAsync(new ListOrdersRequest { PageNumber = 1, PageSize = 20 });

foreach (var order in page.Items)
    Console.WriteLine(order.Id);

if (page.HasNext)
{
    var next = await client.Orders.ListAsync(new ListOrdersRequest { PageNumber = 2, PageSize = 20 });
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
var evento = ParceleMaisWebhookEvent.Parse(rawJson, signatureHeader, signingSecret);
```

Verifica a assinatura HMAC-SHA256 do cabeçalho e a janela de replay (5 minutos) antes de expor o evento. Lança `ParceleMaisWebhookSignatureException` se a assinatura for inválida ou o evento estiver fora da janela.

## Samples

- `samples/ParceleMais.Sample.Console` — .NET 8, DI standalone
- `samples/ParceleMais.Sample.AspNetCore` — .NET 8, minimal API
- `samples/ParceleMais.Sample.NetFramework` — net472, `ServiceCollection` standalone

## Licença

[MIT](LICENSE)
