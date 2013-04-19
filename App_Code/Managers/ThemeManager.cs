using System;

namespace Managers
{
    /// <summary>
    /// Handles the theme level in the tree specifically when needed.
    /// </summary>
    static class ThemeManager
    {
        /// <summary>
        /// Parses a string to determine if it should use a shortcut name instead.
        /// </summary>
        internal static string GetTrueTheme(string fullThemeName)
        {
            // It may already be correct.
            var trueThemeName = fullThemeName;

            // Handle the case where the theme begins with 'Other'.
            if (fullThemeName.Length >= 5 && fullThemeName.Substring(0, 5) == "Other")
            {
                trueThemeName = "Other";
            }
            else
            {
                // Some titles are shortened in parentheses and should be used instead.
                var leftIndex = fullThemeName.IndexOf('(');
                var rightIndex = fullThemeName.IndexOf(')');
                if (leftIndex != -1 && rightIndex != -1 && rightIndex > leftIndex)
                {
                    trueThemeName = fullThemeName.Substring(leftIndex + 1, rightIndex - leftIndex - 1);
                }
            }

            return trueThemeName;
        }

        /// <summary>
        /// Searches a HTML description to find a persistent theme color.
        /// </summary>
        /// <returns>The chosen HTML color or an empty string if it doesn't exist.</returns>
        internal static string ParseHtmlForColor(string html)
        {
            // Find our manually placed identifier.
            var identifierStart = html.IndexOf("TREEMAP COLOR", StringComparison.InvariantCultureIgnoreCase);
            if (identifierStart == -1)
            {
                return string.Empty;
            }

            // Find the closest leading instance of the relevant HTML attribute to the identifier.
            const string htmlColorPrefix = "BACKGROUND-COLOR: ";
            var htmlColorStart = html.LastIndexOf(htmlColorPrefix, identifierStart, StringComparison.Ordinal);
            if (htmlColorStart == -1)
            {
                return string.Empty;
            }

            // Pull out the HTML color.
            const int htmlColorLength = 7; // #RRGGBB = 7 characters.
            var htmlColor = html.Substring(htmlColorStart + htmlColorPrefix.Length, htmlColorLength);
            return htmlColor;
        }
    }
}