using System.Text.RegularExpressions;
using System.Windows.Media;

namespace Fluent11Helper
{
    internal static class AcrylicTint
    {
        public const string DefaultDark = "#88222222";
        public const string DefaultLight = "#88F3F3F3";
        private static readonly Regex HexColor = new Regex("^#?([0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);

        public static string Normalize(string text)
        {
            var match = HexColor.Match(text?.Trim() ?? string.Empty);
            if (!match.Success) return null;

            var color = (Color)ColorConverter.ConvertFromString("#" + match.Groups[1].Value);
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public static Color ToColor(string text, bool dark)
        {
            return (Color)ColorConverter.ConvertFromString(Normalize(text) ?? (dark ? DefaultDark : DefaultLight));
        }
    }
}
