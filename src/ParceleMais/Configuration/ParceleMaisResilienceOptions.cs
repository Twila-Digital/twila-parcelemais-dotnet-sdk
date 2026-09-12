namespace ParceleMais.Configuration;

/// <summary>
/// Configuração da pipeline de resiliência (timeout, retry, circuit breaker, idempotência) do client.
/// </summary>
public sealed class ParceleMaisResilienceOptions
{
    /// <summary>
    /// Timeout total de uma chamada lógica, incluindo todas as tentativas de retry. Default: 30s.
    /// </summary>
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Timeout de uma única tentativa HTTP. Default: 10s.
    /// </summary>
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Timeout de uma única tentativa para o endpoint de importação de nota fiscal (payload maior). Default: 60s.
    /// </summary>
    public TimeSpan InvoiceUploadAttemptTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Número máximo de tentativas (incluindo a original) para requisições retryable. Default: 3.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay base do backoff exponencial com jitter entre tentativas. Default: 500ms.
    /// </summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Proporção de falhas na janela de amostragem que abre o circuit breaker. Default: 0.5 (50%).
    /// </summary>
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    /// <summary>
    /// Janela de amostragem do circuit breaker. Default: 30s.
    /// </summary>
    public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Quantidade mínima de chamadas na janela para o circuit breaker considerar abrir. Default: 10.
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Tempo que o circuit breaker permanece aberto antes de testar meia-abertura. Default: 15s.
    /// </summary>
    public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Trata HTTP 500 como transitório (retryable). Default: <see langword="false"/>.
    /// </summary>
    public bool RetryOn500 { get; set; }

    /// <summary>
    /// Desabilita o envio automático de <c>Idempotency-Key</c> em <c>POST /v1/order</c>,
    /// <c>/start-cdc-sale</c>, <c>/invoice</c> e <c>/v1/webhooks</c>. Esses endpoints deixam de ser
    /// retryable automaticamente quando desabilitado. Default: <see langword="false"/>.
    /// </summary>
    public bool DisableAutomaticIdempotencyKey { get; set; }
}
