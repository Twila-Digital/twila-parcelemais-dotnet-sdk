namespace ParceleMais.Establishments.Models;

/// <param name="Phone">Telefone celular no formato E.164 (código do país + DDD + número). Ex.: +5511999998888.</param>
public sealed record EstablishmentOwner(string Name, string Email, string Phone);
