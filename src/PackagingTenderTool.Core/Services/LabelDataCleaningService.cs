using System.Globalization;
using System.Text;
using PackagingTenderTool.Core.Import;
using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Services;

public sealed class LabelDataCleaningService
{
    public CleanedLabelLineItem Clean(LabelLineItem lineItem)
    {
        ArgumentNullException.ThrowIfNull(lineItem);

        return new CleanedLabelLineItem
        {
            Source = lineItem,
            NormalizedLabelSize = NormalizeLabelSize(lineItem.LabelSize),
            NormalizedMaterial = NormalizeMaterial(lineItem.Material),
            Country = NormalizeCountry(lineItem.Site),
            NormalizedColorGroup = NormalizeColorGroup(lineItem.NumberOfColors, lineItem.SourceManualReviewFlags),
            NormalizedWindingDirection = NormalizeWindingDirection(lineItem.WindingDirection)
        };
    }

    public IReadOnlyList<CleanedLabelLineItem> CleanMany(IEnumerable<LabelLineItem> lineItems)
    {
        ArgumentNullException.ThrowIfNull(lineItems);

        return lineItems.Select(Clean).ToList();
    }

    public static string? NormalizeLabelSize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", string.Empty)
            .Replace(',', '.');
        var parts = normalized.Split('X', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return normalized;
        }

        return TryNormalizeDimension(parts[0], out var width)
            && TryNormalizeDimension(parts[1], out var height)
            ? $"{width}x{height}"
            : normalized.Replace('X', 'x');
    }

    public static string? NormalizeMaterial(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = CollapseWhitespace(value).ToLowerInvariant();
        if (normalized.Contains("thermo", StringComparison.OrdinalIgnoreCase))
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
        }

        if (normalized.StartsWith("pp ", StringComparison.OrdinalIgnoreCase)
            || normalized == "pp")
        {
            return normalized.ToUpperInvariant().Replace(" TOP ", " top ", StringComparison.OrdinalIgnoreCase);
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
    }

    public static string NormalizeCountry(string? site)
    {
        if (string.IsNullOrWhiteSpace(site))
        {
            return "(missing)";
        }

        var normalized = CollapseWhitespace(site).ToLowerInvariant();
        return normalized switch
        {
            "jæren" or "jaeren" or "stokke" => "Norway",
            _ => "Unknown"
        };
    }

    public static string? NormalizeColorGroup(int? numberOfColors, IEnumerable<ManualReviewFlag>? sourceFlags = null)
    {
        if (numberOfColors is null)
        {
            return sourceFlags?.Any(flag => flag.FieldName == nameof(LabelLineItem.NumberOfColors)) == true
                ? "Invalid color count"
                : null;
        }

        return numberOfColors.Value switch
        {
            0 => "Unprinted",
            <= 2 => "1-2 colors",
            <= 4 => "3-4 colors",
            <= 6 => "5-6 colors",
            _ => "7+ colors"
        };
    }

    public static string? NormalizeWindingDirection(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = CollapseWhitespace(value).ToLowerInvariant();
        if (normalized.Contains("bottom"))
        {
            return "Bottom first";
        }

        if (normalized.Contains("head"))
        {
            return "Head first";
        }

        if (normalized.Contains("left"))
        {
            return "Left side first";
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
    }

    private static bool TryNormalizeDimension(string value, out string normalized)
    {
        normalized = string.Empty;
        if (!FlexibleNumberParser.TryParseFlexibleDecimal(value, out var decimalValue))
        {
            return false;
        }

        normalized = decimalValue % 1m == 0m
            ? decimalValue.ToString("0", CultureInfo.InvariantCulture)
            : decimalValue.ToString("0.###", CultureInfo.InvariantCulture);
        return true;
    }

    private static string CollapseWhitespace(string value)
    {
        var builder = new StringBuilder();
        var previousWasWhitespace = false;
        foreach (var character in value.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                }

                previousWasWhitespace = true;
                continue;
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        return builder.ToString();
    }
}
