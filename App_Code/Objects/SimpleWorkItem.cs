using App_Code.Managers;

namespace App_Code.Objects
{
    /// <summary>
    /// Represents a work item with only relevant information.
    /// </summary>
    class SimpleWorkItem
    {
        internal int Id { get; set; }
        internal double Effort { get; set; }
        internal double Priority { get; set; }
        internal string Type { get; set; } // Not currently used.
        internal string State { get; set; }

        // Used for levels.
        internal string Quarter { get; set; }
        internal string Sprint { get; set; }
        internal string Team { get; set; }
        internal string Product { get; set; }
        internal string Theme { get; set; }
        internal string Title { get; set; } // "PBI level".

        // Not PBI-related.
        internal string MainThemeColor { get; set; }
        internal double MeanVelocity { get; set; }

        internal static string[] NonPbiLevels = { "Quarter", "Sprint", "Team", "Product", "Theme" };

        /// <summary>
        /// Gets the unique identifier for an item's level.
        /// </summary>
        internal string GetKey(string groupLevel)
        {
            switch (groupLevel)
            {
                case "Quarter":
                    return Quarter;
                case "Sprint":
                    return Sprint;
                case "Team":
                    return Team;
                case "Product":
                    return Product;
                case "Theme":
                    return Theme;
                case "PBI": // PBI titles need to be made unique.
                    return Title + GlobalManager.LevelSplitter + Id;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the string representation of a missing value.
        /// </summary>
        internal static string GetMissingString(string groupLevel)
        {
            return "NO " + groupLevel.ToUpper();
        }
    }
}