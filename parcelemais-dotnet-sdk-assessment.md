# Parcele+ .NET SDK — Assessment (Fase 1)

**Status:** Fase 1 de 3 (assessment → plano → implementação). Nenhum código foi escrito ainda.

**Fontes usadas:**
- `https://documentacao.parcelemais.com.br/llms.txt` (índice)
- `https://documentacao.parcelemais.com.br/openapi.yaml` (spec completa, lida integralmente)
- Páginas: `authentication.md`, `webhooks.md`, `development.md`, `flow.md`
- Conhecimento de primeira mão do backend real (`WebIntegration`, mesmo produto por trás de `api.staging.parcelemais.com.br/integration`), adquirido implementando e corrigindo esse backend nesta mesma sessão de trabalho. Onde uso esse conhecimento em vez da documentação pública, isso é **marcado explicitamente** como "observado na implementação" — porque não está no OpenAPI e pode mudar sem aviso.

Convenção de prioridade: **MUST** (bloqueia release 1.0), **SHOULD** (forte recomendação, pode ficar para 1.x), **COULD** (nice-to-have), **WON'T** (decidido não fazer, com justificativa).

---

## 1. Resumo da API atual

A Parcele+ expõe uma API REST (`WebIntegration`) para parceiros venderem CDC (Crédito Direto ao Consumidor) para seus clientes finais. Superfície pequena e focada:

- **Autenticação**: client-credentials (`clientId`/`clientSecret` → Bearer JWT), sem OAuth2 discovery, sem refresh token — é reemissão, não refresh.
- **Pedidos**: criar, consultar (por id / paginado), simular (dois formatos), iniciar venda CDC (gera link de checkout hospedado), importar nota fiscal.
- **Clientes**: consultar por id / paginado (somente leitura — não há criação direta de cliente pela API; o cliente é criado como efeito colateral de `POST /v1/order`).
- **Webhooks**: CRUD completo (create/paged/update/delete), mas só o tipo "Pedido" dispara notificações hoje; "Cliente" e "Simulação" existem no contrato mas não emitem nada (roadmap).

Não há endpoints de: cancelamento de pedido, reembolso, listagem de transações financeiras, ou gestão de estabelecimento/rede — esses existem no backend mas não são expostos na superfície de integração pública. O SDK deve cobrir **exatamente** o que está no OpenAPI, nada mais.

## 2. Inventário completo dos endpoints

| # | Método | Path | Operação | Mutação? | Idempotente? |
|---|---|---|---|---|---|
| 1 | POST | `/v1/authentication/accesstoken` | Gerar token | Não (sem estado de negócio) | Sim (múltiplas chamadas geram tokens distintos mas sem efeito colateral) |
| 2 | GET | `/v1/order/simulate-installments` | Simular parcelas | Não | Sim |
| 3 | GET | `/v1/order/simulate-values` | Simular repasse | Não | Sim |
| 4 | POST | `/v1/order` | Criar pedido | **Sim** | **Não** — sem idempotency key |
| 5 | POST | `/v1/order/start-cdc-sale` | Iniciar venda CDC | **Sim** | **Não** |
| 6 | POST | `/v1/order/invoice` | Importar nota fiscal | **Sim** | **Não** (mas há guarda de negócio: já importado → erro) |
| 7 | GET | `/v1/order/{pedidoId}` | Obter pedido | Não | Sim |
| 8 | GET | `/v1/order/paged` | Listar pedidos | Não | Sim |
| 9 | GET | `/v1/customer/{clienteId}` | Obter cliente | Não | Sim |
| 10 | GET | `/v1/customer/paged` | Listar clientes | Não | Sim |
| 11 | POST | `/v1/webhooks` | Criar webhook | **Sim** | Parcial — `tipo` é único por parceiro, conflito vira `409` (não duplica) |
| 12 | GET | `/v1/webhooks/paged` | Listar webhooks | Não | Sim |
| 13 | PUT | `/v1/webhooks/{tipo}` | Editar webhook | **Sim** | **Sim** (PUT é substituição completa por chave natural `tipo`) |
| 14 | DELETE | `/v1/webhooks/{tipo}` | Excluir webhook | **Sim** | **Sim** (delete por chave natural é naturalmente idempotente — repetir dá 404, não erro de negócio) |

**Nota sobre `estado.` (§4):** o único endpoint verdadeiramente perigoso para retry automático é `POST /v1/order` (cria um pedido de crédito real por trás) e, em menor grau, `POST /v1/order/start-cdc-sale` e `POST /v1/order/invoice`. Isso dirige toda a política de retry (§15).

## 3. Modelo de autenticação

- Grant type: **client credentials** (não é OAuth2 RFC 6749 completo — não há `grant_type` no corpo, é um endpoint proprietário).
- Request: `{ clientId, clientSecret }` → Response: `{ token_de_acesso, expira_em_segundos, expira_em, tipo_de_token, escopo }`.
- `expira_em_segundos` observado como 3600 (1h) no exemplo da doc.
- Todos os endpoints de negócio exigem `Authorization: Bearer <token>` (declarado via `securitySchemes.bearerAuth`, aplicado globalmente exceto no próprio endpoint de geração de token, que tem `security: []`).
- **Não há refresh token** — para renovar, gera-se um novo token do zero com as mesmas credenciais. Isso simplifica o SDK: não existe fluxo de "refresh token expirado" a tratar, só "gerar de novo".
- Identidade do parceiro/rede de lojas é resolvida **pelo próprio token** no backend (claims) — o cliente nunca envia `partnerId`/`establishmentChainId` explicitamente em nenhum request. O SDK não precisa (nem deve) expor esses conceitos como parâmetros.

## 4. Análise do OpenAPI

Pontos relevantes do contrato:

- **Formato**: OpenAPI 3.0.1, gerado a partir de anotações Swashbuckle no backend real (`title: WebIntegration`). Não é escrito à mão — é fonte de verdade viva, mas também **desatualiza com o código** se alguém esquecer de anotar um schema (já vi isso acontecer nesta mesma API).
- **Servers**: só lista o servidor de **staging** (`https://api.staging.parcelemais.com.br/integration`). A URL de produção (`https://api.parcelemais.com.br/integration`) só existe na página `authentication.md`, não no OpenAPI. **Gap real** — ver §5.
- **Erros**: um único schema `Microsoft.AspNetCore.Mvc.ProblemDetails`, mas com **campos em português** (`tipo`, `titulo`, `status`, `detalhe`, `instancia`) — não é RFC 7807 padrão, é uma tradução feita por um middleware do backend. Passei a sessão inteira mexendo nesse middleware; sei por experiência direta que:
  - Erros de validação de negócio (FluentValidation, via `Result.Failure`) preenchem `tipo` com um **código de domínio** (ex.: `"Order.TermOutOfRange"`), e adicionam um campo `errors`/`erros` (array) **não documentado no schema do OpenAPI**.
  - Erros de validação de payload malformado (data annotations do ASP.NET, antes de chegar no FluentValidation) podem preencher `tipo` com uma **URL RFC** genérica (ex.: `https://tools.ietf.org/html/rfc7231#section-6.5.1`) e `detalhe`/`erros` como `null` — isso já foi um bug real que corrigi nesta API, mas não tenho garantia de que não existe em outro endpoint ainda não coberto.
  - **Conclusão prática**: o SDK não pode tratar `tipo` como um enum fechado nem confiar que `erros` sempre existe. Precisa ler o body de forma tolerante (extension data) e nunca quebrar a desserialização por causa de um campo ausente/nulo/inesperado.
