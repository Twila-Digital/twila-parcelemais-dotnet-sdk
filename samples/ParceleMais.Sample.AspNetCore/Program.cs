using ParceleMais;
using ParceleMais.Configuration;
using ParceleMais.DependencyInjection;
using ParceleMais.Errors;
using ParceleMais.Simulations.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddParceleMais(options =>
{
    options.ClientId = builder.Configuration["ParceleMais:ClientId"] ?? string.Empty;
    options.ClientSecret = builder.Configuration["ParceleMais:ClientSecret"] ?? string.Empty;
    options.Environment = ParceleMaisEnvironment.Staging;
});

var app = builder.Build();

app.MapGet("/simulacao/{valor}/{prazo}", async (decimal valor, int prazo, IParceleMaisClient client, CancellationToken cancellationToken) =>
{
    try
    {
        var result = await client.Simulations.SimulateValuesAsync(new SimulateValuesRequest(valor, prazo), cancellationToken);
        return Results.Ok(result);
    }
    catch (ParceleMaisApiException ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: (int)ex.StatusCode, title: ex.ErrorCode);
    }
});

app.MapGet("/pedidos/{id:guid}", async (Guid id, IParceleMaisClient client, CancellationToken cancellationToken) =>
{
    try
    {
        var order = await client.Orders.GetAsync(id, cancellationToken);
        return Results.Ok(order);
    }
    catch (ParceleMaisApiException ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: (int)ex.StatusCode, title: ex.ErrorCode);
    }
});

app.Run();
