using System.Collections.Generic;
using System.Collections.Specialized;
using App_Code.Objects;

namespace App_Code.Managers
{
    /// <summary>
    /// Handles properties associated with each item in the main tree collection.
    /// </summary>
    class PropertiesManager
    {
        readonly GlobalManager _globalManager;

        internal ListDictionary Properties;

        internal PropertiesManager(GlobalManager globalManager)
        {
            _globalManager = globalManager;
            Properties = new ListDictionary();
        }

        /// <summary>
        /// Sets properties associated with a specific tree level.
        /// </summary>
        internal void SetLevelProperties(string treeLevel, string itemKey)
        {
            switch (treeLevel)
            {
                case "Theme":
                    SetThemeProperties(itemKey);
                    break;
                case "Team":
                    SetTeamProperties(itemKey);
                    break;
            }
        }

        /// <summary>
        /// Sets properties associated specifically with themes.
        /// </summary>
        internal void SetThemeProperties(string theme)
        {
            // Determine the properties.
            var mainTheme = GlobalManager.GetMainTheme(theme);
            var priority = _globalManager.GetMainThemePriority(mainTheme);

            // An odd place to do this.
            _globalManager.MainThemeColors.TryAdd(mainTheme, "");

            // Set the properties.
            Properties["mainTheme"] = mainTheme;
            Properties["priority"] = priority;
        }

        /// <summary>
        /// Sets properties associated specifically with teams.
        /// </summary>
        internal void SetTeamProperties(string team)
        {
            double velocity;
            _globalManager.TeamVelocities.TryGetValue(team, out velocity);
            AddVelocity(velocity);
        }

        /// <summary>
        /// Sets properties associated specifically with PBIs.
        /// </summary>
        internal void SetPbiProperties(SimpleWorkItem simpleWorkItem)
        {
            Properties["priority"] = simpleWorkItem.Priority; // Useful in SortManager.
            Properties["simpleWorkItem"] = simpleWorkItem;
        }

        /// <summary>
        /// Adds effort to a possibly pre-existing effort sum.
        /// </summary>
        internal void AddEffort(double effort)
        {
            var oldEffortSum = Properties["effort"] is double ? (double)Properties["effort"] : 0d;
            Properties["effort"] = oldEffortSum + effort;
        }

        /// <summary>
        /// Adds velocity to a possibly pre-existing velocity sum.
        /// </summary>
        internal void AddVelocity(double velocity)
        {
            var oldVelocitySum = Properties["velocity"] is double ? (double) Properties["velocity"] : 0d;
            Properties["velocity"] = oldVelocitySum + velocity;
        }

        /// <summary>
        /// Makes a collection for adding children.
        /// </summary>
        internal void AddChildren()
        {
            var children = new Dictionary<string, ListDictionary>();
            Properties["children"] = children;
        }

        /// <summary>
        /// Gets this item's immediate children.
        /// </summary>
        /// <returns></returns>
        internal Dictionary<string, ListDictionary> GetChildren()
        {
            return Properties["children"] as Dictionary<string, ListDictionary>;
        }
    }
}