using Convy.Application.Common.Services;
using FluentAssertions;

namespace Convy.Application.Tests.Common;

public class UserFacingTextNormalizerTests
{
    private readonly UserFacingTextNormalizer _normalizer = new();

    [Theory]
    [InlineData(" leche ", "Leche")]
    [InlineData("LECHE", "Leche")]
    [InlineData("papel   higiénico", "Papel higiénico")]
    [InlineData("  bebida   de avena ", "Bebida de avena")]
    [InlineData("Coca-Cola", "Coca-Cola")]
    [InlineData("iPhone", "iPhone")]
    public void NormalizeTitle_ReturnsDisplayTitle(string value, string expected)
    {
        _normalizer.NormalizeTitle(value).Should().Be(expected);
    }

    [Theory]
    [InlineData("Leche", "leche")]
    [InlineData(" leche ", "leche")]
    [InlineData("CAFÉ", "cafe")]
    [InlineData("Cafe", "cafe")]
    [InlineData("papel   higiénico", "papel higienico")]
    public void NormalizeForComparison_ReturnsStableKey(string value, string expected)
    {
        _normalizer.NormalizeForComparison(value).Should().Be(expected);
    }
}
