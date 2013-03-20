using System.Collections.Generic;

namespace App_Code.Objects
{
    /// <summary>
    /// Represents save data compatible with jsTree.
    /// </summary>
    class Save
    {
        internal Dictionary<string, object> Settings;

        internal Save(string name, string type, string color, string size, string sort, string filter, string velocity, string level, string label, string layout)
        {
            // Build the collection of save settings.
            var settings = new Dictionary<string, string> 
            { { "type", type }, { "color", color }, { "size", size }, { "sort", sort }, 
            { "filter", filter }, { "velocity", velocity },
            { "level", level }, { "label", label }, { "layout", layout } };

            // Store the save settings.
            Settings = new Dictionary<string, object> { { "data", name }, { "metadata", settings } };
        }
    }
}