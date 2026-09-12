namespace ParceleMais.Errors;

/// <summary>
/// Lançada quando a assinatura de um payload de webhook não é válida (não confere com o segredo
/// esperado, cabeçalho malformado, ou timestamp fora da janela de tolerância de replay).
/// </summary>
public sealed class ParceleMaisWebhookSignatureException : ParceleMaisException
{
    public ParceleMaisWebhookSignatureException(string message) : base(message)
    {
    }
}
