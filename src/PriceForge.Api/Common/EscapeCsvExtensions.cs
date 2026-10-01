namespace PriceForge.Api.Common;

public static class EscapeCsvExtensions
{
    public static string EscapeCsv(this string value)
    {
        if (value.IndexOfAny([',', '"', '\n', '\r']) == -1)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
