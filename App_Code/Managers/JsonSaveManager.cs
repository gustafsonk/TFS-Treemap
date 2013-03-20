using System;
using System.Collections.Generic;
using System.Linq;
using App_Code.Objects;

namespace App_Code.Managers
{
    /// <summary>
    /// Creates JSON strings for saves.
    /// </summary>
    class JsonSaveManager
    {
        /// <summary>
        /// Adds a new save to the JSON string of existing saves if the save name is available.
        /// If overwrite is true, then it will overwrite the existing save instead of throwing an exception.
        /// </summary>
        internal static string AddSave(string oldJson, Save save, bool overwrite)
        {
            // Parse the old saves.
            var oldSaves = ParseSaves(oldJson);

            // Get the save name.
            var name = (string)save.Settings["data"];

            if (overwrite)
            {
                // Select all saves not sharing that save name, essentially deleting the save.
                oldSaves = oldSaves.Where(dictionary => (string)((Dictionary<string, object>)dictionary)["data"] != name).ToList();
            }
            else
            {
                // Check if the save name is taken and throw an exception instead of saving.
                var isNameTaken = oldSaves.Count(dictionary => (string)((Dictionary<string, object>)dictionary)["data"] == name) >= 1;
                if (isNameTaken)
                {
                    throw new Exception("exists");
                }
            }

            // Add the new save.
            oldSaves.Add(save.Settings);

            return SortAndSerializeSaves(oldSaves);
        }

        /// <summary>
        /// Removes a save from the JSON string of existing saves.
        /// </summary>
        internal static string DeleteSave(string oldJson, string name)
        {
            // Parse the old saves.
            var oldSaves = ParseSaves(oldJson);

            // Select all saves not sharing that save name, essentially deleting the save.
            oldSaves = oldSaves.Where(dictionary => (string)((Dictionary<string, object>)dictionary)["data"] != name).ToList();

            return SortAndSerializeSaves(oldSaves);
        }

        /// <summary>
        /// Parse the JSON string into a list of saves.
        /// </summary>
        static List<object> ParseSaves(string oldJson)
        {
            return (List<object>)fastJSON.JSON.Instance.Parse(oldJson);
        }

        /// <summary>
        /// Sorts and serializes the saves back to JSON.
        /// </summary>
        static string SortAndSerializeSaves(List<object> oldSaves)
        {
            // Sort the saves in alphabetical order.
            oldSaves = oldSaves.OrderBy(dictionary => ((Dictionary<string, object>)dictionary)["data"]).ToList();

            // Reserialize the saves.
            var saveJson = fastJSON.JSON.Instance.ToJSON(oldSaves);

            return saveJson;
        }
    }
}