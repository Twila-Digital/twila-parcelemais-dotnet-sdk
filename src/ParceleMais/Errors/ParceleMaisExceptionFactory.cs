using System.Net;
using System.Text.Json;
using ParceleMais.Serialization;

namespace ParceleMais.Errors;

internal static class ParceleMaisExceptionFactory
{
    public static async Task<ParceleMaisException> FromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problemDetails = await ReadProblemDetailsAsync(response, cancellationToken).ConfigureAwait(false);
        var message = problemDetails.Detail ?? problemDetails.Title ?? $"A API do Parcele+ retornou {(int)response.StatusCode} {response.StatusCode}.";

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new ParceleMaisAuthenticationException(message),
            HttpStatusCode.BadRequest when problemDetails.Errors is { Count: > 0 } => new ParceleMaisValidationException(message, problemDetails),
            (HttpStatusCode)429 => new ParceleMaisRateLimitException(message, problemDetails, GetRetryAfter(response)),
            _ => new ParceleMaisApiException(message, response.StatusCode, problemDetails)
        };
    }

    private static async Task<ProblemDetailsModel> ReadProblemDetailsAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetailsModel>(stream, ParceleMaisJsonOptions.Default, cancellationToken).ConfigureAwait(false);

            return problemDetails ?? ProblemDetailsModel.Empty();
        }
        catch (JsonException)
        {
            return ProblemDetailsModel.Empty();
        }
    }

    private static TimeSpan? GetRetryAfter(HttpResponseMessage response) =>
        response.Headers.RetryAfter?.Delta
            ?? (response.Headers.RetryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : null);
}
