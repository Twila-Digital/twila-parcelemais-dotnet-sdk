using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ParceleMais;
using ParceleMais.Configuration;
using ParceleMais.DependencyInjection;
using ParceleMais.Errors;
using ParceleMais.Simulations.Models;

namespace ParceleMais.Sample.NetFramework
{
    internal static class Program
    {
        private static async Task<int> Main()
        {
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

            using (var provider = services.BuildServiceProvider())
            {
                var client = provider.GetRequiredService<IParceleMaisClient>();

                try
                {
                    Console.WriteLine("Simulando parcelas para R$ 1.500,00...");

                    var installments = await client.Simulations.SimulateInstallmentsAsync(new SimulateInstallmentsRequest(1500.00m));

                    foreach (var installment in installments)
                        Console.WriteLine(string.Format("  {0}x de R$ {1:0.00} (total R$ {2:0.00})", installment.Term, installment.InstallmentAmount, installment.TotalAmount));

                    return 0;
                }
                catch (ParceleMaisAuthenticationException ex)
                {
                    Console.WriteLine("Falha de autenticação — confira ClientId/ClientSecret: " + ex.Message);
                    return 1;
                }
                catch (ParceleMaisApiException ex)
                {
                    Console.WriteLine(string.Format("Erro da API ({0}, {1}): {2}", ex.StatusCode, ex.ErrorCode, ex.Message));
                    return 1;
                }
            }
        }
    }
}
