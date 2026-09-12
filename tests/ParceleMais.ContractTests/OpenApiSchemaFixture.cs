using System.Text.Json;
using Xunit;

namespace ParceleMais.ContractTests;

public sealed class OpenApiSchemaFixture : IAsyncLifetime
{
    private const string DefaultOpenApiUrl = "https://api.staging.parcelemais.com.br/integration/swagger/v1/swagger.json";

    public JsonElement Schemas { get; private set; }

    public async Task InitializeAsync()
    {
        var url = Environment.GetEnvironmentVariable("PARCELEMAIS_CONTRACT_TESTS_OPENAPI_URL") ?? DefaultOpenApiUrl;

        using var httpClient = new HttpClient();
        using var stream = await httpClient.GetStreamAsync(url);
        using var document = await JsonDocument.ParseAsync(stream);

        Schemas = document.RootElement.GetProperty("components").GetProperty("schemas").Clone();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(nameof(OpenApiCollection))]
public sealed class OpenApiCollection : ICollectionFixture<OpenApiSchemaFixture>;
