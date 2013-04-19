using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Objects;

namespace Managers
{
    /// <summary>
    /// Sorts the data prior to object serialization.
    /// </summary>
    static class SortManager
    {
        /// <summary>
        /// Sort the collection in the proper manner.
        /// </summary>
        internal static Dictionary<string, ListDictionary> SortCollection(Dictionary<string, ListDictionary> collection, string treeLevel, string sortLevel)
        {
            switch (sortLevel)
            {
                case "Alphabet":
                    return collection
                        .OrderBy(kvp => kvp.Key)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                case "Effort":
                    return collection
                        .OrderByDescending(kvp => kvp.Value["effort"])
                        .ThenBy(kvp => kvp.Key) // Sort alphabetically if there is a tie.
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                case "Priority":
                    return collection
                        .OrderByDescending(kvp => kvp.Value["priority"])
                        .ThenBy(kvp => kvp.Key) // Sort alphabetically if there is a tie.
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                case "Product":
                    var missingTeamString = SimpleWorkItem.GetMissingString("Team");
                    var missingTeamCollection = collection.Where(kvp => kvp.Key == missingTeamString); // Reference the 'NO TEAM' grouping.
                    return collection
                        .Where(kvp => kvp.Key != missingTeamString) // Exclude 'NO TEAM' from sorting.
                        .OrderBy(kvp => kvp.Key != "Atlas")
                        .ThenBy(kvp => kvp.Key != "Anubis")
                        .ThenBy(kvp => kvp.Key != "Proteus")
                        .ThenBy(kvp => kvp.Key != "Shiva")
                        .ThenBy(kvp => kvp.Key != "Janus")
                        .ThenBy(kvp => kvp.Key != "Phoenix")
                        .ThenBy(kvp => kvp.Key != "Brahma")
                        .ThenBy(kvp => kvp.Key != "Loki")
                        .ThenBy(kvp => kvp.Key != "Heimdall")
                        .ThenBy(kvp => kvp.Key != "Vulcan")
                        .ThenBy(kvp => kvp.Key != "Athena")
                        .ThenBy(kvp => kvp.Key != "Ra")
                        .ThenBy(kvp => kvp.Key) // Any unmentioned teams above will be sorted alphabetically.
                        .Concat(missingTeamCollection) // Attach 'NO TEAM' back on.
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                case "Date":
                    const string futureString = "Future";
                    var missingString = SimpleWorkItem.GetMissingString(treeLevel);
                    var futureCollection = collection.Where(kvp => kvp.Key == futureString); // Reference the Future grouping.
                    var missingCollection = collection.Where(kvp => kvp.Key == missingString); // Reference the missing grouping.
                    return collection
                        .Where(kvp => kvp.Key != futureString && kvp.Key != missingString) // Exclude Future and missing from sorting.
                        .OrderBy(kvp => kvp.Key.Split('\\').ElementAtOrDefault(0, "Z")) // Sort by Year, using "Z" to place it last if it's missing.
                        .ThenBy(kvp => kvp.Key.Split('\\').ElementAtOrDefault(1, "Z")) // Sort by Quarter, using "Z" to place it last if it's missing.
                        .ThenBy(kvp => kvp.Key.Split('\\').ElementAtOrDefault(2, "Z")) // Sort by Sprint, using "Z" to place it last if it's missing.
                        .Concat(futureCollection) // Attach Future back on.
                        .Concat(missingCollection) // Attach missing back on.
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                default:
                    throw new Exception("Sorting type: " + sortLevel + ", is not supported.");
            }
        }

        /// <summary>
        /// LINQ extension method for overriding the default value when pulling from a collection.
        /// </summary>
        internal static string ElementAtOrDefault(this IEnumerable<string> list, int index, string @default)
        {
            return index >= 0 && index < list.Count() ? list.ElementAt(index) : @default;
        }
    }
}