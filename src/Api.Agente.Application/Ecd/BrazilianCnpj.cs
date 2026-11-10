namespace Api.Agente.Application.Ecd;

public static class BrazilianCnpj
{
    public static bool IsValid(string? cnpj)
    {
        var digits = new string((cnpj ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != 14)
        {
            return false;
        }

        if (digits.All(c => c == digits[0]))
        {
            return false;
        }

        return CheckDigit(digits, 12) && CheckDigit(digits, 13);
    }

    private static bool CheckDigit(string digits, int position)
    {
        var weights = position == 12
            ? new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 }
            : new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            sum += (digits[i] - '0') * weights[i];
        }

        var mod = sum % 11;
        var expected = mod < 2 ? 0 : 11 - mod;
        return (digits[position] - '0') == expected;
    }
}
