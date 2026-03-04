using System.Text.RegularExpressions;

namespace YouthDataManager.Domain.Utilities;

/// <summary>
/// Normalizes Arabic text for consistent search and storage by unifying character variants,
/// removing diacritics (tashkeel), and tatweel (kashida).
/// </summary>
public static partial class ArabicNormalizer
{
    // Tashkeel/diacritics: Fathatan through Hamza below (U+064B–U+065F), Superscript Alef (U+0670)
    [GeneratedRegex(@"[\u064B-\u065F\u0670]", RegexOptions.Compiled)]
    private static partial Regex TashkeelRegex();

    /// <summary>
    /// Normalizes Arabic text for search and storage. Applies: remove tashkeel and tatweel,
    /// normalize alef/ya/ta marbuta/Farsi variants, trim and collapse whitespace.
    /// </summary>
    /// <param name="input">The text to normalize (can be null).</param>
    /// <returns>Normalized string, or empty string if input is null or whitespace.</returns>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var s = input;

        // Remove tashkeel (diacritics)
        s = TashkeelRegex().Replace(s, string.Empty);

        // Remove tatweel (kashida)
        s = s.Replace("\u0640", string.Empty);

        // Normalize alef variants to ا (U+0627)
        s = s.Replace('\u0622', '\u0627')  // آ
             .Replace('\u0623', '\u0627')  // أ
             .Replace('\u0625', '\u0627')  // إ
             .Replace('\u0671', '\u0627');  // ٱ

        // Normalize ya: alef maksura ى (U+0649) and Farsi ye ی (U+06CC) to ي (U+064A)
        s = s.Replace('\u0649', '\u064A')
             .Replace('\u06CC', '\u064A');

        // Normalize ta marbuta ة (U+0629) to ه (U+0647)
        s = s.Replace('\u0629', '\u0647');

        // Normalize Farsi kaf ک (U+06A9) to Arabic kaf ك (U+0643)
        s = s.Replace('\u06A9', '\u0643');

        // Collapse multiple whitespace and trim
        s = WhitespaceRegex().Replace(s.Trim(), " ");

        return s.Trim();
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
