namespace Api.Agente.Tests.Fixtures;

public static class SampleEcdFiles
{
    public const string ValidMinimal =
        "|0000|LECD|9.0|01012025|31122025|EMPRESA SAMPLE|\r\n|0990|2|\r\n";

    public const string MissingOpening =
        "|0990|1|\r\n";

    public const string MalformedLine =
        "|0000|LECD|9.0|01012025|31122025|EMPRESA|\r\nlinha-sem-pipe\r\n";
}
