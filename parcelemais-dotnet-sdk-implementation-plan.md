# Parcele+ .NET SDK — Plano de implementação (Fase 2)

**Status:** Fase 2 de 3 (assessment → **plano** → implementação). Parte do assessment (Fase 1) já mudou de premissa — ver §0.

## 0. O que mudou desde o assessment

Durante a Fase 1, identifiquei 5 gaps reais na API (`parcelemais-dotnet-sdk-assessment.md`, §5). Antes de iniciar a implementação, todos os 5 foram corrigidos na API real (backend `twila-cartaosimples-backend`, branch `staging`) e na documentação pública:

| Gap do assessment | Status agora | PR |
|---|---|---|
| Sem `Idempotency-Key` em endpoints mutáveis | **Resolvido** — Redis, TTL 24h, lock de 30s, `409` em reuso/concorrência | backend#1444 |
| Sem `correlationId` exposto | **Resolvido** — cabeçalho `CorrelationId` (request/response) + campo `correlationId` em `ProblemDetails` | backend#1444 |
| Servidor de produção ausente do OpenAPI | **Resolvido** — `servers:` agora lista staging + produção | docs#22 |
| `erros`/formato de erro não documentado | **Resolvido** — `ProblemDetails.erros` documentado no schema | docs#22 |
| Webhooks sem assinatura | **Resolvido** — HMAC-SHA256 (`X-ParceleMais-Signature: t=…,v1=…`), `chaveAssinatura` retornada na criação | backend#1445 |

Isso muda decisões da Fase 1 que dependiam desses gaps:

- **§16 do assessment (idempotência)** — não preciso mais do modo "retry desabilitado por padrão com opt-in manual" como estado final. O SDK **gera e envia `Idempotency-Key` automaticamente** em `POST /v1/order`, `/start-cdc-sale`, `/invoice`, `/v1/webhooks`, e habilita retry seguro por padrão nesses endpoints desde o 1.0.0 (não fica para 1.x).
- **§19 (webhooks)** — `ParceleMaisWebhookEvent.Parse` ganha overload com verificação de assinatura desde o 1.0.0, não como item de 1.x.
- **§17 (erros)** — `ParceleMaisApiException.CorrelationId` é populado de forma confiável (antes era "quando existir").
- **§7/§12 (ambiente)** — a URL de produção pode ser resolvida a partir do próprio conhecimento do SDK sobre os dois ambientes nomeados (staging/production), com a mesma confiança que antes só staging tinha.
- Consequência prática: o item 5 do roadmap (§25 do assessment — "assinatura de webhook e Idempotency-Key assim que existirem na API") deixa de ser 1.x e entra no escopo do 1.0.0-beta (fases 3–4 abaixo).

As PRs acima ainda não estão mergeadas em `staging`/`production` dos respectivos repositórios no momento em que este plano é escrito — o SDK é implementado já contra o novo contrato, mas os testes de integração contra staging real (não os testes unitários com `HttpMessageHandler` fake) só rodam de fato depois do merge.

## 1. Ordem das fases e por quê

Cada fase produz algo compilável, testado e revisável isoladamente — nunca um "big bang". A ordem segue dependência técnica real, não a ordem alfabética do assessment:

```
Fase 1: Scaffold + Configuration + Serialization + Errors
Fase 2: Http + Authentication + Resilience
Fase 3: Orders + Simulations (o caminho crítico de venda)
Fase 4: Customers + Webhooks (CRUD + assinatura + parse)
Fase 5: Paginação/auto-paginação (transversal — aplicada em Orders/Customers já na Fase 3/4, testada isoladamente aqui)
Fase 6: Samples (.NET 8 console, ASP.NET Core, .NET Framework 4.7.2)
Fase 7: Contract tests + empacotamento NuGet + CI
```

Fases 1–2 não têm valor de negócio sozinhas (não expõem nenhum recurso), mas **tudo depende delas** — por isso vêm primeiro e são pequenas o bastante para revisar rápido.

---

## Fase 1 — Scaffold, Configuration, Serialization, Errors

**Objetivo:** solução compila, sem nenhum `HttpClient` real ainda, mas com a fundação que todo o resto usa.

