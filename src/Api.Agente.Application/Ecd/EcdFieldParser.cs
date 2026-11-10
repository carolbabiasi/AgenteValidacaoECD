using System.Globalization;
using Api.Agente.Domain.Ecd;

namespace Api.Agente.Application.Ecd;

public static class EcdFieldParser
{
    public static string[] SplitFields(string raw) => raw.Split('|', StringSplitOptions.None);

    public static string? GetField(EcdLine line, int index)
    {
        var fields = SplitFields(line.RawText);
        return fields.Length > index ? fields[index] : null;
    }

    public static bool TryParseSpedDate(string? value, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Length != 8)
        {
            return false;
        }

        return DateOnly.TryParseExact(value, "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    public static bool TryParseSpedDecimal(string? value, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    public static string OnlyDigits(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : new string(value.Where(char.IsDigit).ToArray());
}
