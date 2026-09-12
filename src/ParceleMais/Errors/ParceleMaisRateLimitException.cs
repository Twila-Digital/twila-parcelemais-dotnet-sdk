using System.Net;

namespace ParceleMais.Errors;

/// <summary>
/// Lançada para <c>429 Too Many Requests</c>.
/// </summary>
public sealed class ParceleMaisRateLimitException : ParceleMaisApiException
{
    public ParceleMaisRateLimitException(string message, ProblemDetailsModel problemDetails, TimeSpan? retryAfter)
        : base(message, (HttpStatusCode)429, problemDetails)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Cabeçalho <c>Retry-After</c> da resposta, quando presente.
    /// </summary>
    public TimeSpan? RetryAfter { get; }
}
