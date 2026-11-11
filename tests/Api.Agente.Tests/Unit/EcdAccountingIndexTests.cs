using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Ecd;
using Api.Agente.Infrastructure.Ecd;

namespace Api.Agente.Tests.Unit;

public sealed class EcdAccountingIndexTests
{
    [Fact]
    public async Task Build_ECD001_Basica_IndexaPeriodosSaldosELancamentos()
    {
        var lines = await LoadFixtureLinesAsync("ECD_001_BASICA.txt");
        var index = EcdAccountingIndex.Build(lines);

        Assert.Contains(index.Chart.All, a => a.CodConta == "1.1.1.01");
        Assert.True(index.Chart.ContainsAccount("4.1"));
        Assert.True(index.Chart.IsResultAccount("4.1"));

        Assert.Single(index.PeriodicBalances.Periods);
        var period = index.PeriodicBalances.Periods[0];
        Assert.Equal(new DateOnly(2025, 1, 1), period.DtIni);
        Assert.Equal(new DateOnly(2025, 12, 31), period.DtFin);

        Assert.Equal(6, index.PeriodicBalances.Balances.Count);
        var caixa = index.PeriodicBalances.TryGetBalance(period, EcdAccountKey.Create("1.1.1.01", string.Empty));
        Assert.NotNull(caixa);
        Assert.Equal(10000m, caixa!.TotalDebitos);

        Assert.Equal(4, index.Journal.Headers.Count);
        Assert.Equal(8, index.Journal.Parts.Count);

        var first = index.Journal.Headers[0];
        var (deb, cred) = index.Journal.SumParts(first);
        Assert.Equal(deb, cred);
        Assert.Equal(187.25m, deb);

        var contained = index.PeriodicBalances.FindPeriodContaining(new DateOnly(2025, 2, 2));
        Assert.NotNull(contained);
        Assert.Equal(period.HeaderLineNumber, contained!.HeaderLineNumber);
    }

    [Fact]
    public void Build_LeiauteManual_I155_ParseiaCamposOficiais()
    {
        var lines = new List<EcdLine>
        {
            new(10, "|I150|01012023|31012023|", "I150"),
            new(20, "|I155|1.1.1.01||1000,00|D|500,00|400,00|1100,00|D|", "I155"),
        };

        var index = EcdPeriodicBalanceIndex.Build(lines);
        Assert.Single(index.Balances);
        var balance = index.Balances[0];
        Assert.Equal(1000m, balance.SaldoInicial);
        Assert.Equal(500m, balance.TotalDebitos);
        Assert.Equal(400m, balance.TotalCreditos);
        Assert.Equal(1100m, balance.SaldoFinal);
    }

    private static async Task<List<EcdLine>> LoadFixtureLinesAsync(string fileName)
    {
        var path = Path.Combine(FindRepoRoot(), "ecds-teste", fileName);
        var content = await File.ReadAllTextAsync(path);
        var list = new List<EcdLine>();
        var n = 0;
        foreach (var raw in content.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries))
        {
            n++;
            list.Add(new EcdLine(n, raw, StreamingEcdFileReader.ParseRecordType(raw)));
        }

        return list;
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, "ecds-teste")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }
}
