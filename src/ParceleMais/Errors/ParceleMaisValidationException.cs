using System.Net;

namespace ParceleMais.Errors;

/// <summary>
/// Lançada para <c>400 Bad Request</c> com erros de validação de campo.
/// </summary>
public sealed class ParceleMaisValidationException : ParceleMaisApiException
{
    public ParceleMaisValidationException(string message, ProblemDetailsModel problemDetails)
        : base(message, HttpStatusCode.BadRequest, problemDetails)
    {
    }
}
