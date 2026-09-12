namespace ParceleMais.Errors;

/// <summary>
/// Lançada quando a autenticação falha: credenciais inválidas, ou <c>401</c> persistente após uma
/// tentativa de renovação de token.
/// </summary>
public sealed class ParceleMaisAuthenticationException : ParceleMaisException
{
    public ParceleMaisAuthenticationException(string message) : base(message)
    {
    }

    public ParceleMaisAuthenticationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