**Arquivos/classes:**
- `ParceleMais.sln`, `Directory.Build.props` (TFMs, `Nullable=enable`, `LangVersion=latest`, `TreatWarningsAsErrors` em Release), `Directory.Packages.props` (central package management), `.editorconfig`, `global.json` (pin do SDK).
- `src/ParceleMais/ParceleMais.csproj` — `<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>`.
- `Configuration/ParceleMaisEnvironment.cs` — enum `Staging`/`Production` + `BaseUrl` (ou `Uri` customizada via options avançada).
- `Configuration/ParceleMaisOptions.cs` — `ClientId`, `ClientSecret`, `Environment`, `BaseUrl?` (override explícito).
- `Configuration/ParceleMaisResilienceOptions.cs` — timeouts/retry/circuit breaker (defaults do assessment §14), `AllowUnsafeRetryForMutations` (agora com default reavaliado — ver nota abaixo).
- `Configuration/ParceleMaisOptionsValidator.cs` — `IValidateOptions<ParceleMaisOptions>` (`ClientId`/`ClientSecret` obrigatórios; `BaseUrl` custom exige `Environment` explícito).
- `Errors/ParceleMaisException.cs`, `ParceleMaisConfigurationException.cs`, `ParceleMaisAuthenticationException.cs`, `ParceleMaisApiException.cs`, `ParceleMaisValidationException.cs`, `ParceleMaisRateLimitException.cs`, `ParceleMaisTimeoutException.cs`, `Errors/ProblemDetailsModel.cs` (tolerante — `JsonExtensionData`).
- `Serialization/ParceleMaisJsonOptions.cs` (`JsonSerializerOptions` central, `UnmappedMemberHandling.Skip`), `Serialization/UnknownEnumJsonConverterFactory.cs`, `Serialization/DecimalMoneyConverter.cs` (se necessário — validar se `double`→`decimal` do schema precisa de converter dedicado).

**Nota sobre `AllowUnsafeRetryForMutations`:** como `Idempotency-Key` agora existe (§0), o **default muda de comportamento, não de nome**: com a chave sendo enviada automaticamente, retry em `POST /v1/order` etc. já é seguro por padrão — essa flag passa a controlar apenas o caso extremo de "o consumidor desabilitou explicitamente o envio de Idempotency-Key" (ver Fase 2), não o caminho comum.

**Testes:** `ParceleMaisOptionsValidatorTests` (options inválidas lançam `ParceleMaisConfigurationException` com a mensagem certa, nunca com o secret no texto), `UnknownEnumJsonConverterTests` (valor desconhecido → `Unknown`, não exceção), `ProblemDetailsModelTests` (desserializa os 3 formatos reais observados no assessment §4: erro de negócio com `erros`, erro de payload sem `erros`, e um payload com campo totalmente novo não mapeado).

**Riscos:** `netstandard2.0` + `System.Text.Json` puxa dependência de pacote extra (`System.Text.Json` standalone) — validar que não conflita com uma versão já referenciada pelo host em .NET Framework (mitigado testando no sample da Fase 6, mas um smoke build em `net472` já nesta fase, mesmo sem sample completo, evita descobrir tarde).

**Critério de aceite:** `dotnet build` limpo nos dois TFMs; cobertura de teste das classes acima ≥ 90%; nenhuma classe de `Internal`/`Generated` (ainda não existe, mas o padrão de namespace já é reservado) referenciada por teste externo.

---

## Fase 2 — Http, Authentication, Resilience

**Objetivo:** um `IParceleMaisClient` "vazio" (sem recursos ainda) consegue efetivamente autenticar e fazer uma chamada HTTP de teste contra staging, com retry/circuit breaker funcionando.

