namespace ParceleMais.Serialization;

internal static class EnumMapping
{
    public static TEnum FromWireValue<TEnum>(int value) where TEnum : struct, Enum
    {
        var candidate = (TEnum)Enum.ToObject(typeof(TEnum), value);

        if (Enum.IsDefined(typeof(TEnum), candidate))
            return candidate;

        var unknownField = UnknownEnumJsonConverterFactory.FindUnknownMember(typeof(TEnum))
            ?? throw new InvalidOperationException($"O enum {typeof(TEnum).Name} não tem um membro marcado com [UnknownValue].");

        return (TEnum)unknownField.GetValue(null)!;
    }
}
