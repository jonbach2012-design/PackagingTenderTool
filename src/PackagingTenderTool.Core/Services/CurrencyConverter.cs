namespace PackagingTenderTool.Core.Services;

/// <summary>
/// Converts amounts between currencies using rates from TenderSettings.
/// Inverse rates computed automatically. Same currency returns amount unchanged.
/// </summary>
public sealed class CurrencyConverter
{
    private readonly IReadOnlyDictionary<string, decimal> _rates;

    public CurrencyConverter(IReadOnlyDictionary<string, decimal> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);
        _rates = rates;
    }

    /// <summary>
    /// Creates a CurrencyConverter from TenderSettings.CurrencyRates.
    /// </summary>
    public static CurrencyConverter FromSettings(PackagingTenderTool.Core.Models.TenderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new CurrencyConverter(settings.CurrencyRates);
    }

    /// <summary>
    /// Converts amount from one currency to another.
    /// Returns amount unchanged if fromCurrency == toCurrency.
    /// Throws InvalidOperationException if rate not found and inverse not available.
    /// </summary>
    public decimal Convert(decimal amount, string fromCurrency, string toCurrency)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        var key = $"{fromCurrency.ToUpperInvariant()}:{toCurrency.ToUpperInvariant()}";
        if (_rates.TryGetValue(key, out var rate))
            return decimal.Round(amount * rate, 4, MidpointRounding.AwayFromZero);

        // Try inverse rate
        var inverseKey = $"{toCurrency.ToUpperInvariant()}:{fromCurrency.ToUpperInvariant()}";
        if (_rates.TryGetValue(inverseKey, out var inverseRate) && inverseRate != 0m)
            return decimal.Round(amount / inverseRate, 4, MidpointRounding.AwayFromZero);

        throw new InvalidOperationException(
            $"No exchange rate found for {fromCurrency} → {toCurrency}. " +
            $"Add \"{key}\" to TenderSettings.CurrencyRates.");
    }

    /// <summary>
    /// Returns true if conversion is possible between the two currencies.
    /// </summary>
    public bool CanConvert(string fromCurrency, string toCurrency)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return true;

        var key = $"{fromCurrency.ToUpperInvariant()}:{toCurrency.ToUpperInvariant()}";
        if (_rates.ContainsKey(key)) return true;

        var inverseKey = $"{toCurrency.ToUpperInvariant()}:{fromCurrency.ToUpperInvariant()}";
        return _rates.TryGetValue(inverseKey, out var inv) && inv != 0m;
    }
}