- **Sem `operationId` cross-reference com tags de versão** — o OpenAPI não expõe versionamento de API (`ApiVersion("1.0")` existe no código mas não aparece no path nem no schema). Se o backend introduzir `v2`, o path muda de forma não versionada no contrato público atual.
- **Enums como inteiros crus com descrição em texto livre** (`status`, `tipoAutenticacao`, `tipo` de webhook) — não são `enum` nomeados no sentido de gerar tipos C# amigáveis automaticamente sem mapeamento manual. O SDK deve mapear esses inteiros para enums C# nomeados **na camada de fachada**, não expor `int` cru na API pública.
- **`OrderStatus` tem uma inconsistência**: a descrição do filtro `status` em `GET /v1/order/paged` lista valores de `0` a `18`, mas a implementação real do backend tem um 19º valor (`Disbursed = 19`, "Desembolsado") que pode aparecer em `status.valor` de uma resposta mesmo que não apareça como filtro documentado. O SDK deve tratar valores de enum desconhecidos de forma **forward-compatible** (nunca lançar exceção de desserialização por um `int` fora do range conhecido — ver §17).

## 5. Gaps encontrados na API

| Problema | Impacto no SDK | Impacto no cliente | Mudança recomendada | Prioridade | Breaking? |
|---|---|---|---|---|---|
| Sem `Idempotency-Key` em `POST /v1/order`, `/v1/order/start-cdc-sale`, `/v1/order/invoice`, `/v1/webhooks` | SDK não pode retry automático nesses endpoints com segurança | Timeout de rede em `POST /v1/order` deixa o cliente sem saber se o pedido foi criado ou não — só pode consultar `GET /v1/order/paged` por CPF/data para descobrir | Aceitar um header `Idempotency-Key` (guid) em todo POST/PUT mutável; armazenar por N horas e devolver a resposta original em caso de replay | **Alta** | Non-breaking (aditivo) |
| Sem `X-Request-Id`/`X-Correlation-Id`/`traceparent` em nenhuma resposta | SDK não consegue devolver um identificador de suporte junto do erro | Ao abrir chamado de suporte, cliente não tem nada além de "aconteceu às 14:32" para correlacionar | Ecoar (ou gerar) um `X-Request-Id` em toda resposta, incluindo erros | **Alta** | Non-breaking |
| Servidor de produção ausente do OpenAPI | SDK precisa hardcodar a URL de produção a partir da doc em prosa, não do contrato | Nenhum direto, mas gera drift entre SDK e contrato | Adicionar `servers:` com staging + production no OpenAPI | Média | Non-breaking |
| `erros`/`errors` de validação não documentado no schema `ProblemDetails` | Consumo tipado desse campo no SDK é "melhor esforço", não garantido pelo contrato | Cliente não sabe formalmente que pode inspecionar erros de campo | Documentar o schema completo, incluindo o array de erros por campo | Média | Non-breaking |
| Sem rate-limit headers (`X-RateLimit-*`) nem `429` documentado em nenhum endpoint | SDK não sabe se/quando vai ser limitado, não pode informar o cliente de forma proativa | Cliente pode ser bloqueado sem aviso, sem saber o motivo | Documentar limites (se existirem no gateway) e devolver `429` + `Retry-After` de forma consistente | Média | Non-breaking |
| Webhooks sem assinatura (`tipoAutenticacao` só oferece Nenhuma/Basic/JWT estático, configurado pelo próprio parceiro) | SDK não tem nada criptográfico para validar — não existe "verificar assinatura" real | Qualquer um que descubra a URL do webhook (ou que o parceiro tenha escolhido "Nenhuma") pode forjar notificações de pedido aprovado/pago | Adicionar HMAC-SHA256 de um segredo compartilhado sobre o corpo + timestamp, num header tipo `X-ParceleMais-Signature`, com proteção a replay | **Alta** (segurança) | Non-breaking |
| `POST /v1/webhooks` mistura tipos "roadmap" (Cliente, Simulação) com o único tipo funcional (Pedido) no mesmo enum | Cliente pode configurar algo que nunca dispara e achar que está com bug | Confusão / suporte desnecessário | Documentar claramente no response de criação, ou rejeitar `tipo` ainda não ativo com mensagem explícita | Baixa | Non-breaking |
| Inconsistência de formato de erro entre validação de payload malformado e validação de negócio (ver §4) | SDK precisa de parsing tolerante em vez de um único DTO estrito | Mensagens de erro inconsistentes entre bugs parecidos | Garantir que **todo** endpoint passe pelo mesmo pipeline de erro (já em progresso no backend, mas não uniforme em 100% dos endpoints) | Média | Non-breaking |
| `OrderStatus` documentado (0–18) diverge do enum real (inclui 19 = Disbursed) | Enum gerado a partir do OpenAPI ficaria incompleto | Cliente não reconhece um status novo que a API já emite | Sincronizar a descrição do filtro com o enum completo | Baixa | Non-breaking |

## 6. Comparação com SDKs de mercado

