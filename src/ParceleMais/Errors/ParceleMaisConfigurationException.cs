namespace ParceleMais.Errors;

/// <summary>
/// Lançada quando <c>ParceleMaisOptions</c> é inválida. A mensagem identifica o campo, nunca o valor.
/// </summary>
public sealed class ParceleMaisConfigurationException : ParceleMaisException
{
    public ParceleMaisConfigurationException(string message) : base(message)
    {
    }
}
