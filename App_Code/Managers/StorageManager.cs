using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using App_Code.Objects;

namespace App_Code.Managers
{
    /// <summary>
    /// Handles storing information for various work item types.
    /// </summary>
    class StorageManager
    {
        readonly GlobalManager _globalManager;

        readonly TreeManager _treeManager;

        internal StorageManager(GlobalManager globalManager)
        {
            _globalManager = globalManager;
            _treeManager = new TreeManager(globalManager);
        }

        /// <summary>
        /// Pulls information from work item types.
        /// </summary>
        internal void StoreWorkItem(string type, SimpleWorkItem simpleWorkItem)
        {
            switch (type)
            {
                case "PBIs":
                    StorePbi(simpleWorkItem);
                    break;
                case "Themes":
                    StoreTheme(simpleWorkItem);
                    break;
                case "Filters":
                    StoreFilter(simpleWorkItem);
                    break;
                case "Teams":
                    StoreTeam(simpleWorkItem);
                    break;
                default:
                    throw new Exception("Work item type: " + type + ", is not supported.");
            }
        }

        /// <summary>
        /// Stores a SimpleWorkItem in a logically nested collection for efficient iteration.
        /// </summary>
        void StorePbi(SimpleWorkItem simpleWorkItem)
        {
            // Only PBIs are currently supported.
            if (simpleWorkItem.Type != "Product Backlog Item")
            {
                return;
            }

            // Don't store this item if it's marked to be ignored by any of the filter levels.
            if (IsNotIgnored(simpleWorkItem))
            {
                _treeManager.AddToTree(simpleWorkItem);
            }
        }

        /// <summary>
        /// Stores information specific to themes.
        /// </summary>
        void StoreTheme(SimpleWorkItem simpleWorkItem)
        {
            // Some themes have shortcuts that should be used instead.
            var trueTheme = ThemeManager.GetTrueTheme(simpleWorkItem.Title);

            // Make this possibly already parsed title match the parsing conventions.
            var mainThemeName = GlobalManager.GetMainTheme(trueTheme);

            // Store its priority.
            try
            {
                _globalManager.ThemePriorities.Add(mainThemeName, simpleWorkItem.Priority);
            }
            catch (ArgumentException)
            {
                // TODO: Ignoring same name themes.
            }

            // Store its color.
            if (!_globalManager.MainThemeColors.TryAdd(mainThemeName, simpleWorkItem.MainThemeColor))
            {
                // TODO: Ignoring same name themes.
            }
        }

        /// <summary>
        /// Stores filter names into a collection.
        /// </summary>
        void StoreFilter(SimpleWorkItem simpleWorkItem)
        {
            foreach (var dataLevel in SimpleWorkItem.NonPbiLevels)
            {
                var dataKey = simpleWorkItem.GetKey(dataLevel);
                var group = _globalManager.Groupings.GetOrAdd(dataLevel, new ConcurrentDictionary<string, string>());
                group.TryAdd(dataKey, "");
            }
        }

        /// <summary>
        /// Stores team velocities into a collection.
        /// </summary>
        void StoreTeam(SimpleWorkItem simpleWorkItem)
        {
            _globalManager.TeamVelocities.TryAdd(simpleWorkItem.Title, simpleWorkItem.MeanVelocity);
        }

        /// <summary>
        /// Checks each filter level to see if this item should be ignored.
        /// </summary>
        internal bool IsNotIgnored(SimpleWorkItem simpleWorkItem)
        {
            // If there's no filter provided, it's not ignored.
            var filterCollection = _globalManager.FilterLevels;
            if (filterCollection == null)
            {
                return true;
            }

            // Check if any filter level's filter wants this item ignored.
            foreach (var filterLevel in SimpleWorkItem.NonPbiLevels)
            {
                // Get the filter level keys.
                HashSet<string> filterKeys;
                if (filterCollection.TryGetValue(filterLevel, out filterKeys))
                {
                    // Check if the item's associated key is not contained in the filter level keys.
                    var keyToCheck = simpleWorkItem.GetKey(filterLevel);
                    if (!filterKeys.Contains(keyToCheck))
                    {
                        return false;
                    }
                }
            }

            // It was not found in any of the filters.
            return true;
        }
    }
}