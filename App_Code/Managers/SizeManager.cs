using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace App_Code.Managers
{
    /// <summary>
    /// Sizes tree nodes.
    /// </summary>
    class SizeManager
    {
        readonly GlobalManager _globalManager;

        internal SizeManager(GlobalManager globalManager)
        {
            _globalManager = globalManager;
        }

        /// <summary>
        /// Aligns groupings in a level by adding 'missing' groupings found under its siblings.
        /// </summary>
        internal void AlignGroupings()
        {
            RecursiveAlign(_globalManager.Tree, 1);
        }

        /// <summary>
        /// Recursively works through the entire tree, aligning parent nodes by adding dummy child nodes.
        /// </summary>
        void RecursiveAlign(Dictionary<string, ListDictionary> tree, int childLevel)
        {
            // Don't bother iterating if there's no point.
            if (childLevel >= _globalManager.TreeLevels.Count)
            {
                return;
            }

            // Now that the tree is built, reiterate through it to find the 'missing' groupings.
            foreach (var keyValuePair in tree)
            {
                // Get the child groupings. Stop if there isn't any.
                var childGroupings = keyValuePair.Value["children"] as Dictionary<string, ListDictionary>;
                if (childGroupings == null)
                {
                    return;
                }

                // If this level should be aligned, do so.
                var childSizeLevel = _globalManager.SizeLevels[childLevel];
                if (childSizeLevel == "Aligning")
                {
                    // Get the expected groupings for this level.
                    var treeLevel = _globalManager.TreeLevels[childLevel];
                    ConcurrentDictionary<string, string> expectedGroupings;
                    _globalManager.Groupings.TryGetValue(treeLevel, out expectedGroupings);
                    foreach (var expectedGroupingKey in expectedGroupings.Keys)
                    {
                        // Check if it's a missing grouping.
                        if (!childGroupings.ContainsKey(expectedGroupingKey))
                        {
                            // Set any properties of this new grouping.
                            var propertiesManager = new PropertiesManager(_globalManager);

                            // Make this dummy grouping have no effort.
                            propertiesManager.AddEffort(0);

                            // The children tree is needed when multiple levels have "Aligning".
                            propertiesManager.AddChildren();

                            // Some tree levels have specific properties.
                            propertiesManager.SetLevelProperties(treeLevel, expectedGroupingKey);

                            // Add the new grouping to its parent.
                            childGroupings.Add(expectedGroupingKey, propertiesManager.Properties);
                        }
                    }
                }

                // Do the same for each child grouping.
                RecursiveAlign(childGroupings, childLevel + 1);
            }
        }
    }
}