| Aspecto | Parcele+ (proposto) | Stripe .NET | Adyen .NET | Azure/AWS SDK |
|---|---|---|---|---|
| Client configuration | `AddParceleMais(options => ...)` + ctor direto p/ Framework | `StripeClient` com `ApiKey` simples (sem OAuth) | `Client` com `ApiKey` ou certificado | `DefaultAzureCredential` / builders complexos |
| DI | Opcional, via `Microsoft.Extensions.DependencyInjection` | Não oficial (comunidade) | Não oficial | Oficial, profundo |
| HttpClient lifecycle | `IHttpClientFactory`, typed client | Cria `HttpClient` próprio internamente (histórico) | Similar à Stripe | `IHttpClientFactory` (SDKs novos) |
| Authentication | Client-credentials → Bearer, gerido internamente | API key estática (sem token a renovar) | API key ou client cert | Managed identity / client-credentials |
| Retry | Extensions.Http.Resilience, só idempotentes | Retry nativo, x-stripe-idempotency-key suportado | Retry configurável | Retry com backoff nativo (`Azure.Core` pipeline) |
| Circuit breaker | Sim (novo p/ esse domínio) | Não expõe | Não expõe | Sim (`Azure.Core`) |
| Idempotency | **Bloqueada pela API** (gap real, ver §5/§9) | Suporte nativo via header, todo POST | Suporte nativo (`idempotencyKey` no request) | N/A (a maioria é idempotente por design) |
| Pagination | Page-number, auto-paginação via `IAsyncEnumerable` | Cursor-based (`starting_after`) + auto-pagination | Offset-based | Continuation token |
| Errors | Exceções tipadas + `ErrorCode` string | Exceções tipadas (`StripeException` + subtipos) | Exceções tipadas | Exceções tipadas por serviço |
| Logging | `ILogger` opcional, sem forçar OTel | `ILogger` opcional | Mínimo | `EventSource`/`ILogger` |
| OpenTelemetry | Via `Activity`/`ActivitySource` nativo (sem dependência extra) | Suporte parcial | Não | Suporte nativo forte |
| Webhooks | Parse de payload (sem verificação de assinatura, pois API não assina) | Verificação de assinatura HMAC nativa (`Webhook.ConstructEvent`) | Verificação HMAC nativa | N/A |
| Framework support | net472+ / netstandard2.0 / net8+ | net472+ | net461+ | Varia; muitos já net6+ only |
| OpenAPI generation | Interno (fachada sobre client gerado), não exposto | Não usa OpenAPI gen (mantido à mão há anos) | Não usa OpenAPI gen | Gerado (`TypeSpec`/swagger interno) |
| Versioning | SemVer estrito | SemVer, mas com "API version" própria da Stripe (data) | SemVer | SemVer por serviço |

**O que eu NÃO copio de propósito:** o padrão de "API version" por data (estilo Stripe, `Stripe-Version: 2024-06-20`) não existe na Parcele+ hoje — não vou inventar um conceito que a API não suporta. O padrão de idempotency key nativo (Stripe/Adyen) eu **recomendo à Parcele+**, mas não posso implementá-lo no SDK client-side sem suporte no servidor (ver §9).

## 7. Matriz de compatibilidade

| TFM | Suporte | Racional |
|---|---|---|
| `net472` | **MUST** (via `netstandard2.0`) | Cliente real em .NET Framework é requisito explícito. `netstandard2.0` é consumível por `net461`+, mas `System.Text.Json`, `Microsoft.Extensions.Http` etc. em Framework têm histórico de fricção com binding redirects — validar com sample real (§27). |
| `netstandard2.0` | **MUST** (baseline) | Maior alcance: Framework 4.6.1+, .NET Core 2.0+, Xamarin, UWP. |
| `net8.0` | **MUST** | LTS atual amplamente adotado. |
| `net10.0` | **SHOULD** (assim que estável) | Vantagem técnica real: `IAsyncEnumerable`, `System.Text.Json` nativo mais rápido, sem pacote extra de polyfill. Só compensa multi-target se houver uso de API exclusiva (ex.: `TimeProvider`, novas otimizações de `HttpClient`) — caso contrário, `net8.0` já serve e reduz matriz de teste. |
| `net462`, `net48` explícitos | **WON'T** dedicado | `netstandard2.0` já cobre. Só criaria mais TFMs para testar sem ganho de API — CoreCLR-only features não existem nesses alvos de qualquer forma. Testamos **contra** `net48` (sample app), mas não compilamos um TFM dedicado pra ele. |

`TargetFrameworks` proposto para o pacote principal:
```xml
<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>
```
Adiciono `net10.0` assim que a API estiver estável e houver uma vantagem concreta validada (não por redundância — item explícito do prompt).

**Package Validation**: habilitar `EnablePackageValidation` desde o primeiro pacote publicado (mesmo em prerelease), com baseline a partir da 1.0.0 assim que sair — impede quebra de superfície pública entre TFMs e entre versões.

## 8. Dependências propostas

| Pacote | Por quê | Alternativa nativa? | TFMs suportados | Impacto no consumidor | Risco em .NET Framework |
|---|---|---|---|---|---|
| `Microsoft.Extensions.Http` | `IHttpClientFactory`, pooling correto de `HttpMessageHandler`, evita socket exhaustion | Não — `HttpClient` cru exigiria reimplementar pooling/lifetime à mão (o erro clássico de "novo HttpClient por request") | netstandard2.0+ | Baixo — pacote extremamente comum, provavelmente já presente em qualquer app ASP.NET Core | Baixo, mas testar binding redirect em Framework |
| `Microsoft.Extensions.Http.Resilience` | Retry/circuit-breaker/timeout maduros (built on `Polly` v8), evita reimplementar backoff+jitter à mão | Não madura o suficiente sem reimplementar Polly | **Requer confirmar TFM mínimo** — versões atuais frequentemente exigem `net8.0`+; se não suportar `netstandard2.0`, uso **Polly v8 (`Microsoft.Extensions.Resilience`/`Polly.Core`) direto**, que tem melhor suporte netstandard2.0, como fallback nesse TFM | A confirmar na Fase 2 antes de fixar a versão | Médio — pacote mais pesado; validar version conflict | Médio — precisa checar caso a caso |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | Permitir `AddParceleMais(IServiceCollection, ...)` sem forçar o container concreto | N/A (é a abstração) | netstandard2.0+ | Nenhum — abstração, não implementação | Baixo |
| `Microsoft.Extensions.Options` | `IOptions<ParceleMaisOptions>`, validação de options no startup | Poderia usar POCO simples sem `IOptions`, mas perderia integração com `IOptionsMonitor`/validação padrão do ecossistema | netstandard2.0+ | Baixo | Baixo |
| `Microsoft.Extensions.Logging.Abstractions` | `ILogger` opcional sem forçar implementação | Poderia expor delegate próprio, mas reinventaria uma abstração que já existe e é universal | netstandard2.0+ | Baixo | Baixo |
| `System.Text.Json` | Serialização — nativo no .NET moderno; em `netstandard2.0` vem como pacote NuGet | `Newtonsoft.Json` — **rejeitado explicitamente**: mais pesado, sem melhoria real de DX aqui, e o prompt já pede para evitar | netstandard2.0 (via pacote), nativo em net8+ | Risco de conflito de versão em apps que já usam `Newtonsoft.Json` — mas não é dependência transitiva nossa, então ok | Médio-baixo — versão do pacote em Framework precisa ser testada (§27) |
| `Microsoft.Bcl.AsyncInterfaces` | `IAsyncEnumerable<T>` em `netstandard2.0` (para auto-paginação) | Sem alternativa — é o polyfill oficial | netstandard2.0 | Baixo, pacote leve e onipresente | Baixo |

