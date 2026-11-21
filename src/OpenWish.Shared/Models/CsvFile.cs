using System.Globalization;
using System.Text;

namespace OpenWish.Shared.Models;

/// <summary>Writes spreadsheet-safe CSV: quoted when needed, and cells that could run as formulas are neutralized.</summary>
public static class CsvFile
{
    public static string Build(IReadOnlyList<string> header, IEnumerable<IReadOnlyList<string?>> rows)
    {
        var builder = new StringBuilder();
        AppendRow(builder, header);
        foreach (var row in rows)
        {
            AppendRow(builder, row);
        }

        return builder.ToString();
    }

    public static string Price(decimal? price) =>
        price?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty;

    public static string Date(DateTimeOffset? date) =>
        date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    public static string FileName(string name, string fallback)
    {
        var cleaned = new string(name.Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_').ToArray()).Trim();
        return cleaned.Length == 0 ? fallback : cleaned.Replace(' ', '-');
    }

    private static void AppendRow(StringBuilder builder, IReadOnlyList<string?> cells)
    {
        for (var i = 0; i < cells.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(Escape(cells[i]));
        }

        builder.Append("\r\n");
    }

    private static string Escape(string? cell)
    {
        if (string.IsNullOrEmpty(cell))
        {
            return string.Empty;
        }

        // A leading =, +, -, @, tab, or carriage return makes spreadsheets evaluate the cell as a formula.
        if (cell[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            cell = "'" + cell;
        }

        return cell.AsSpan().IndexOfAny(",\"\r\n") >= 0
            ? "\"" + cell.Replace("\"", "\"\"") + "\""
            : cell;
    }
}