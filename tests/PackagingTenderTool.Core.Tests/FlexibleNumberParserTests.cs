using PackagingTenderTool.Core.Import;

namespace PackagingTenderTool.Core.Tests;

public sealed class FlexibleNumberParserTests
{
    [Theory]
    [InlineData("12.50", 12.50)]
    [InlineData("12,50", 12.50)]
    [InlineData("1,200.50", 1200.50)]
    [InlineData("1.200,50", 1200.50)]
    [InlineData("1 200,50", 1200.50)]
    [InlineData("1 200.50", 1200.50)]
    public void TryParseFlexibleDecimal_parses_common_formats(string input, decimal expected)
    {
        Assert.True(FlexibleNumberParser.TryParseFlexibleDecimal(input, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1.000", 1000)] // single dot + exactly 3 fractional-looking digits → thousands
    [InlineData("1,000", 1000)] // single comma + 3 digits → thousands (EU-style)
    [InlineData("12.345,67", 12345.67)]
    [InlineData("12,345.67", 12345.67)]
    [InlineData("0", 0)]
    [InlineData("0,0", 0)]
    [InlineData("0.0", 0)]
    [InlineData(".5", 0.5)]
    [InlineData(",5", 0.5)]
    public void TryParseFlexibleDecimal_parses_thousand_and_edge_separators(string input, decimal expected)
    {
        Assert.True(FlexibleNumberParser.TryParseFlexibleDecimal(input, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("-12,50", -12.50)]
    [InlineData("-12.50", -12.50)]
    [InlineData("-1.234,56", -1234.56)]
    [InlineData("-1,234.56", -1234.56)]
    public void TryParseFlexibleDecimal_parses_negative_values(string input, decimal expected)
    {
        Assert.True(FlexibleNumberParser.TryParseFlexibleDecimal(input, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(" 12,50 kr ")]
    [InlineData("1000 EUR")]
    [InlineData("12,5 %")]
    public void TryParseFlexibleDecimal_strips_trailing_currency_or_percent(string input)
    {
        Assert.True(FlexibleNumberParser.TryParseFlexibleDecimal(input, out var value));
        Assert.True(value > 0);
    }

    [Fact]
    public void TryParseFlexibleDecimal_empty_fails()
    {
        Assert.False(FlexibleNumberParser.TryParseFlexibleDecimal(null, out _));
        Assert.False(FlexibleNumberParser.TryParseFlexibleDecimal("   ", out _));
    }

    [Fact]
    public void TryParseFlexibleDecimal_invalid_text_fails_without_throwing()
    {
        Assert.False(FlexibleNumberParser.TryParseFlexibleDecimal("not-a-number", out var v));
        Assert.Equal(0m, v);
    }

    [Fact]
    public void TryParseFlexibleDecimal_rejects_letters_in_numeric_token()
    {
        Assert.False(FlexibleNumberParser.TryParseFlexibleDecimal("12abc", out _));
    }
}