**Explicitamente NÃO usado** (e por quê):
- **Refit** — geraria um client HTTP declarativo, mas eu quero controle total sobre o pipeline de auth/retry/erro; Refit adicionaria uma camada de "magia" sobre a qual eu teria que fazer workaround para os handlers customizados de autenticação/retry. Prefiro `HttpClient` tipado + geração de modelos apenas (não de client).
- **RestSharp** — mesma razão, mais um histórico de mudanças de API breaking entre major versions que não quero herdar.
- **Newtonsoft.Json** — ver acima.
- **Polly diretamente (standalone, fora do `Microsoft.Extensions.Http.Resilience`)** — só cai para isso como fallback se `Microsoft.Extensions.Http.Resilience` não suportar `netstandard2.0` (a confirmar na Fase 2); não uso os dois ao mesmo tempo.

## 9. Arquitetura proposta

**Decision:** projeto único `ParceleMais` (mais um pacote fino de DI se necessário) versus fragmentação em vários pacotes desde o dia 1.

**Context:** o prompt em si sugere considerar `ParceleMais.DependencyInjection`, `ParceleMais.AspNetCore`, `ParceleMais.Webhooks` como pacotes separados.

**Options:**
1. Pacote único `ParceleMais`, com DI opcional embutida atrás de `#if`/dependência opcional.
2. `ParceleMais` (core, sem DI) + `ParceleMais.DependencyInjection` (extensão `AddParceleMais`) + `ParceleMais.Webhooks` (parse de payload) desde já.
3. Um único pacote com **todas** as dependências (`Microsoft.Extensions.DependencyInjection` como dependência obrigatória, não opcional).

**Trade-offs:**
- (1) é mais simples de manter e versionar, mas mistura preocupações (core HTTP + DI) — normalmente não é problema real desde que `Microsoft.Extensions.DependencyInjection.Abstractions` seja leve o bastante para não incomodar quem não usa DI (ex.: uma app .NET Framework console, que ainda pode simplesmente ignorar os métodos de extensão).
- (2) é a forma "correta" de longo prazo (cada pacote com seu próprio ciclo de release), mas cedo demais: sem uso real em produção, arriscamos fragmentar em torno de fronteiras erradas e depois ter que fazer breaking change nos pacotes.
- (3) é o pior dos mundos — força DI mesmo em quem não quer.

**Recommendation:** opção (1) para a v1.0 — **um único pacote** `ParceleMais`, com:
- `AddParceleMais(...)` disponível sempre (a dependência em `Microsoft.Extensions.DependencyInjection.Abstractions` é leve e universal o suficiente para não ser um problema, mesmo em Framework).
- `ParceleMaisWebhookEvent.Parse(...)` como namespace `ParceleMais.Webhooks` **dentro do mesmo assembly**, não pacote separado — não há razão técnica para separar algo tão pequeno (parse de JSON) hoje.
- Reavaliar split em pacotes menores **somente se** o pacote ficar pesado por causa de uma dependência específica que a maioria dos consumidores não precisa (ex.: se um dia adicionarmos um pacote `ParceleMais.OpenTelemetry` com exporters específicos — aí sim separar, porque nem todo mundo usa OTel).

Estrutura de camadas dentro do assembly único:

```
Client (fachada pública)
   ↓ usa
Resources (IOrdersClient, ICustomersClient, ISimulationsClient, IWebhooksClient)
   ↓ usam
Internal transport (modelos gerados a partir do OpenAPI + HttpClient tipado)
   ↓ passa por
DelegatingHandlers (Auth → Resilience é gerido pelo próprio HttpClientFactory pipeline)
   ↓
HttpClient (via IHttpClientFactory)
```

