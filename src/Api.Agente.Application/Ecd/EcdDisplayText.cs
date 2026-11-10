using System.Globalization;

namespace Api.Agente.Application.Ecd;

public static class EcdDisplayText
{
    public static string FormatSpedDate(string ddMmYyyy)
    {
        if (ddMmYyyy.Length != 8
            || !int.TryParse(ddMmYyyy[..2], out var day)
            || !int.TryParse(ddMmYyyy[2..4], out var month)
            || !int.TryParse(ddMmYyyy[4..], out var year))
        {
            return ddMmYyyy;
        }

        return $"{day:D2}/{month:D2}/{year}";
    }

    public static string FormatCnpj(string digits)
    {
        if (digits.Length != 14)
        {
            return digits;
        }

        return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
    }

    public static string FormatMoney(decimal value) =>
        value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));

    public static string DescribeField(string recordType, string? fieldName) =>
        (recordType, fieldName) switch
        {
            ("0000", "DT_INI") => "data inicial do arquivo",
            ("0000", "DT_FIN") => "data final do arquivo",
            ("0000", "CNPJ") => "CNPJ da empresa",
            ("I030", "DT_INI_ESC") => "data de início da escrituração",
            ("I030", "CNPJ_EMP") => "CNPJ na identificação (I030)",
            ("I150", "DT_INI") => "data inicial do saldo",
            ("I150", "DT_FIN") => "data final do saldo",
            ("I200", "DT_LCTO") => "data do lançamento",
            ("I250", "VL_DEB_CRE") => "valor da partida",
            ("I250", "COD_CTA") => "código da conta",
            ("I155", "COD_CTA") => "código da conta",
            ("I050", "COD_CTA") => "código da conta",
            ("I050", "NAT_CONTA") => "natureza da conta",
            ("I050", "COD_CTA_SUP") => "conta superior",
            _ => fieldName ?? "campo",
        };
}
