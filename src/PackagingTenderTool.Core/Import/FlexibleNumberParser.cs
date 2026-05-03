using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PackagingTenderTool.Core.Import;

/// <summary>
/// Parses decimals from supplier Excel cells (Danish and English-style grouping and decimals).
/// </summary>
public static class FlexibleNumberParser
{
    private static readonly Regex TrailingCurrency = new(
        @"(\s*(%|kr|eur|nok|dkk|sek|gbp|usd))+\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Attempts to parse a decimal using flexible grouping rules (last comma vs last dot wins as decimal separator).
    /// Does not throw.
    /// </summary>
    public static bool TryParseFlexibleDecimal(string? input, out decimal value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var t = input.Trim().Replace("\u00A0", string.Empty).Replace(" ", string.Empty);
        t = StripTrailingCurrencySuffixes(t);
        if (string.IsNullOrEmpty(t))
        {
            return false;
        }

        if (t.Any(char.IsLetter))
        {
            return false;
        }

        var numericOnly = new string(t
            .Where(static c => char.IsDigit(c) || c is ',' or '.' or '-')
            .ToArray());
        if (string.IsNullOrEmpty(numericOnly))
        {
            return false;
        }

        var normalized = NormalizeDecimalSeparators(numericOnly);
        if (normalized is null)
        {
            return false;
        }

        return decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static string StripTrailingCurrencySuffixes(string value)
    {
        var t = value;
        while (t.Length > 0)
        {
            var m = TrailingCurrency.Match(t);
            if (!m.Success || m.Index + m.Length != t.Length)
            {
                break;
            }

            t = t[..m.Index].TrimEnd();
        }

        return t;
    }

    /// <summary>
    /// When both comma and dot exist, the rightmost separator is the decimal separator.
    /// Otherwise applies common single-separator heuristics (thousands vs decimal).
    /// </summary>
    private static string? NormalizeDecimalSeparators(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var lastComma = value.LastIndexOf(',');
        var lastDot = value.LastIndexOf('.');

        if (lastComma >= 0 && lastDot >= 0)
        {
            return lastComma > lastDot
                ? value.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.')
                : value.Replace(",", string.Empty, StringComparison.Ordinal);
        }

        if (lastComma >= 0)
        {
            if (value.Count(static c => c == ',') > 1
                || value.Length - lastComma - 1 == 3)
            {
                return value.Replace(",", string.Empty, StringComparison.Ordinal);
            }

            return value.Replace(',', '.');
        }

        if (lastDot >= 0
            && (value.Count(static c => c == '.') > 1
                || value.Length - lastDot - 1 == 3))
        {
            return value.Replace(".", string.Empty, StringComparison.Ordinal);
        }

        return value;
    }
}