**Arquivos/classes:**
- `Http/HttpClientNames.cs` (`"parcelemais.auth"`, `"parcelemais.api"`).
- `Authentication/AccessToken.cs`, `IAccessTokenProvider.cs`, `AccessTokenProvider.cs` (implementação exata do §13 do assessment — `SemaphoreSlim` + double-check + refresh antecipado), `ITokenApiClient.cs`/`TokenApiClient.cs`, `AuthenticationDelegatingHandler.cs` (inclui a lógica de invalidar+retry único em 401, com contador por request via `HttpRequestOptionsKey<int>`).
- `Idempotency/IdempotencyKeyDelegatingHandler.cs` **(novo em relação ao assessment — viabilizado pelo gap resolvido)**: gera um GUID por chamada lógica (não por tentativa de retry — mesma chave em todas as tentativas de uma mesma operação) e anexa `Idempotency-Key` em `POST /v1/order`, `/start-cdc-sale`, `/invoice`, `/v1/webhooks`. A chave é gerada uma vez por invocação do método público (ex.: uma chamada a `CreateAsync`), não recriada a cada tentativa de retry do Polly — isso é o que torna o retry seguro.
- `Resilience/ResiliencePipelineFactory.cs`, `Resilience/IdempotencyClassifier.cs` (tabela do assessment §15, agora com `POST /v1/order` etc. marcados como retry-safe **porque** o `IdempotencyKeyDelegatingHandler` roda antes na pipeline).
- `DependencyInjection/ServiceCollectionExtensions.cs` (`AddParceleMais`).

**Desvio do plano original:** `IParceleMaisClient`/`ParceleMaisClient.cs` (com `Orders`/`Customers`/`Simulations`/`Webhooks`) foi adiado para o início da Fase 3 — essas propriedades são tipadas pelas interfaces de recurso (`IOrdersClient` etc.), que só nascem na Fase 3/4. Criá-las vazias agora só para satisfazer a fachada seria inverter a ordem sem ganho real. Nesta fase, `AddParceleMais` já registra e expõe (via DI) tudo que a fachada vai consumir: `IAccessTokenProvider`, `ITokenApiClient`, os `HttpClient`s nomeados com a pipeline completa (idempotência → resiliência → autenticação).

**Confirmação técnica resolvida:** `Microsoft.Extensions.Http.Resilience` 9.0.0 só declara grupos de dependência para `.NETFramework4.6.2`, `net8.0` e `net9.0` (confirmado via nuspec no NuGet) — não suporta `netstandard2.0`. `Polly.Core` 8.5.0 suporta `netstandard2.0`, `net462`, `net472`, `net6.0` e `net8.0`. Decisão: `ResiliencePipelineFactory` usa **Polly.Core diretamente nos dois TFMs** (não só como fallback do netstandard2.0) — evita manter duas implementações de pipeline por TFM sem ganho real, já que o Polly.Core cobre tudo que o projeto precisa (timeout, retry, circuit breaker) sem o open telemetry/diagnostics extra do pacote `.Http.Resilience`, que o SDK não precisa forçar como dependência. A API pública de `ParceleMaisResilienceOptions` não muda.

**Testes:** autenticação (geração, cache-hit, renovação antecipada, 100 chamadas concorrentes → 1 única chamada HTTP de token — via contador em `HttpMessageHandler` fake, 401 invalida e tenta 1x, segundo 401 não faz loop e lança `ParceleMaisAuthenticationException`), idempotency handler (mesma chave em 3 tentativas de retry de uma mesma chamada lógica; chave diferente entre duas chamadas lógicas distintas), resiliência (retry em 503/GET, não-retry em `POST /v1/order` sem o header — caso o handler de idempotência esteja desabilitado explicitamente, `Retry-After` respeitado em 429, circuit breaker abre/half-open/recupera), cancelamento (`CancellationToken` interrompe uma tentativa em andamento).

**Critério de aceite:** um teste de integração manual (não CI ainda) contra staging real gera um token e recebe `401` esperado ao chamar um endpoint qualquer sem escopo — prova que a pipeline inteira (DNS → TLS → auth → parse de erro) funciona ponta a ponta antes de qualquer recurso de negócio existir.

---

## Fase 3 — Orders + Simulations

**Objetivo:** cobre o caminho crítico de venda (o que justifica a existência do SDK). Primeira fase com valor de negócio real — candidata a **1.0.0-alpha**.

