namespace App_Code.Managers
{
    /// <summary>
    /// Main class that begins and finishes the process of building representative JSON strings.
    /// </summary>
    class BuildManager
    {
        readonly GlobalManager _globalManager;

        readonly ConnectionManager _connectionManager;
        readonly ColorManager _colorManager;
        readonly SizeManager _sizeManager;
        readonly JsonTreeManager _jsonTreeManager;

        internal BuildManager(GlobalManager globalManager)
        {
            _globalManager = globalManager;
            _connectionManager = new ConnectionManager(globalManager);
            _colorManager = new ColorManager(globalManager);
            _sizeManager = new SizeManager(globalManager);
            _jsonTreeManager = new JsonTreeManager(globalManager);
        }

        /// <summary>
        /// Begins the tree building process.
        /// </summary>
        /// <returns>A JSON string representing the tree.</returns>
        internal string BuildTree()
        {
            // Get the theme priorities/colors if needed.
            if (_globalManager.TreeLevels.Contains("Theme"))
            {
                _connectionManager.PrepareWorkItems("Themes");
            }

            // Get the mean velocities for the teams if needed.
            if (_globalManager.TreeLevels.Contains("Team"))
            {
                _connectionManager.PrepareWorkItems("Teams");
            }

            // Retrieve and store the collection in a logically nested format.
            _connectionManager.PrepareWorkItems("PBIs");

            // Align grouping contents by adding 'missing' grouping siblings.
            if (_globalManager.SizeLevels.Contains("Aligning"))
            {
                _sizeManager.AlignGroupings();
            }

            // Generate colors to be used for the groupings.
            _colorManager.AssignGroupColors();

            // Serialize the collection to JSON and return it.
            var json = _jsonTreeManager.ConvertTreeToJson();
            return json;
        }

        /// <summary>
        /// Determines the available filters for use.
        /// </summary>
        /// <returns>A JSON string representing the available filters.</returns>
        internal string BuildFilter()
        {
            _connectionManager.PrepareWorkItems("Filters");
            return new FilterManager(_globalManager).GetFilterJson();
        }
    }
}