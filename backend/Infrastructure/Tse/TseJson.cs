using System.Globalization;
using System.Text.Json;

namespace Election.Infrastructure.Tse;

internal static class TseJson
{
    internal static string Text(JsonElement element, string key)
    {
        if (!element.TryGetProperty(key, out var value) || value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number))
            throw new InvalidDataException($"Campo TSE ausente ou inválido: {key}");
        return value.ToString();
    }
    internal static long Integer(JsonElement element, string key)
    {
        if (!long.TryParse(Text(element, key), NumberStyles.None, CultureInfo.InvariantCulture, out var n))
            throw new InvalidDataException($"Inteiro TSE inválido: {key}");
        return n;
    }
    internal static decimal Percent(JsonElement element, string key)
    {
        var text = Text(element, key).Replace(',', '.');
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var n) || n is < 0 or > 100)
            throw new InvalidDataException($"Percentual TSE inválido: {key}");
        return n;
    }
    internal static JsonElement.ArrayEnumerator Array(JsonElement element, string key)
    {
        if (!element.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException($"Lista TSE ausente: {key}");
        return value.EnumerateArray();
    }
}
