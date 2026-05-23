using PackagingTenderTool.Core.Services;
using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Tests;

public sealed class CurrencyConverterTests
{
    private static CurrencyConverter CreateDefault() =>
        CurrencyConverter.FromSettings(new TenderSettings());

    [Fact]
    public void SameCurrency_ReturnsAmountUnchanged()
    {
        var converter = CreateDefault();
        Assert.Equal(100m, converter.Convert(100m, "NOK", "NOK"));
        Assert.Equal(100m, converter.Convert(100m, "DKK", "DKK"));
    }

    [Fact]
    public void DkkToNok_UsesConfiguredRate()
    {
        var converter = CreateDefault();
        var result = converter.Convert(100m, "DKK", "NOK");
        Assert.Equal(144.0300m, result);
    }

    [Fact]
    public void NokToDkk_UsesInverseRate()
    {
        var converter = CreateDefault();
        var dkk = converter.Convert(144.03m, "NOK", "DKK");
        // Should be approximately 100 DKK
        Assert.InRange(dkk, 99.5m, 100.5m);
    }

    [Fact]
    public void UnknownRate_ThrowsInvalidOperationException()
    {
        var converter = new CurrencyConverter(new Dictionary<string, decimal>
        {
            ["DKK:NOK"] = 1.4403m
        });
        Assert.Throws<InvalidOperationException>(() =>
            converter.Convert(100m, "DKK", "GBP"));
    }

    [Fact]
    public void CanConvert_ReturnsTrueForKnownAndInversePairs()
    {
        var converter = CreateDefault();
        Assert.True(converter.CanConvert("DKK", "NOK"));
        Assert.True(converter.CanConvert("NOK", "DKK")); // inverse
        Assert.True(converter.CanConvert("NOK", "NOK")); // same
        Assert.False(converter.CanConvert("DKK", "GBP")); // unknown
    }

    [Fact]
    public void FromSettings_UsesDefaultRatesFromTenderSettings()
    {
        var settings = new TenderSettings();
        var converter = CurrencyConverter.FromSettings(settings);
        Assert.True(converter.CanConvert("DKK", "NOK"));
        Assert.True(converter.CanConvert("NOK", "EUR"));
    }
}
