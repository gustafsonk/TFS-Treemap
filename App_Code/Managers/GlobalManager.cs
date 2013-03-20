using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using App_Code.Objects;

namespace App_Code.Managers
{
    /// <summary>
    /// Shares variables and methods between multiple classes.
    /// </summary>
    class GlobalManager
    {
        internal string TreeType;
        internal IList<string> TreeLevels;
        internal IList<string> ColorLevels;
        internal IList<string> SizeLevels;
        internal IList<string> SortLevels;
        internal Dictionary<string, HashSet<string>> FilterLevels;
        internal const char LevelSplitter = '-';

        internal readonly Dictionary<string, ListDictionary> Tree;
        internal Dictionary<string, double> ThemePriorities;
        internal ConcurrentDictionary<string, ConcurrentDictionary<string, string>> Groupings;
        internal ConcurrentDictionary<string, string> MainThemeColors;
        internal ConcurrentDictionary<string, double> TeamVelocities;

        internal GlobalManager()
        {
            Tree = new Dictionary<string, ListDictionary>();
            ThemePriorities = new Dictionary<string, double>();
            Groupings = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
            MainThemeColors = new ConcurrentDictionary<string, string>();
            TeamVelocities = new ConcurrentDictionary<string, double>();
        }

        /// <summary>
        /// Parses a string of characters to determine the main theme name.
        /// </summary>
        internal static string GetMainTheme(string themeName)
        {
            var mainThemeName =
                new string(
                    themeName
                    .TakeWhile(c => c != '.' && c != ',' && c != '(') // Read the entire string until one of these characters is found.
                    .ToArray() // Convert IEnumerable<char> to a char[], which is convertible to a string.
                )
                .Trim() // Remove all leading/trailing whitespace.
                .ToUpper(); // Make it all uppercase to ignore case sensitivity.

            return mainThemeName;
        }

        /// <summary>
        /// A getter made for retrieving a main theme's priority.
        /// </summary>
        internal double GetMainThemePriority(string mainThemeName)
        {
            double priority;
            ThemePriorities.TryGetValue(mainThemeName, out priority);
            return priority;
        }

        /// <summary>
        /// Pulls from the main grouping collection to determine the provided group name's color.
        /// </summary>
        internal string GetColor(string groupLevel, string groupName)
        {
            ConcurrentDictionary<string, string> colorGroup;
            Groupings.TryGetValue(groupLevel, out colorGroup);
            string color;
            colorGroup.TryGetValue(groupName, out color);
            return color ?? "#FFFFFF"; // Should never happen.
        }

        /// <summary>
        /// Pulls this theme's color from the main theme collection.
        /// </summary>
        internal string GetMainThemeColor(string mainTheme)
        {
            string color;
            MainThemeColors.TryGetValue(mainTheme, out color);
            return color ?? "#FFFFFF"; // Should never happen.
        }

        /// <summary>
        /// Uses properties of the SimpleWorkItem to determine this PBI's color.
        /// </summary>
        internal static string GetPbiColor(SimpleWorkItem pbi)
        {
            // Mark PBIs with no effort as red.
            if (pbi.Effort == 0)
            {
                return "#BB0000";
            }

            // Otherwise, color by the PBI's state.
            switch (pbi.State)
            {
                case "New":
                    return "#0000BB";
                case "Approved":
                    return "#990099";
                case "Committed":
                    return "#FF6600";
                case "Done":
                    return "#00BB00";
                case "Removed":
                    return "#BB0000";
                default: // Unknown state.
                    return "#FFFFFF";
            }
        }
    }
}