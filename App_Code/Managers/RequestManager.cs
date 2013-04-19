using Objects;

namespace Managers
{
    /// <summary>
    /// Handles data requests from RequestController and creates the global storage for shared variables and methods.
    /// </summary>
    class RequestManager
    {
        readonly GlobalManager _globalManager;

        /// <summary>
        /// Filter constructor.
        /// </summary>
        internal RequestManager()
        {
            _globalManager = new GlobalManager();
        }

        /// <summary>
        /// Tree constructor.
        /// </summary>
        internal RequestManager(string type, string color, string size, string sort, string filter)
        {
            _globalManager = new GlobalManager();
            ParseParameters(type, color, size, sort, filter);
        }

        /// <summary>
        /// Parses the tree building parameters.
        /// </summary>
        void ParseParameters(string type, string color, string size, string sort, string filter)
        {
            // Initialize data level collections for reference.
            if (!string.IsNullOrWhiteSpace(type))
            {
                _globalManager.TreeType = type;
                _globalManager.TreeLevels = type.Split(GlobalManager.LevelSplitter);
            }

            // Some preliminary checks.
            if (!string.IsNullOrWhiteSpace(color))
            {
                _globalManager.ColorLevels = color.Split(GlobalManager.LevelSplitter);
            }
            if (!string.IsNullOrWhiteSpace(size))
            {
                _globalManager.SizeLevels = size.Split(GlobalManager.LevelSplitter);
            }
            if (!string.IsNullOrWhiteSpace(sort))
            {
                _globalManager.SortLevels = sort.Split(GlobalManager.LevelSplitter);
            }
            if (!string.IsNullOrWhiteSpace(filter))
            {
                _globalManager.FilterLevels = FilterManager.GetLevelFilters(filter);
            }
        }

        /// <summary>
        /// Removes C#-related data from JSON serializing.
        /// </summary>
        static void ConfigureFastJson()
        {
            // Turn off C#-related data since I only care about properties and values.
            // The current version being used is a slightly modified 2.0.13, search files for '!TREEMAP'.
            App_Code.fastJSON.JSON.Instance.Parameters.UseExtensions = false;

            // No need to JSONify unused values.
            App_Code.fastJSON.JSON.Instance.Parameters.SerializeNullValues = false;
        }

        /// <summary>
        /// Requests an appropriate JSON string.
        /// </summary>
        internal string GetData(string dataType)
        {
            // Return empty when no data is selected.
            if (dataType == "Tree" && _globalManager.TreeType == null)
            {
                return "[]";
            }

            // Prepare the JSON serializer.
            ConfigureFastJson();

            // Build the appropriate JSON string and return it.
            var buildManager = new BuildManager(_globalManager);
            var json = dataType == "Tree" ? buildManager.BuildTree() : buildManager.BuildFilter();
            return json;
        }

        /// <summary>
        /// Requests a tree's state to be saved.
        /// </summary>
        internal static string SaveTree(Save save, bool overwrite)
        {
            // Prepare the JSON serializer.
            ConfigureFastJson();

            // Request a save.
            return SaveManager.SaveTree(save, overwrite);
        }

        /// <summary>
        /// Requests a JSON list of saved trees.
        /// </summary>
        internal static string GetSaves()
        {
            return SaveManager.GetSaves();
        }

        /// <summary>
        /// Requests a saved tree to be removed.
        /// </summary>
        internal static void DeleteSave(string name)
        {
            SaveManager.DeleteSave(name);
        }
    }
}