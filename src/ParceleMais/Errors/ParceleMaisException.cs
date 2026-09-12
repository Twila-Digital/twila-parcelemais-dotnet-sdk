namespace ParceleMais.Errors;

/// <summary>
/// Exceção base para todos os erros lançados pelo SDK do Parcele+.
/// </summary>
public abstract class ParceleMaisException : Exception
{
    protected ParceleMaisException(string message) : base(message)
    {
    }

    protected ParceleMaisException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
