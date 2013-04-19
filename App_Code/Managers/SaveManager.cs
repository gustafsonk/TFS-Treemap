using System;
using System.IO;
using Objects;

namespace Managers
{
    /// <summary>
    /// Handles saving trees and retrieving saved trees.
    /// </summary>
    static class SaveManager
    {
        static volatile object _fileLock = new object();
        const string FileName = "C:\\treemap.txt";

        /// <summary>
        /// Appends a tree to the stored JSON of trees.
        /// </summary>
        internal static string SaveTree(Save save, bool overwrite)
        {
            lock (_fileLock)
            {
                // Read in the old stored JSON.
                var oldJson = ReadJson();

                // Handle the case where the name is already taken.
                try
                {
                    // Add the save to the old JSON string.
                    var newJson = JsonSaveManager.AddSave(oldJson, save, overwrite);

                    WriteJson(newJson);

                    return string.Empty; // Success.
                }
                catch (Exception exception)
                {
                    if (exception.Message == "exists")
                    {
                        return exception.Message;
                    }

                    throw; // Any other error should still throw.
                }
            }
        }

        /// <summary>
        /// Retrieves the JSON of saves.
        /// </summary>
        internal static string GetSaves()
        {
            lock (_fileLock)
            {
                return ReadJson();
            }
        }

        /// <summary>
        /// Shared method for reading from the file.
        /// </summary>
        static string ReadJson()
        {
            using (var fileReader = new StreamReader(FileName))
            {
                // Read the entire JSON string.
                var json = fileReader.ReadToEnd();

                // If the file is empty, use a blank JSON array.
                if (json == string.Empty)
                {
                    json = "[]";
                }

                return json;
            }
        }

        /// <summary>
        /// Shared method for writing to the file.
        /// </summary>
        static void WriteJson(string newJson)
        {
            using (var fileWriter = new StreamWriter(FileName))
            {
                // Store the new JSON string.
                fileWriter.Write(newJson);
            }
        }

        /// <summary>
        /// Removes a saved tree from the JSON of saves.
        /// </summary>
        internal static void DeleteSave(string name)
        {
            lock (_fileLock)
            {
                // Read in the old stored JSON.
                var oldJson = ReadJson();

                // Delete from the JSON string using the save name.
                var newJson = JsonSaveManager.DeleteSave(oldJson, name);

                WriteJson(newJson);
            }
        }
    }
}