**Arquivos/classes:**
- `Internal/Generated/` — decisão tomada: modelos escritos manualmente (records `internal sealed` com `JsonPropertyName`), espelhando exatamente o `openapi.yaml` real (produção — servidor já presente desde §0), em vez de montar um pipeline de geração (NSwag/Kiota) só para ~10 schemas. Reavaliar geração automática se o número de schemas crescer o bastante para justificar o custo de manter a ferramenta de build.
- `Internal/Mapping/OrderMapper.cs`, `SimulationMapper.cs` — traduz `Generated` → modelos públicos (`Order`, `OrderStatus` enum, `InstallmentSimulation`, `ValuesSimulation`). `Serialization/EnumMapping.cs` reaproveita o mesmo fallback `[UnknownValue]` do conversor JSON (assessment §17) para os enums mapeados manualmente aqui (`OrderStatus`, `CalculationValueType`), não só para os que passam por `System.Text.Json` diretamente.
- `Orders/IOrdersClient.cs`/`OrdersClient.cs`, `Orders/Models/*` (`CreateOrderRequest`, `Order`, `OrderStatus`, `Address`, `InvoiceFile`, `CheckoutLink`).
- `Simulations/ISimulationsClient.cs`/`SimulationsClient.cs`, `Simulations/Models/*`.
- `InvoiceFile` — construtores a partir de `Stream` (sync/async), `byte[]`, caminho de arquivo; converte para base64 internamente (nunca expõe a string base64 na API pública).
- `IParceleMaisClient`/`ParceleMaisClient.cs` (adiados da Fase 2) criados agora, já com `Orders`/`Simulations` reais — `Customers`/`Webhooks` entram como propriedades na Fase 4.

**Desvio do plano original:** `IOrdersClient.CreateAsync` retorna `Guid` (o id do pedido), não `Order` completo. `POST /v1/order` só devolve `{ pedidoId }` — fazer um `GET` adicional escondido dentro de `CreateAsync` para montar um `Order` completo esconderia uma segunda chamada de rede e criaria uma falha "fantasma" pós-criação bem-sucedida (a criação funcionou, mas o SDK lançaria por causa do `GET` de confirmação). Mais correto e honesto o consumidor decidir se quer buscar o pedido via `GetAsync(id)` depois.

**Testes:** contrato de request/response real (fixtures capturadas do `example:` do OpenAPI), `OrderStatus` desconhecido (ex.: um 99º valor simulado) não quebra, `CreateAsync` envia `Idempotency-Key` (verificado via handler de teste inspecionando o header), `ImportInvoiceAsync` aceita os 3 construtores de `InvoiceFile` e produz o mesmo base64 nos 3 casos, `ListAllAsync` percorre páginas automaticamente, `SimulateValuesAsync` sempre envia `modeloJuros=1` (único valor disponível hoje).

**Critério de aceite:** um console de teste manual (ainda não o sample oficial da Fase 6) consegue: gerar token → simular parcelas → criar pedido → consultar o pedido criado, contra staging real.

---

## Fase 4 — Customers + Webhooks

**Objetivo:** completa a superfície pública do OpenAPI. Candidata a **1.0.0-beta**.