Por que fachada sobre modelos gerados, e não expor o gerado diretamente: o OpenAPI é gerado a partir de anotações do backend (Swashbuckle) e **pode mudar nomes de schema** (`Integration.Shared.Responses.Order.OrderIntegrationResponse` é um nome de classe C# interna do backend vazado para o schema name — não é um nome de API estável). Se o SDK expusesse isso diretamente, qualquer refactor interno do backend quebraria o SDK. A fachada isola isso.

## 10. Árvore de projetos

```
ParceleMais.sln

src/
  ParceleMais/
    ParceleMais.csproj                  (netstandard2.0;net8.0)
    ParceleMaisClient.cs                (implementa IParceleMaisClient)
    Configuration/
      ParceleMaisOptions.cs
      ParceleMaisResilienceOptions.cs
      ParceleMaisEnvironment.cs
      ParceleMaisOptionsValidator.cs
    DependencyInjection/
      ServiceCollectionExtensions.cs     (AddParceleMais)
      ParceleMaisClientBuilder.cs        (ConfigureHttpClient / ConfigurePrimaryHttpMessageHandler)
    Authentication/
      IAccessTokenProvider.cs
      AccessTokenProvider.cs             (cache + refresh antecipado + lock)
      AccessToken.cs
      AuthenticationDelegatingHandler.cs
      ITokenApiClient.cs / TokenApiClient.cs   (cliente HTTP isolado, sem passar pelo AuthenticationDelegatingHandler)
    Http/
      HttpClientNames.cs                 (constantes de named clients: "parcelemais.api", "parcelemais.auth")
      RequestBuilder.cs / extensões de URL, query string
    Resilience/
      ResiliencePipelineFactory.cs        (monta o pipeline por endpoint: mutável vs idempotente)
      IdempotencyClassifier.cs            (mapeia método+path → seguro/inseguro para retry)
    Serialization/
      ParceleMaisJsonContext.cs           (ou JsonSerializerOptions central)
      UnknownEnumJsonConverter.cs         (converters para enums forward-compatible)
    Errors/
      ParceleMaisException.cs
      ParceleMaisApiException.cs
      ParceleMaisAuthenticationException.cs
      ParceleMaisValidationException.cs
      ParceleMaisRateLimitException.cs
      ParceleMaisTimeoutException.cs
      ParceleMaisConfigurationException.cs
      ProblemDetailsModel.cs              (modelo interno tolerante)
    Orders/
      IOrdersClient.cs / OrdersClient.cs
      Models/ (CreateOrderRequest, Order, OrderStatus, PagedResult<T>, ...)
    Customers/
      ICustomersClient.cs / CustomersClient.cs
      Models/
    Simulations/
      ISimulationsClient.cs / SimulationsClient.cs
      Models/
    Webhooks/
      IWebhooksClient.cs / WebhooksClient.cs   (CRUD via API)
      ParceleMaisWebhookEvent.cs               (parse de payload recebido)
      Models/
    Internal/
      Generated/                          (saída do gerador a partir do OpenAPI — não editado à mão)
      Mapping/                            (mapeadores Generated → API pública)

tests/
  ParceleMais.UnitTests/
  ParceleMais.IntegrationTests/           (contra staging real, sob flag/skip se sem credenciais)
  ParceleMais.ContractTests/              (compara OpenAPI ao vivo vs modelos gerados)

samples/
  ParceleMais.Sample.Console/             (net8.0)
  ParceleMais.Sample.AspNetCore/          (net8.0, minimal API)
  ParceleMais.Sample.NetFramework/        (net472, console/WinForms simples)
```

## 11. API pública proposta

```csharp
public interface IParceleMaisClient
{
    IOrdersClient Orders { get; }
    ICustomersClient Customers { get; }
    ISimulationsClient Simulations { get; }
    IWebhooksClient Webhooks { get; }
}

public interface IOrdersClient
{
    Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<Order> GetAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<PagedResult<Order>> ListAsync(ListOrdersRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Order> ListAllAsync(ListOrdersRequest request, CancellationToken cancellationToken = default);
    Task<CheckoutLink> StartCdcSaleAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task ImportInvoiceAsync(Guid orderId, InvoiceFile file, CancellationToken cancellationToken = default);
}

public interface ISimulationsClient
{
    Task<IReadOnlyList<InstallmentSimulation>> SimulateInstallmentsAsync(SimulateInstallmentsRequest request, CancellationToken cancellationToken = default);
    Task<ValuesSimulation> SimulateValuesAsync(SimulateValuesRequest request, CancellationToken cancellationToken = default);
}

public interface ICustomersClient
{
    Task<Customer> GetAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<PagedResult<Customer>> ListAsync(ListCustomersRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Customer> ListAllAsync(ListCustomersRequest request, CancellationToken cancellationToken = default);
}

public interface IWebhooksClient
{
    Task CreateAsync(CreateWebhookRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<Webhook>> ListAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task UpdateAsync(WebhookType type, UpdateWebhookRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(WebhookType type, CancellationToken cancellationToken = default);
}
```

Notas de design:
- `InvoiceFile` encapsula a conversão para base64 a partir de `Stream`/`byte[]`/`FileInfo` — o consumidor nunca monta a string base64 manualmente.
- `CheckoutLink` é um tipo dedicado (não `string`), para permitir evoluir (ex.: adicionar `ExpiresAt`) sem breaking change.
- Todos os enums públicos (`OrderStatus`, `WebhookType`, `WebhookAuthenticationType`, `CalculationType`) são `enum` C# nomeados, com um valor `Unknown = -1` (ou padrão) para tolerar valores futuros não mapeados — nunca lança exceção de desserialização por enum desconhecido (ver §17).
- Nenhum tipo de `Internal.Generated` aparece em qualquer assinatura pública.

## 12. Fluxo de autenticação

```
App chama client.Orders.CreateAsync(...)
        ↓
AuthenticationDelegatingHandler.SendAsync
        ↓
token = await _tokenProvider.GetTokenAsync(ct)   // cache hit na maioria das vezes
        ↓
request.Headers.Authorization = Bearer token
        ↓
próximo handler (resiliência) → HttpClient real
```

O `AccessTokenProvider` NUNCA usa o `HttpClient` "autenticado" (o que passa pelo próprio `AuthenticationDelegatingHandler`) para buscar o token — isso criaria uma dependência circular (§14). Ele usa um `ITokenApiClient` ligado a um `HttpClient` nomeado separado (`"parcelemais.auth"`), sem o handler de autenticação, apontando para o mesmo host mas só `POST /v1/authentication/accesstoken`.

## 13. Fluxo de token refresh

**Decision:** como armazenar/renovar o token em memória com segurança sob concorrência.

**Context:** múltiplas requisições concorrentes não podem disparar N chamadas de geração de token; token deve renovar um pouco antes de expirar, não exatamente no segundo da expiração.

**Options:**
1. `lock`/`SemaphoreSlim(1,1)` simples ao redor de "se expirado ou perto de expirar, busca novo".
2. Double-checked locking manual com `Interlocked`.
3. `Lazy<Task<T>>` recriado a cada expiração ("AsyncLazy" pattern).

**Trade-offs:**
- (1) é o mais simples e testável; o custo é que toda thread que chega durante a renovação espera no semáforo (aceitável — a renovação é rápida, ~1 chamada HTTP).
- (2) é mais rápido em tese (evita await no caminho comum), mas exige `volatile`/memory barriers manuais e é fácil errar; ganho de performance é irrelevante aqui (isso não é um hot path de microssegundos).
- (3) (`Lazy<Task<T>>`) é elegante para "computar uma vez", mas se a tarefa falhar, `Lazy` por padrão **cacheia a exceção para sempre** (com `LazyThreadSafetyMode.ExecutionAndPublication` default) — teria que usar `LazyThreadSafetyMode.PublicationOnly` ou reset manual, adicionando complexidade sem benefício sobre (1).

**Recommendation:** (1), com esta forma:

```csharp
private readonly SemaphoreSlim _lock = new(1, 1);
private AccessToken? _cached;

public async Task<string> GetTokenAsync(CancellationToken ct)
{
    var current = _cached;
    if (current is not null && !current.IsCloseToExpiry(_clockSkew))
        return current.Value;

    await _lock.WaitAsync(ct);
    try
    {
        // double-check: outra thread pode já ter renovado enquanto esperávamos
        current = _cached;
        if (current is not null && !current.IsCloseToExpiry(_clockSkew))
            return current.Value;

        var fresh = await _tokenApiClient.GenerateAsync(ct);
        _cached = fresh;
        return fresh.Value;
    }
    finally
    {
        _lock.Release();
    }
}
```

- **Refresh antecipado**: renova quando faltam `clockSkew` (default: 60s, configurável) para expirar — não espera expirar de fato. Isso evita a corrida "token expira exatamente entre a checagem e o envio do request".
- **100 requests simultâneas → 1 refresh**: garantido pelo double-check dentro do semáforo — a primeira thread a entrar busca o token; as demais, ao adquirirem o lock depois, já encontram `_cached` válido e saem sem nova chamada HTTP.
- **Invalidação por 401** (ver §6 do prompt): o `AuthenticationDelegatingHandler`, ao receber 401, chama `_tokenProvider.Invalidate()` (zera `_cached`) e tenta **uma única vez** de novo com um token novo. Um contador de tentativa por request (via `HttpRequestOptions` ou clonagem do request) impede loop infinito caso o 401 persista (ex.: credenciais realmente inválidas) — na segunda falha, propaga `ParceleMaisAuthenticationException`.
- **Nunca loga `clientSecret` nem o token** — só loga "token renovado" / "token inválido, tentando novamente" sem o valor.

## 14. Pipeline de resiliência

Named `HttpClient`s:
- `"parcelemais.auth"` — sem `AuthenticationDelegatingHandler` (evita a circularidade do §14 do prompt). Resiliência própria e mais simples (menos retries — falha de auth não deve mascarar problema de credencial com retries longos).
- `"parcelemais.api"` — `AuthenticationDelegatingHandler` + pipeline de resiliência completo (`Microsoft.Extensions.Http.Resilience`, `AddResilienceHandler`).

Camadas da pipeline (`"parcelemais.api"`), da mais externa para a mais interna:
```
Total Timeout (30s default)
  → Retry (condicional por idempotência — ver §15)
      → Circuit Breaker
          → Attempt Timeout (10s default)
              → HttpClientHandler real
```

Defaults propostos (todos sobrescrevíveis via `ParceleMaisResilienceOptions`):

| Parâmetro | Default | Racional |
|---|---|---|
| Total timeout | 30s | Cobre até 3 tentativas de 10s com folga para backoff; suficiente para simulação/consulta, mas não trava a UI do parceiro por minutos. |
| Attempt timeout | 10s | A maioria dos endpoints (GET, simulate) responde em bem menos que isso; 10s dá folga para picos sem mascarar uma API realmente travada. |
| Attempt timeout (upload de NF) | 60s | Upload de base64 de um PDF é maior payload — tratado como override específico nesse endpoint, não no default global. |
| Retry — tentativas | 3 (1 original + 2 retries) | Padrão de mercado (Stripe/Azure usam 2–3); mais que isso só atrasa a resposta ao cliente final sem ganho real. |
| Retry — backoff | Exponencial + jitter, base 500ms | Evita thundering herd quando muitos clientes retry ao mesmo tempo após uma instabilidade momentânea da API. |
| Circuit breaker — FailureRatio | 0.5 (50%) | Não abre por falhas esporádicas; abre quando metade das chamadas recentes falha — sinal real de degradação. |
| Circuit breaker — SamplingDuration | 30s | Janela curta o bastante para reagir rápido, longa o bastante para não abrir por 2 falhas isoladas. |
| Circuit breaker — MinimumThroughput | 10 | Evita abrir com base em amostra pequena (ex.: 2 chamadas, 1 falhou = 50%, mas não é estatisticamente significativo). |
| Circuit breaker — BreakDuration | 15s | Curto o bastante para tentar half-open rápido; API financeira não deve ficar bloqueada por minutos por engano. |

Todos overridable via:
```csharp
services.AddParceleMais(options => { ... })
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(45))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { ... });
```

## 15. Política de retry por método/endpoint

| Método | Retry automático? | Condição |
|---|---|---|
| GET | **Sim** | Sempre seguro — leitura pura. |
| HEAD/OPTIONS | Sim (não usados por esta API hoje, mas a regra vale se surgirem) | Idem GET. |
| PUT `/v1/webhooks/{tipo}` | **Sim** | Substituição completa por chave natural — repetir dá o mesmo resultado final. |
| DELETE `/v1/webhooks/{tipo}` | **Sim** | Repetir um delete já aplicado dá 404 (tratado como sucesso silencioso pelo SDK, ou erro tipado explícito — decisão de UX na Fase 2). |
| POST `/v1/authentication/accesstoken` | **Sim**, mas com política própria (menos tentativas) | Gerar token não tem efeito colateral de negócio — só desperdício de uma chamada extra em caso de falha real de credencial (não deveria ser mascarado por muitas tentativas). |
| POST `/v1/order` | **Não** por padrão | Sem idempotency key — retry pode criar dois pedidos de crédito para o mesmo cliente. Ver §16. |
| POST `/v1/order/start-cdc-sale` | **Não** por padrão | Mesma razão — pode gerar dois links de checkout / duplo início de venda. |
| POST `/v1/order/invoice` | **Não** por padrão | Há uma guarda de negócio (already-imported), mas não é uma garantia formal de idempotência a nível HTTP — trato como não seguro. |
| POST `/v1/webhooks` | **Não** por padrão (mas erro de "já existe" é tratado como não-transitório, então na prática o efeito de uma duplicata acidental é benigno — 409, não duplicação real) | Ver nota na tabela do §2. |

Erros tratados como transitórios (retryable), independente do método (mas só aplicados quando o método já é retryable pela tabela acima):
```
HttpRequestException (falha de conexão)
408 Request Timeout
429 Too Many Requests   (respeita Retry-After se presente)
502 Bad Gateway
503 Service Unavailable
504 Gateway Timeout
```
`500` **não** é tratado como transitório por padrão — na prática, na API observada, um 500 quase sempre reflete uma exceção não tratada no backend para aquele payload específico (não um problema de infraestrutura), e re-enviar o mesmo payload tende a repetir o mesmo erro. Deixo como opção explícita (`RetryOn500 = false` por padrão) para quem quiser mudar.

## 16. Estratégia de idempotência

**Situação atual da API:** não há `Idempotency-Key` nem qualquer campo equivalente (`requestId`, `externalId`, `correlationId`) em nenhum endpoint mutável do OpenAPI.

**Decisão para o SDK, dado esse cenário:**
1. `POST /v1/order`, `POST /v1/order/start-cdc-sale`, `POST /v1/order/invoice` — **retry automático desabilitado por padrão**, mesmo para erros claramente transitórios (timeout de rede, 503). O SDK expõe a falha ao consumidor como `ParceleMaisTimeoutException`/`ParceleMaisApiException` e deixa a decisão de tentar de novo **explicitamente com o consumidor** (ele decide se quer chamar `CreateAsync` de novo, sabendo do risco).
2. Adiciono uma opção explícita e opt-in `options.AllowUnsafeRetryForMutations = false` (default `false`) para quem, cientes do risco, quiserem habilitar retry mesmo assim — nunca ligado por padrão.
3. **Recomendação formal à API Parcele+** (não implementável só no SDK):
   - Aceitar um header `Idempotency-Key: <guid>` em `POST /v1/order`, `POST /v1/order/start-cdc-sale`, `POST /v1/order/invoice`, `POST /v1/webhooks`.
   - Servidor armazena `(partnerId, endpoint, idempotencyKey) → (statusCode, responseBody)` por uma janela (ex.: 24h), com um índice único.
   - Uma segunda chamada com a mesma chave (dentro da janela) devolve a **resposta original** sem reprocessar (nem criar um segundo pedido).
   - Chave com corpo de request **diferente** do original → `422` (conflito de idempotência), não silenciosamente processa como se fosse nova.
   - Assim que existir, o SDK passa a gerar automaticamente uma `Idempotency-Key` por chamada lógica (não por tentativa de retry — a mesma chave em todas as tentativas de uma mesma chamada) e habilita retry seguro para esses endpoints por padrão.

Prioridade dessa recomendação: **Alta** — é o gap que mais me preocupa nesta API, porque hoje **não existe forma segura** de recuperar automaticamente de uma falha de rede em `POST /v1/order` sem risco de duplicar uma operação de crédito real.

## 17. Estratégia de erros

**Decision:** exceções tipadas vs `Result<T>` vs `ApiResponse<T>`.

**Options:**
1. Exceções tipadas (`ParceleMaisApiException` e subtipos), método assíncrono lança em caso de erro.
2. `Result<T>`/`OneOf<TSuccess, TError>` — sucesso e erro como valores de retorno.
3. `ApiResponse<T>` com `IsSuccess`, `StatusCode`, `Data`, `Error` — meio-termo.

**Trade-offs:**
- `Result<T>` é ótimo dentro de uma aplicação de negócio com controle total do código de chamada (é literalmente o padrão usado no backend Parcele+ que já conheço bem). Mas em uma **biblioteca pública .NET**, forçar `Result<T>` quebra a convenção do ecossistema (`await`, `try/catch`, integração natural com `ILogger`/APM que já correlacionam exceções) e obriga todo consumidor a aprender um padrão específico do SDK antes de escrever a primeira linha. Isso vai contra a diretriz explícita do prompt de "não usar Result Pattern só por preferência arquitetural".
- `ApiResponse<T>` (opção 3) evita exceções para erros esperados, mas na prática vira "meio try/catch, meio if" — a maioria dos consumidores vai `if (!response.IsSuccess) throw` de qualquer jeito, só adiando o problema.
- Exceções tipadas (opção 1) são o padrão dominante em SDKs .NET de mercado (Stripe, Azure, AWS) — familiar, integra nativamente com `try/catch`, com filtros de exceção (`catch (ParceleMaisApiException ex) when (ex.StatusCode == 404)`), e com APM/logging padrão.

**Recommendation:** exceções tipadas.

```
ParceleMaisException                          (base, abstrata)
├── ParceleMaisConfigurationException          (options inválidas — lançada no startup/DI)
├── ParceleMaisAuthenticationException         (401 mesmo após retry único de refresh)
├── ParceleMaisApiException                    (qualquer erro HTTP de negócio — 400/404/409/422/5xx)
│     ├── StatusCode
│     ├── ErrorCode            (o campo `tipo` do ProblemDetails — string opaca, não enum fechado)
│     ├── Errors                (IReadOnlyDictionary<string,string[]>? — quando o campo erros/errors existir)
│     ├── RequestId             (quando existir — ver gap §5)
│     └── ← ProblemDetails bruto acessível via propriedade, para quem precisar de algo que o SDK ainda não modelou
├── ParceleMaisValidationException  : ParceleMaisApiException   (400 com erros de campo)
├── ParceleMaisRateLimitException   : ParceleMaisApiException   (429, expõe RetryAfter)
└── ParceleMaisTimeoutException                (timeout de attempt/total, ou circuit breaker aberto)
```

`ParceleMaisApiException.Message` **nunca** inclui o `Authorization` header nem o `clientSecret`; o corpo de resposta é sanitizado antes de virar `ResponseBodySanitized` (remove qualquer campo que bata com uma lista de nomes sensíveis, defensivamente, mesmo que a API hoje não devolva secrets no corpo).

Desserialização tolerante — **forward-compatibility** (§17 do prompt):
- `JsonSerializerOptions` central com `UnmappedMemberHandling.Skip` (ou equivalente) — propriedade nova da API não quebra o SDK antigo.
- Enums mapeados via converter customizado que devolve um valor `Unknown`/`(OrderStatus)(-1)` em vez de lançar `JsonException` quando o backend adicionar um valor novo (ex.: um 20º status) antes do SDK ser atualizado.
- `DateTimeOffset` para todo campo de data (a API já usa ISO-8601 com offset) — nunca `DateTime` puro (evita ambiguidade de timezone).
- `decimal` para todo valor monetário (nunca `double`/`float`) — a API já usa `number`/`double` no schema, mas na prática são valores monetários; o SDK converte para `decimal` na fachada pública para evitar erro de arredondamento binário em cálculos do lado do consumidor.

## 18. Paginação

A API usa paginação por número de página (`pagina`/`tamanhoPagina`), sem cursor. Resposta sempre com `items` + metadados (`tem_proximo`, `tem_anterior`, `numero`, `tamanho`, `total`).

```csharp
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public bool HasNext { get; }
    public bool HasPrevious { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
}
```

Auto-paginação:
```csharp
public async IAsyncEnumerable<Order> ListAllAsync(
    ListOrdersRequest request,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var page = request.Page ?? 1;
    while (true)
    {
        var result = await ListAsync(request with { Page = page }, cancellationToken);
        foreach (var item in result.Items)
            yield return item;

        if (!result.HasNext)
            yield break;

        page++;
    }
}
```
`IAsyncEnumerable<T>` em `netstandard2.0` via `Microsoft.Bcl.AsyncInterfaces` — dependência única e leve, compensa o ganho de DX de `await foreach` nativo em vez de um `IPageEnumerator` customizado.

## 19. Webhooks

Gestão via API (CRUD) já coberta em `IWebhooksClient` (§11).

Para consumo do payload recebido pelo endpoint do cliente:

```csharp
var orderEvent = ParceleMaisWebhookEvent.Parse(rawJsonBody);
// orderEvent.OrderId, orderEvent.Status (enum), orderEvent.StatusRaw (int, para forward-compat)
```

**Sobre assinatura**: a API **não assina** webhooks hoje (confirmado na doc — autenticação do lado do webhook é Nenhuma/Basic/JWT, configurada pelo próprio parceiro, e serve para o parceiro validar que a chamada tem a credencial que ele mesmo configurou — não é uma assinatura da Parcele+ sobre o conteúdo). Então:
- `ParceleMaisWebhookEvent.Parse` faz **só** desserialização tolerante — não existe `Verify(signature)` porque não há assinatura para verificar.
- Documento isso como **gap de segurança da plataforma**, não do SDK (§5): sem HMAC, qualquer um que conheça (ou adivinhe) a credencial Basic/JWT configurada — ou pior, se o parceiro escolheu "Nenhuma" — pode forjar um evento de "pedido aprovado" para o endpoint do cliente.
- Recomendo à Parcele+: adicionar um segredo compartilhado por parceiro, calcular `HMAC-SHA256(secret, timestamp + "." + body)`, enviar em `X-ParceleMais-Signature: t=<timestamp>,v1=<hmac_hex>`, e o SDK então oferece `ParceleMaisWebhookEvent.Parse(body, signatureHeader, secret)` que:
  - Recalcula o HMAC e compara em tempo constante (`CryptographicOperations.FixedTimeEquals`), evitando timing attack.
  - Rejeita `timestamp` fora de uma janela de tolerância (ex.: 5 minutos), mitigando replay.
- Até isso existir no backend, o SDK **não finge** ter verificação de assinatura — não quero dar falsa sensação de segurança.

## 20. Observabilidade

- `ILogger<T>` opcional injetado via DI (ou `NullLogger` se não configurado, para uso sem DI).
- `ActivitySource` próprio (`"ParceleMais.Sdk"`) usando `System.Diagnostics.DiagnosticSource` nativo — **sem** dependência de pacote OpenTelemetry. Quem já usa OTel no host da aplicação automaticamente coleta essas activities (basta adicionar `"ParceleMais.Sdk"` como source no `TracerProviderBuilder` deles); quem não usa, não paga nenhum custo de dependência.
- Cada `Activity` inclui: método HTTP, "endpoint lógico" (ex.: `"orders.create"`, nunca a URL crua com IDs, para não explodir cardinalidade), status code, duração, número da tentativa (retry), estado do circuit breaker.
- **Nunca logado, em nenhum nível**: `ClientSecret`, o token Bearer completo (só um hash curto ou os últimos 4 caracteres, se necessário para debug), `Authorization` header cru, CPF, payload completo de cliente/pedido, valores financeiros individuais de uma transação. Isso é reforçado com um `[LoggerMessage]`/helper central que já sanitiza antes de qualquer log de request/response ser emitido — não fica a critério de cada chamada individual lembrar de sanitizar.

## 21. Segurança

Threat model resumido:

| Ameaça | Mitigação |
|---|---|
| `ClientSecret` vazado em log | Logging central sanitizado (§20); nunca serializado em `ToString()`/exceção. |
| `ClientSecret`/token em exception message | `ParceleMaisException` nunca inclui os headers de request na mensagem; corpo de resposta sanitizado antes de anexar. |
| Uso client-side (browser/mobile) do SDK, expondo `ClientSecret` | README e XML docs deixam explícito: **uso exclusivamente server-side**; SDK não é anunciado nem empacotado para Blazor WASM/MAUI direto ao público. |
| TLS downgrade | `HttpClient` não segue redirecionamento de HTTPS para HTTP (`AllowAutoRedirect` avaliado; se necessário, handler recusa redirect para esquema não-https). |
| SSRF via BaseUrl customizada | BaseUrl customizada é opt-in explícito (constructor/options avançada), documentada como "para testes/mocks", nunca aceita de input externo não confiável do próprio consumidor. |
| Webhook spoofing / replay | Ver §19 — gap conhecido, mitigação recomendada à API, SDK não finge segurança que não existe. |
| Supply chain (dependências) | Dependency graph mínimo (§8), sem Refit/RestSharp/Newtonsoft; `Directory.Packages.props` com versões centralizadas; Dependabot habilitado no repo. |
| Secrets em exception de configuração | `ParceleMaisConfigurationException` relata *qual campo* está inválido ("ClientSecret não pode ser vazio"), nunca o valor. |

## 22. Testes

Estratégia: todo transporte HTTP é testável via `HttpMessageHandler` fake (sem tocar rede), usando um handler de teste simples (`Func<HttpRequestMessage, HttpResponseMessage>`) — sem framework de mock HTTP pesado adicional como dependência do SDK (mas os testes do próprio repo podem usar `RichardSzalay.MockHttp` ou similar como dependência **só de teste**, não do pacote publicado).

Cobertura mínima (mapeando 1:1 para a lista do prompt, sem repetir aqui): autenticação (geração, cache, renovação antecipada, 100 chamadas concorrentes → 1 refresh, 401 invalida e tenta 1x, segundo 401 não faz loop), retry (503 em GET, Retry-After respeitado, jitter presente, cancellation interrompe, POST não-idempotente não é repetido), circuit breaker (abre, rejeita em aberto, half-open, recupera), serialização (contratos reais capturados do OpenAPI, enum desconhecido, decimal, datas, propriedade desconhecida), erros (400/401/403/404/409/422/429/500/timeout/conexão recusada mapeados para a exceção certa).

## 23. Contract tests

CI baixa o `openapi.yaml` ao vivo (staging) a cada execução (ou em job agendado + no pipeline de release) e:
1. Valida que todo path/schema que o SDK modela ainda existe com o mesmo shape esperado (comparação estrutural, não byte-a-byte, para tolerar adição de campos opcionais sem quebrar).
2. Falha o build se um campo **obrigatório** que o SDK depende for removido/renomeado.
3. Roda contra um subconjunto de exemplos reais do próprio OpenAPI (`example:` de cada schema) desserializando com os modelos do SDK — se um exemplo documentado não desserializa, é um bug do SDK ou uma divergência de contrato, ambos dignos de falhar o build.

## 24. NuGet e Versionamento

SemVer estrito: PATCH = correção sem mudança de contrato; MINOR = novo recurso/endpoint compatível (ex.: API adiciona um campo novo, SDK passa a expor); MAJOR = breaking change real (remoção/renomeação de membro público, mudança de assinatura). Adição de campo pela API **não** justifica MAJOR — é exatamente o caso que a estratégia de forward-compatibility do §17 existe para absorver como MINOR ou nem isso.

Metadados de pacote: `GeneratePackageOnBuild`, `GenerateDocumentationFile`, `IncludeSymbols`+`snupkg`, `Deterministic`+`ContinuousIntegrationBuild` (para SourceLink funcionar), `EnablePackageValidation`, ícone, license (`PackageLicenseExpression` ou arquivo), `PackageProjectUrl`/`RepositoryUrl` apontando para este repo, tags (`parcelemais`, `cdc`, `credito`, `payments`), README embutido no pacote (`PackageReadmeFile`).

## 25. Roadmap (visão geral — plano detalhado fica no documento de Fase 2)

1. **1.0.0-alpha** — transporte HTTP + auth + resiliência + erros, cobrindo Orders/Simulations (o caminho crítico do fluxo de venda).
2. **1.0.0-beta** — Customers + Webhooks (CRUD + parse de evento), paginação/auto-paginação, contract tests no CI.
3. **1.0.0-rc** — sample .NET Framework validado, README completo, package validation, SourceLink.
4. **1.0.0** — GA.
5. **1.x** — assinatura de webhook e `Idempotency-Key` **assim que existirem na API** (dependem da mudança recomendada em §16/§19 — não bloqueiam o 1.0, mas são a próxima prioridade real de segurança/confiabilidade).

---

## Próximo passo

Assim que este assessment for aprovado (ou ajustado), sigo para `parcelemais-dotnet-sdk-implementation-plan.md` (Fase 2) — divisão em fases pequenas de implementação, com objetivo/arquivos/classes/testes/riscos/critérios de aceite por fase, conforme pedido. Nenhum código será escrito antes disso.
