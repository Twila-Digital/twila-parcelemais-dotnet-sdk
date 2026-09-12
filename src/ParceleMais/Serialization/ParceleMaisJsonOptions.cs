using System.Text.Json;
using System.Text.Json.Serialization;

namespace ParceleMais.Serialization;

/// <summary>
/// <see cref="JsonSerializerOptions"/> central para (de)serializar requests/responses e o
/// <c>ProblemDetails</c> de erro.
/// </summary>
public static class ParceleMaisJsonOptions
{
    public static JsonSerializerOptions Default { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

        options.Converters.Add(new UnknownEnumJsonConverterFactory());

        return options;
    }
}
