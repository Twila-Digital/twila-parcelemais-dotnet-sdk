using Microsoft.Extensions.DependencyInjection;
using ParceleMais;
using ParceleMais.Configuration;
using ParceleMais.DependencyInjection;
using ParceleMais.Errors;
using ParceleMais.Simulations.Models;

var clientId = Environment.GetEnvironmentVariable("PARCELEMAIS_CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("PARCELEMAIS_CLIENT_SECRET");

if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
{
    Console.WriteLine("Defina as variáveis de ambiente PARCELEMAIS_CLIENT_ID e PARCELEMAIS_CLIENT_SECRET (credenciais de staging) antes de rodar este sample.");
    return 1;
}

var services = new ServiceCollection();

services.AddParceleMais(options =>
{
    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
    options.Environment = ParceleMaisEnvironment.Staging;
});

await using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IParceleMaisClient>();

try
{
    Console.WriteLine("Simulando parcelas para R$ 1.500,00...");

    var installments = await client.Simulations.SimulateInstallmentsAsync(new SimulateInstallmentsRequest(1500.00m));

    foreach (var installment in installments)
        Console.WriteLine($"  {installment.Term}x de R$ {installment.InstallmentAmount:0.00} (total R$ {installment.TotalAmount:0.00})");

    Console.WriteLine();
    Console.WriteLine("Simulando o repasse para o mesmo valor, em 12x...");

    var values = await client.Simulations.SimulateValuesAsync(new SimulateValuesRequest(1500.00m, 12));

    Console.WriteLine($"  Valor de venda: R$ {values.SaleAmount:0.00}");
    Console.WriteLine($"  Valor de desembolso: R$ {values.DisbursementAmount:0.00}");
    Console.WriteLine($"  Valor da parcela do cliente: R$ {values.InstallmentAmount:0.00}");

    Console.WriteLine();
    Console.WriteLine("Listando os 10 primeiros clientes...");

    var customers = await client.Customers.ListAsync();

    foreach (var customer in customers.Items)
        Console.WriteLine($"  {customer.Id} — {customer.Name} ({customer.Document})");

    if (customers.Items.Count == 0)
        Console.WriteLine("  (nenhum cliente cadastrado ainda para este parceiro)");

    return 0;
}
catch (ParceleMaisAuthenticationException ex)
{
    Console.WriteLine($"Falha de autenticação — confira ClientId/ClientSecret: {ex.Message}");
    return 1;
}
catch (ParceleMaisApiException ex)
{
    Console.WriteLine($"Erro da API ({ex.StatusCode}, {ex.ErrorCode}): {ex.Message}");
    if (ex.CorrelationId is not null)
        Console.WriteLine($"CorrelationId para suporte: {ex.CorrelationId}");
    return 1;
}
