using PackagingTenderTool.Core.Models;
using System.Text.Json;

namespace PackagingTenderTool.Core.Services;

public sealed class EprFeeService : IEprFeeService
{
    private readonly IReadOnlyList<EprRate> rates;

    public EprFeeService()
        : this(LoadRatesFromDefaultJsonOrFallback())
    {
    }

    public EprFeeService(IEnumerable<EprRate> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);
        this.rates = rates.ToList();
    }

    public IReadOnlyList<EprRate> GetRates() => rates;

    public decimal CalculateFee(string country, string category, decimal weightKg)
    {
        if (!TryCalculateFee(country, category, weightKg, out var fee, out var flag))
        {
            throw new ArgumentException(flag?.Reason ?? "EPR fee could not be calculated.");
        }

        return fee;
    }

    public bool TryCalculateFee(
        string country,
        string category,
        decimal weightKg,
        out decimal fee,
        out ManualReviewFlag? manualReviewFlag)
    {
        fee = 0m;
        manualReviewFlag = null;

        if (string.IsNullOrWhiteSpace(country))
        {
            manualReviewFlag = new ManualReviewFlag
            {
                FieldName = "EprCountry",
                Reason = "EPR country is missing.",
                Severity = ManualReviewSeverity.Warning
            };
            return false;
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            manualReviewFlag = new ManualReviewFlag
            {
                FieldName = "EprCategory",
                Reason = "EPR category is missing.",
                Severity = ManualReviewSeverity.Warning
            };
            return false;
        }

        if (weightKg < 0m)
        {
            manualReviewFlag = new ManualReviewFlag
            {
                FieldName = "WeightKg",
                SourceValue = weightKg.ToString("G"),
                Reason = "Weight cannot be negative for EPR fee calculation.",
                Severity = ManualReviewSeverity.Error
            };
            return false;
        }

        if (weightKg == 0m)
        {
            fee = 0m;
            return true;
        }

        var normalizedCountry = country.Trim().ToUpperInvariant();
        var normalizedCategory = NormalizeCategory(category);

        var rate = rates.FirstOrDefault(r =>
            string.Equals(r.CountryCode, normalizedCountry, StringComparison.OrdinalIgnoreCase)
            && string.Equals(r.Category, normalizedCategory, StringComparison.OrdinalIgnoreCase));

        if (rate is null)
        {
            manualReviewFlag = new ManualReviewFlag
            {
                FieldName = "EprRate",
                SourceValue = $"{normalizedCountry}|{normalizedCategory}",
                Reason = $"Missing EPR rate for country '{normalizedCountry}' and category '{normalizedCategory}'.",
                Severity = ManualReviewSeverity.Warning
            };
            return false;
        }

        fee = decimal.Round(weightKg * rate.RatePerKg, 4);
        return true;
    }

    private static string NormalizeCategory(string category)
    {
        var trimmed = category.Trim();

        return trimmed.Equals("PackagingMixed", StringComparison.OrdinalIgnoreCase)
            ? "Packaging Mixed"
            : trimmed;
    }

    private static IReadOnlyList<EprRate> LoadRatesFromDefaultJsonOrFallback()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "config", "epr-settings.json");
            if (!File.Exists(path))
            {
                return CreatePlaceholderRates();
            }

            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<EprSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (settings?.Rates is null || settings.Rates.Count == 0)
            {
                return CreatePlaceholderRates();
            }

            return settings.Rates
                .Where(rate => !string.IsNullOrWhiteSpace(rate.CountryCode) && !string.IsNullOrWhiteSpace(rate.Category))
                .Select(rate => new EprRate
                {
                    CountryCode = rate.CountryCode.Trim().ToUpperInvariant(),
                    Category = rate.Category.Trim(),
                    RatePerKg = rate.RatePerKg
                })
                .ToList();
        }
        catch
        {
            // Robust default: never block evaluation because config is missing/broken.
            return CreatePlaceholderRates();
        }
    }

    private static IReadOnlyList<EprRate> CreatePlaceholderRates()
    {
        // Placeholder values only. Replace with Scandi Standard data later.
        const decimal low = 0.10m;
        const decimal mid = 0.50m;
        const decimal high = 1.20m;

        var supportedCountries = new[] { "DK", "SE", "NO", "FI", "IE" };
        var coreCategories = new[] { "Labels", "Cardboard", "Trays", "Packaging Mixed", "Flexibles" };

        decimal RateForCategory(string category) => category switch
        {
            "Cardboard" => low,
            "Labels" => mid,
            "Trays" => mid,
            "Packaging Mixed" => mid,
            "Flexibles" => high,
            _ => mid
        };

        return supportedCountries
            .SelectMany(country => coreCategories.Select(category => new EprRate
            {
                CountryCode = country,
                Category = category,
                RatePerKg = RateForCategory(category)
            }))
            .ToList();
    }

    private sealed class EprSettings
    {
        public List<string> SupportedCountries { get; set; } = [];
        public List<string> Categories { get; set; } = [];
        public List<EprRateDto> Rates { get; set; } = [];
    }

    private sealed class EprRateDto
    {
        public string CountryCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal RatePerKg { get; set; }
    }
}

