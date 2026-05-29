using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Convy.Application.Common.Interfaces;

namespace Convy.Application.Common.Services;

public partial class UserFacingTextNormalizer : IUserFacingTextNormalizer
{
    public string NormalizeTitle(string value)
    {
        var collapsed = CollapseWhitespace(value);
        if (collapsed.Length == 0)
            return string.Empty;

        if (IsUniformCase(collapsed))
        {
            var lower = collapsed.ToLowerInvariant();
            return char.ToUpperInvariant(lower[0]) + lower[1..];
        }

        return collapsed;
    }

    public string NormalizeForComparison(string value)
    {
        var collapsed = CollapseWhitespace(value).ToLowerInvariant();
        if (collapsed.Length == 0)
            return string.Empty;

        var normalized = collapsed.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string CollapseWhitespace(string value) =>
        WhitespaceRegex().Replace(value.Trim(), " ");

    private static bool IsUniformCase(string value)
    {
        var letters = value.Where(char.IsLetter).ToList();
        if (letters.Count == 0)
            return false;

        return letters.All(char.IsLower) || letters.All(char.IsUpper);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
