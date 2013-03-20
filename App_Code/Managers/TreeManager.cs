using System;
using System.Collections.Concurrent;
using System.Linq;
using App_Code.Objects;

namespace App_Code.Managers
{
    /// <summary>
    /// Pulls information from PBIs into a singular tree structure.
    /// </summary>
    class TreeManager
    {
        readonly GlobalManager _globalManager;

        internal TreeManager(GlobalManager globalManager)
        {
            _globalManager = globalManager;
        }

        /// <summary>
        /// Adds the work item to the main nested collection, creating branches as necessary.
        /// </summary>
        internal void AddToTree(SimpleWorkItem simpleWorkItem)
        {
            // Start from the beginning of the collection and work down the tree levels.
            var currentTree = _globalManager.Tree;
            foreach (var treeLevel in _globalManager.TreeLevels)
            {
                // Should never happen.
                if (currentTree == null)
                {
                    throw new Exception("Tree was null.");
                }

                // Get the relevant data from this item for this tree level to use as a key.
                var itemKey = simpleWorkItem.GetKey(treeLevel);

                // Every item has a set of properties associated with it.
                var propertiesManager = new PropertiesManager(_globalManager);

                if (treeLevel == "PBI") // Final level reached, add the work item. This level is optional.
                {
                    propertiesManager.SetPbiProperties(simpleWorkItem);
                    currentTree.Add(itemKey, propertiesManager.Properties);
                }
                else // Get the appropriate collection, making a new one if necessary.
                {
                    if (!currentTree.ContainsKey(itemKey)) // This key's collection doesn't exist.
                    {
                        // Since it's new, add it to a grouping list for synchronizing size/color across a tree level.
                        var group = _globalManager.Groupings.GetOrAdd(treeLevel, new ConcurrentDictionary<string, string>());
                        group.TryAdd(itemKey, "");

                        // The last level won't have children so don't make a new list for children.
                        // This distinction is helpful in other places for determining if there are any children by checking if properties["children"] = null.
                        if (treeLevel != _globalManager.TreeLevels.Last())
                        {
                            propertiesManager.AddChildren();
                        }

                        // Some tree levels have specific properties.
                        propertiesManager.SetLevelProperties(treeLevel, itemKey);

                        // Now that properties are determined, add it to the collection.
                        currentTree.Add(itemKey, propertiesManager.Properties);
                    }
                    else // This key's collection already exists.
                    {
                        // Get the existing properties instead.
                        propertiesManager.Properties = currentTree[itemKey];
                    }

                    // Go a level deeper for the next iteration.
                    currentTree = propertiesManager.GetChildren();
                }

                // Add to the total effort for this grouping/item at this tree level.
                propertiesManager.AddEffort(simpleWorkItem.Effort);
            }
        }
    }
}