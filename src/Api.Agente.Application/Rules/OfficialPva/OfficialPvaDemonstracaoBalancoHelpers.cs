using Api.Agente.Application.Ecd;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

internal static class OfficialPvaDemonstracaoBalancoHelpers
{
    public static void ValidateSomaTotalizadores(
        IReadOnlyList<EcdDemonstracaoAglutinacaoLine> lines,
        bool saldoInicial,
        string recordType,
        List<RuleEvidence> evidence)
    {
        foreach (var totalizador in lines.Where(l => l.IndCodAgl == "T"))
        {
            var filhos = lines
                .Where(c => string.Equals(c.CodAglSup, totalizador.CodAgl, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (filhos.Count == 0)
            {
                continue;
            }

            var soma = filhos.Sum(c => saldoInicial ? EcdDemonstracaoSaldo.SignedIni(c) : EcdDemonstracaoSaldo.SignedFin(c));
            var esperado = saldoInicial
                ? EcdDemonstracaoSaldo.SignedIni(totalizador)
                : EcdDemonstracaoSaldo.SignedFin(totalizador);
            if (EcdDemonstracaoSaldo.AmountsEqual(esperado, soma))
            {
                continue;
            }

            var campo = saldoInicial ? "VL_CTA_INI" : "VL_CTA_FIN";
            evidence.Add(new RuleEvidence
            {
                RecordType = recordType,
                LineNumber = totalizador.LineNumber,
                Message =
                    $"Linha {totalizador.LineNumber}, COD_AGL {totalizador.CodAgl}: {campo} do totalizador "
                    + $"({esperado:F2}) difere da soma dos filhos ({soma:F2}).",
            });
        }
    }

    public static void ValidateAtivoPassivoNivel1(
        IReadOnlyList<EcdDemonstracaoAglutinacaoLine> lines,
        bool saldoInicial,
        List<RuleEvidence> evidence)
    {
        var ativo = lines.FirstOrDefault(l =>
            l.Nivel == 1 && string.Equals(l.IndGrpBal, "A", StringComparison.OrdinalIgnoreCase));
        var passivo = lines.FirstOrDefault(l =>
            l.Nivel == 1 && string.Equals(l.IndGrpBal, "P", StringComparison.OrdinalIgnoreCase));
        if (ativo is null || passivo is null)
        {
            return;
        }

        var vlAtivo = saldoInicial ? ativo.VlIni : ativo.VlFin;
        var vlPassivo = saldoInicial ? passivo.VlIni : passivo.VlFin;
        if (EcdDemonstracaoSaldo.AmountsEqual(vlAtivo, vlPassivo))
        {
            return;
        }

        var campo = saldoInicial ? "VL_CTA_INI" : "VL_CTA_FIN";
        evidence.Add(new RuleEvidence
        {
            RecordType = "J100",
            LineNumber = ativo.LineNumber,
            Message =
                $"Nível 1 Ativo x Passivo ({campo}): Ativo ({vlAtivo:F2}) difere de Passivo ({vlPassivo:F2}).",
        });
    }

    public static void ValidateBalancoDetalheAtivoPassivo(
        IReadOnlyList<EcdDemonstracaoAglutinacaoLine> lines,
        bool saldoInicial,
        List<RuleEvidence> evidence)
    {
        var detalhesAtivo = lines.Where(l =>
            l.IndCodAgl == "D" && string.Equals(l.IndGrpBal, "A", StringComparison.OrdinalIgnoreCase));
        var detalhesPassivo = lines.Where(l =>
            l.IndCodAgl == "D" && string.Equals(l.IndGrpBal, "P", StringComparison.OrdinalIgnoreCase));

        if (!detalhesAtivo.Any() || !detalhesPassivo.Any())
        {
            return;
        }

        var somaAtivo = detalhesAtivo.Sum(l => saldoInicial ? l.VlIni : l.VlFin);
        var somaPassivo = detalhesPassivo.Sum(l => saldoInicial ? l.VlIni : l.VlFin);
        if (EcdDemonstracaoSaldo.AmountsEqual(somaAtivo, somaPassivo))
        {
            return;
        }

        var campo = saldoInicial ? "VL_CTA_INI" : "VL_CTA_FIN";
        evidence.Add(new RuleEvidence
        {
            RecordType = "J100",
            LineNumber = detalhesAtivo.First().LineNumber,
            Message =
                $"Soma dos detalhes Ativo ({somaAtivo:F2}) difere da soma dos detalhes Passivo ({somaPassivo:F2}) em {campo}.",
        });
    }
}
