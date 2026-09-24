using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ParceleMais.Internal.Generated;
using ParceleMais.Internal.Generated.Customer;
using ParceleMais.Internal.Generated.Order;
using ParceleMais.Internal.Generated.Simulations;
using ParceleMais.Internal.Generated.WebHook;
using Xunit;

namespace ParceleMais.ContractTests;

[Collection(nameof(OpenApiCollection))]
public sealed class WireContractTests(OpenApiSchemaFixture fixture)
{
    public static IEnumerable<object[]> WireTypes()
    {
        yield return new object[] { typeof(OrderWire), "Integration.Shared.Responses.Order.OrderIntegrationResponse" };
        yield return new object[] { typeof(OrderStatusWire), "Integration.Shared.Responses.Order.StatusIntegrationResponse" };
        yield return new object[] { typeof(IdentifierResponseWire), "Integration.Shared.Responses.Order.CreateCDCOrderResponse" };
        yield return new object[] { typeof(LinkPaymentResponseWire), "Payment.Shared.Responses.LinkPaymentResponse" };
        yield return new object[] { typeof(CreateOrderRequestWire), "WebIntegration.Endpoints.Order.Requests.ValidateCDCCustomerDocumentPhoneRequestContent" };
        yield return new object[] { typeof(AddressWire), "WebIntegration.Endpoints.Order.Requests.AddressRequestContent" };
        yield return new object[] { typeof(ImportOrderInvoiceRequestWire), "WebIntegration.Endpoints.Order.Requests.ImportOrderInvoiceFromBase64Content" };
        yield return new object[] { typeof(StartCdcSaleRequestWire), "WebIntegration.Endpoints.Order.Requests.StartCDCSaleContest" };

        yield return new object[] { typeof(CustomerWire), "Integration.Shared.Responses.Customer.CustomerIntegrationResponse" };
        yield return new object[] { typeof(CustomerAddressWire), "Integration.Shared.Responses.Customer.Address" };

        yield return new object[] { typeof(WebHookWire), "Integration.Shared.Responses.WebHook.WebHookIntegrationResponse" };
        yield return new object[] { typeof(CreateWebHookResponseWire), "WebIntegration.Endpoints.WebHook.Responses.CreateWebHookResponse" };
        yield return new object[] { typeof(CreateWebHookRequestWire), "WebIntegration.Endpoints.WebHook.Requests.CreateWebHookContent" };
        yield return new object[] { typeof(UpdateWebHookRequestWire), "WebIntegration.Endpoints.WebHook.Requests.UpdateWebHookContent" };
        yield return new object[] { typeof(AuditWebHookWire), "Integration.Shared.Responses.WebHook.AuditWebHookIntegrationResponse" };

        yield return new object[] { typeof(PaginaWire), "Integration.Shared.Responses.PaginationIntegrationResponse" };
        yield return new object[] { typeof(PagedResultWire<>), "Integration.Shared.Responses.PagedIntegrationResponse`1[Integration.Shared.Responses.Order.OrderIntegrationResponse]" };
        yield return new object[] { typeof(PagedResultWire<>), "Integration.Shared.Responses.PagedIntegrationResponse`1[Integration.Shared.Responses.Customer.CustomerIntegrationResponse]" };
        yield return new object[] { typeof(PagedResultWire<>), "Integration.Shared.Responses.PagedIntegrationResponse`1[Integration.Shared.Responses.WebHook.AuditWebHookIntegrationResponse]" };

        yield return new object[] { typeof(SimulateInstallmentWire), "Integration.Shared.Responses.Order.ListSimulateInstallmentsSimplifiedIntegrationResponse" };
        yield return new object[] { typeof(EstablishmentSimulationValuesWire), "Integration.Shared.Responses.Order.EstablishmentSimulationValuesIntegrationResponse" };
        yield return new object[] { typeof(CustomerSimulationValuesWire), "Integration.Shared.Responses.Order.CustomerSimulationValuesIntegrationResponse" };
        yield return new object[] { typeof(SimulationValuesWire), "Integration.Shared.Responses.Order.SimulationValuesIntegrationResponse" };
    }

    [Theory]
    [MemberData(nameof(WireTypes))]
    public void PropertyNamesMatchStagingSchema(Type wireType, string schemaName)
    {
        Assert.True(fixture.Schemas.TryGetProperty(schemaName, out var schema), $"Schema '{schemaName}' não encontrado no OpenAPI de staging.");
        Assert.True(schema.TryGetProperty("properties", out var schemaProperties), $"Schema '{schemaName}' não declara 'properties'.");

        var wirePropertyNames = GetJsonPropertyNames(wireType);
        var schemaPropertyNames = schemaProperties.EnumerateObject().Select(p => p.Name).ToHashSet();

        var missingFromSchema = wirePropertyNames.Except(schemaPropertyNames).ToList();
        var missingFromWire = schemaPropertyNames.Except(wirePropertyNames).ToList();

        Assert.True(missingFromSchema.Count == 0,
            $"'{wireType.Name}' declara {string.Join(", ", missingFromSchema)}, ausente(s) no schema '{schemaName}'.");
        Assert.True(missingFromWire.Count == 0,
            $"Schema '{schemaName}' tem {string.Join(", ", missingFromWire)}, não modelado(s) em '{wireType.Name}'.");
    }

    private static HashSet<string> GetJsonPropertyNames(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name)
            .ToHashSet();
}