**Arquivos/classes:**
- `Customers/ICustomersClient.cs`/`CustomersClient.cs`, `Customers/Models/*`.
- `Webhooks/IWebhooksClient.cs`/`WebhooksClient.cs` — CRUD via API, `CreateAsync` retorna `CreateWebhookResult` (novo tipo — não existia no assessment porque a API não devolvia nada relevante; agora carrega `SigningSecret`, exibido uma única vez, refletindo o `chaveAssinatura` do backend#1445).
- `Webhooks/ParceleMaisWebhookEvent.cs` — `Parse(rawJson)` (sem verificação, mantido para quem ainda não configurou segredo) **e** `Parse(rawJson, signatureHeader, signingSecret)` (novo — recalcula HMAC-SHA256 sobre `{timestamp}.{body}`, compara com `CryptographicOperations.FixedTimeEquals`, valida janela de tolerância de replay de 5 minutos, lança `ParceleMaisWebhookSignatureException` se inválido).

**Testes:** paginação de clientes/webhooks, `WebhooksClient.CreateAsync` expõe o `SigningSecret` só na criação (não tenta buscá-lo de novo em nenhum outro método — nem existe onde buscar, a API também não devolve depois), `ParceleMaisWebhookEvent.Parse` com assinatura: válida, inválida (byte alterado), expirada (timestamp fora da janela), header ausente (fallback para parse sem verificação, com aviso via `ILogger`, nunca silencioso).

**Critério de aceite:** um teste unitário reproduz exatamente o HMAC de um payload fixo com um segredo fixo e compara com um valor calculado independentemente (ex.: via um script Python de referência) — garante que a implementação do SDK é *byte-compatível* com a do backend, não só "parece certo".

---

## Fase 5 — Paginação (endurecimento transversal)

**Objetivo:** `ListAllAsync` (Orders e Customers) testado isoladamente, já que a Fase 3/4 só testa `ListAsync` página a página.

**Arquivos/classes:** nenhum novo — só testes adicionais sobre o que já existe (`ListAllAsync` em `OrdersClient`/`CustomersClient`, implementado desde a Fase 3/4 conforme assessment §18).

**Testes:** 3 páginas simuladas via handler fake → 1 `IAsyncEnumerable` com todos os itens na ordem certa; cancelamento no meio da segunda página para a enumeração sem buscar a terceira; página vazia não trava em loop.

**Critério de aceite:** cobertura de `ListAllAsync` ≥ 95% (é pouco código, mas com várias bordas).

---

## Fase 6 — Samples

**Objetivo:** valida a experiência real de consumo nos 3 perfis de cliente que o assessment define como obrigatórios (§7).

**Arquivos:** `samples/ParceleMais.Sample.Console` (net8.0), `samples/ParceleMais.Sample.AspNetCore` (net8.0, minimal API + `AddParceleMais` via DI), `samples/ParceleMais.Sample.NetFramework` (net472, console, sem DI — construção direta do client).

**Critério de aceite:** os 3 samples compilam e rodam contra staging com credenciais reais de teste; o sample .NET Framework é o que valida (ou refuta) o risco de binding redirect do assessment §7/§27 — se houver problema, é descoberto aqui, antes do release, não depois de um consumidor real reportar.

---

## Fase 7 — Contract tests, empacotamento, CI

**Objetivo:** release engineering. Sem código de produto novo.

**Itens:**
- `tests/ParceleMais.ContractTests` (assessment §23) — busca `openapi.yaml` de staging (agora com `servers:` completo) no pipeline, compara contra os modelos gerados na Fase 3/4.
- Metadados de pacote completos (assessment §24): `PackageReadmeFile`, ícone, `PackageLicenseExpression`, SourceLink (`Deterministic`+`ContinuousIntegrationBuild`), `IncludeSymbols`+`snupkg`, `EnablePackageValidation` com baseline a partir de `1.0.0-alpha`.
- CI (GitHub Actions): build+test nos dois TFMs a cada PR; contract tests em job agendado (diário) + obrigatório no pipeline de release; publish no NuGet.org só em tag `v*` após os dois anteriores passarem.
- README (Developer Experience, conforme pedido no prompt original): quickstart em < 10 linhas, tabela de exceções, exemplo de auto-paginação, exemplo de verificação de webhook, nota de "server-side only".

**Critério de aceite:** `dotnet pack` produz um `.nupkg` que passa `dotnet-validate package local` sem warnings; um projeto de teste separado (fora da solution) instala o pacote local via feed de arquivo e consome `IOrdersClient` com sucesso — prova que o pacote empacotado (não só o projeto em build) funciona.

---

## Rastreamento de riscos abertos (carregados do assessment, com dono e fase de resolução)

| Risco | Fase que resolve | Como |
|---|---|---|
| `Microsoft.Extensions.Http.Resilience` pode não suportar `netstandard2.0` | Fase 2 (início) | Spike de 30 min antes de escrever `ResiliencePipelineFactory`; fallback documentado (§8 do assessment) já decidido, só falta confirmar se é necessário. |
| Binding redirect / fricção em .NET Framework real | Fase 6 | Sample `net472` real, não só compilação do pacote em `netstandard2.0`. |
| Geração de modelos a partir do OpenAPI pode vazar nomes internos do backend | Fase 3 (mapeamento) | Nenhum tipo de `Internal.Generated` sai da fachada — reforçado por teste de arquitetura (`ArchUnitNET`/teste de reflexão simples verificando que nenhum tipo público do assembly pertence ao namespace `Internal`). |
| PRs backend#1444/#1445/docs#22 ainda não mergeadas | Fase 2–4 (implementação já assume o novo contrato; testes de integração real ficam bloqueados até o merge) | Testes unitários (handler fake) não dependem do merge; só o teste de integração manual contra staging real depende. |

## Próximo passo

Início da **Fase 1** (scaffold + configuration + serialization + errors) nesta mesma sessão, assim que este plano for aceito. Cada fase seguinte é apresentada para revisão antes de mergear (mesmo processo desta sessão com o backend: branch por fase, PR, build verde).
