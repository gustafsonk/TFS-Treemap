using System.Collections.Generic;
using System.Linq;

namespace Managers
{
    /// <summary>
    /// Creates JSON strings for filters.
    /// </summary>
    class JsonFilterManager
    {
        /// <summary>
        /// Converts the grouping collection to JSON for using it as a filter.
        /// </summary>
        internal static string ConvertFiltersToJson(Dictionary<string, List<string>> filterLevels)
        {
            // Build the filter levels.
            var filtersToConvert = new List<Filter>();
            foreach (var filterLevel in filterLevels)
            {
                // Determine the filter level's name and children.
                var filterName = filterLevel.Key;
                var filterChildren = filterLevel.Value.Select(childName => new Filter { data = childName }).ToList();

                // Generate the list of filters for conversion.
                Filter filterToAdd;
                if (filterName == "Theme")
                {
                    // Cluster the theme filters by their main theme name.
                    var mainThemeFilters = new List<Filter>();
                    foreach (var filterChild in filterChildren)
                    {
                        // Determine this child's main theme.
                        var childMainTheme = GlobalManager.GetMainTheme(filterChild.data);

                        // See if a child collection already exists.
                        var index = mainThemeFilters.FindIndex(mainThemeFilter => mainThemeFilter.data == childMainTheme);
                        if (index == -1)
                        {
                            // This main theme is new.
                            mainThemeFilters.Add(new Filter { data = childMainTheme, children = new List<Filter> { filterChild } });
                        }
                        else
                        {
                            // This main theme was already added before.
                            mainThemeFilters[index].children.Add(filterChild);
                        }
                    }

                    // Add these main theme filter collections to the theme filter level.
                    filterToAdd = new Filter { data = filterName, children = mainThemeFilters };
                }
                else
                {
                    // Add the filter names to this filter level.
                    filterToAdd = new Filter { data = filterName, children = filterChildren };
                }

                // Add this filter level to the list of filter levels.
                filtersToConvert.Add(filterToAdd);
            }

            // Serialize our filter list into a JSON string.
            var json = App_Code.fastJSON.JSON.Instance.ToJSON(filtersToConvert);

            return json;
        }
    }
}