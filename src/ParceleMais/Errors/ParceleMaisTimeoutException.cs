namespace ParceleMais.Errors;

/// <summary>
/// Lançada em timeout (por tentativa ou total) ou quando o circuit breaker está aberto.
/// </summary>
public sealed class ParceleMaisTimeoutException : ParceleMaisException
{
    public ParceleMaisTimeoutException(string message) : base(message)
    {
    }

    public ParceleMaisTimeoutException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
