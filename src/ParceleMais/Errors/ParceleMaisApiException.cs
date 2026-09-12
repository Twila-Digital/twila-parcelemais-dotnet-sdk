using System.Net;

namespace ParceleMais.Errors;

/// <summary>
/// Lançada para qualquer resposta de erro HTTP de negócio (400/404/409/422/5xx) da API do Parcele+.
/// </summary>
public class ParceleMaisApiException : ParceleMaisException
{
    public ParceleMaisApiException(
        string message,
        HttpStatusCode statusCode,
        ProblemDetailsModel problemDetails)
        : base(message)
    {
        StatusCode = statusCode;
        ProblemDetails = problemDetails;
    }

    /// <summary>
    /// Código HTTP da resposta.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Campo <c>tipo</c> do ProblemDetails: string opaca, não um enum fechado.
    /// </summary>
    public string? ErrorCode => ProblemDetails.Type;

    /// <summary>
    /// Erros por campo. <see langword="null"/> quando a API não os retornou para este erro.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? Errors => ProblemDetails.Errors;

    /// <summary>
    /// Cabeçalho/campo <c>CorrelationId</c> da resposta, quando presente.
    /// </summary>
    public string? CorrelationId => ProblemDetails.CorrelationId;

    /// <summary>
    /// O <c>ProblemDetails</c> bruto.
    /// </summary>
    public ProblemDetailsModel ProblemDetails { get; }
}
