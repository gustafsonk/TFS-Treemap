using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace App_Code.Managers
{
    /// <summary>
    /// Handles determining and using filters.
    /// </summary>
    class FilterManager
    {
        readonly GlobalManager _globalManager;

        internal FilterManager(GlobalManager globalManager)
        {
            _globalManager = globalManager;
        }

        /// <summary>
        /// Parses the filter parameter to determine what should be filtered.
        /// </summary>
        /// <param name="unparsedFilter">An unparsed string received from the AJAX request.</param>
        internal static Dictionary<string, HashSet<string>> GetLevelFilters(string unparsedFilter)
        {
            var filterLevels = new Dictionary<string, HashSet<string>>();

            // Parse the filter string into levels.
            var unparsedFilterLevels = unparsedFilter.Split(new[] { "\t-\t" }, StringSplitOptions.None);
            foreach (var unparsedFilterLevel in unparsedFilterLevels)
            {
                // Parse the filter level string into meaningful collections.
                var filterTypeAndNames = unparsedFilterLevel.Split('\t');
                var filterType = filterTypeAndNames.First();
                var filterNames = filterTypeAndNames.Where(name => name != filterType);

                // Add each filter name to a set of filter names.
                var filterSet = new HashSet<string>();
                foreach (var filterName in filterNames)
                {
                    filterSet.Add(filterName);
                }

                // Add each filter type to the filter collection.
                filterLevels.Add(filterType, filterSet);
            }

            return filterLevels;
        }

        /// <summary>
        /// Gets the name of every item that can be filtered on.
        /// </summary>
        /// <returns>A JSON string representing available filters.</returns>
        internal string GetFilterJson()
        {
            var groupingsToConvert = new Dictionary<string, List<string>>();
            foreach (var groupingType in _globalManager.Groupings)
            {
                // For each grouping type, sort each collection of groupings specifically.
                var unsortedGroupType = groupingType.Value.ToDictionary<KeyValuePair<string, string>, string, ListDictionary>(group => group.Key, group => null);
                Dictionary<string, ListDictionary> sortedGroupType; // TODO: reusing this collection type for convenience with SortManager.
                switch (groupingType.Key)
                {
                    case "Quarter":
                    case "Sprint":
                        sortedGroupType = SortManager.SortCollection(unsortedGroupType, groupingType.Key, "Date");
                        break;
                    case "Team":
                    case "Product":
                    case "Theme":
                        sortedGroupType = SortManager.SortCollection(unsortedGroupType, groupingType.Key, "Alphabet");
                        break;
                    default:
                        throw new Exception("Work item type: " + groupingType.Key + ", is not supported.");
                }

                // For each grouping type, add the list of groupings to a collection for conversion.
                var groupKeys = sortedGroupType.Keys.ToList();
                groupingsToConvert.Add(groupingType.Key, groupKeys);
            }

            // Sort the grouping types alphabetically.
            groupingsToConvert = groupingsToConvert.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Convert the collection using the JSON serializer and return it.
            var json = JsonFilterManager.ConvertFiltersToJson(groupingsToConvert);
            return json;
        }
    }
}