using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Objects;

namespace Managers
{
    /// <summary>
    /// Creates JSON strings for trees.
    /// </summary>
    class JsonTreeManager
    {
        readonly GlobalManager _globalManager;

        internal JsonTreeManager(GlobalManager globalManager)
        {
            _globalManager = globalManager;
        }

        /// <summary>
        /// Converts the tree collection to a JSON string.
        /// </summary>
        internal string ConvertTreeToJson()
        {
            // Make initial node.
            var root = new Node { children = new List<Node>(), data = new Data(1f, "#FFFFFF", 0), id = _globalManager.TreeLevels.Count.ToString(CultureInfo.InvariantCulture), name = GetCollectionTitle() };

            // Recursively build our JSON objects.
            RecursiveBuild(SortManager.SortCollection(_globalManager.Tree, _globalManager.TreeLevels[0], _globalManager.SortLevels[0]), root, "");

            // Serialize the nested collection into a JSON string.
            var json = App_Code.fastJSON.JSON.Instance.ToJSON(root);

            return json;
        }

        /// <summary>
        /// Recursively navigates the tree to build a JSON string.
        /// </summary>
        /// <param name="tree">The parent tree.</param>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="indexPath">Used for creating IDs for nodes.</param>
        void RecursiveBuild(Dictionary<string, ListDictionary> tree, Node parentNode, string indexPath)
        {
            var index = 0;
            foreach (var treeLevel in tree)
            {
                // Determine the current level.
                const char levelSplitter = GlobalManager.LevelSplitter;
                var currentLevel = indexPath.Count(character => character == levelSplitter);
                var groupLevel = _globalManager.TreeLevels[currentLevel];

                // Determine the node's ID.
                var idPrefix = GetIdPrefix(groupLevel);
                var newIndexPath = indexPath + levelSplitter + index++;
                var nodeId = idPrefix + newIndexPath;

                // Determine the node's name.
                var groupKey = treeLevel.Key;
                string nodeName;
                switch (groupLevel)
                {
                    case "Sprint": // Needs to happen here for sorting by key purposes before this.
                        var sprintIndex = groupKey.IndexOf("Sprint", StringComparison.Ordinal);
                        nodeName = sprintIndex == -1 ? groupKey : groupKey.Substring(sprintIndex);
                        break;
                    case "Quarter":
                    case "Team":
                    case "Product":
                    case "Theme":
                        nodeName = groupKey;
                        break;
                    case "PBI": // Cut off the PBI's ID # that was added to guarantee uniqueness.
                        nodeName = groupKey.Substring(0, groupKey.LastIndexOf(levelSplitter));
                        break;
                    default:
                        throw new Exception("Work item type: " + groupLevel + ", is not supported.");
                }

                // Determine the node's effort.
                var effort = (double)treeLevel.Value["effort"];

                // Determine the node's size.
                double size;
                var sizeType = _globalManager.SizeLevels[currentLevel];
                if (sizeType == "Effort")
                {
                    size = effort == 0 ? 1 : effort;
                }
                else // "Aligning" and "Nothing".
                {
                    size = 1;
                }

                // Determine the node's color.
                var color = "";
                if (groupLevel != "PBI" && groupLevel != "Theme") // PBIs and Themes have special color systems.
                {
                    color = _globalManager.GetColor(groupLevel, groupKey);
                }

                // Make the appropriate data object.
                Data nodeData;
                switch (groupLevel)
                {
                    case "PBI":
                        var pbi = (SimpleWorkItem)treeLevel.Value["simpleWorkItem"];
                        color = GlobalManager.GetPbiColor(pbi);

                        nodeData = new PbiData(size, color, pbi.Effort, pbi.Id, pbi.Priority, pbi.State);
                        break;
                    case "Theme":
                        var mainTheme = (string)treeLevel.Value["mainTheme"];
                        color = _globalManager.GetMainThemeColor(mainTheme);
                        var priority = _globalManager.GetMainThemePriority(mainTheme);

                        nodeData = new ThemeData(size, color, effort, priority);
                        break;
                    case "Team":
                        var velocity = (double)treeLevel.Value["velocity"];

                        nodeData = new TeamData(size, color, effort, velocity);
                        break;
                    case "Product":
                    case "Sprint":
                    case "Quarter":
                        nodeData = new Data(size, color, effort);
                        break;
                    default:
                        throw new Exception("Work item type: " + groupLevel + ", is not supported.");
                }

                // Build the node and add it to the parent node.
                var childNode = new Node { children = new List<Node>(), data = nodeData, id = nodeId, name = nodeName };
                parentNode.children.Add(childNode);

                // Continue recursion if there are more nested lists.
                var unsortedChildren = treeLevel.Value["children"] as Dictionary<string, ListDictionary>;
                if (unsortedChildren != null && unsortedChildren.Count > 0) // Count check is needed when multiple levels have "Aligning".
                {
                    var nextLevel = currentLevel + 1;
                    var sortedChildren = SortManager.SortCollection(unsortedChildren, _globalManager.TreeLevels[nextLevel], _globalManager.SortLevels[nextLevel]);
                    RecursiveBuild(sortedChildren, childNode, newIndexPath);
                }
            }
        }

        /// <summary>
        /// Creates the title for the top-level node in the tree.
        /// </summary>
        string GetCollectionTitle()
        {
            return _globalManager.TreeLevels.Aggregate("", (current, treeLevel) => (current == "" ? current : current + "/") + treeLevel) + " View";
        }

        /// <summary>
        /// Used for making a compact identifier for each node in the tree.
        /// </summary>
        static string GetIdPrefix(string groupLevel)
        {
            switch (groupLevel)
            {
                case "Quarter":
                    return "q";
                case "Sprint":
                    return "s";
                case "Team":
                    return "t";
                case "Product":
                    return "p";
                case "Theme":
                    return "h";
                case "PBI":
                    return "b";
                default:
                    throw new Exception("Work item type: " + groupLevel + ", is not supported.");
            }
        }
    }
}