using System;
using App_Code.Objects;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace App_Code.Managers
{
    /// <summary>
    /// Turns a TFS WorkItem into a SimpleWorkItem.
    /// </summary>
    static class ItemManager
    {
        /// <summary>
        /// Stores a pre-defined list of properties from work items into a simple object.
        /// </summary>
        static internal SimpleWorkItem BuildSimpleWorkItem(WorkItem workItem, DisplayFieldList displayFieldList)
        {
            var simpleWorkItem = new SimpleWorkItem();
            foreach (FieldDefinition fieldDefinition in displayFieldList)
            {
                // Each case needs the field's value.
                var fieldValue = GetFieldValue(workItem, fieldDefinition.Name);

                // Modify properties here before storing them if they aren't in a desirable format, such as replacing team codes with team names.
                switch (fieldDefinition.Name)
                {
                    case "Effort":
                        double effort;
                        double.TryParse(fieldValue, out effort);
                        simpleWorkItem.Effort = effort;
                        break;
                    case "ID":
                        int id;
                        int.TryParse(fieldValue, out id);
                        simpleWorkItem.Id = id;
                        break;
                    case "Backlog Priority":
                        double priority;
                        double.TryParse(fieldValue, out priority);
                        simpleWorkItem.Priority = priority;
                        break;
                    case "Iteration Path":
                        // Remove the project name, or use a constant if it's solely the project name.
                        var quarter = fieldValue.Contains("\\") ? fieldValue.Substring(fieldValue.IndexOf('\\') + 1) : SimpleWorkItem.GetMissingString("Quarter");
                        var sprint = fieldValue.Contains("\\") ? fieldValue.Substring(fieldValue.IndexOf('\\') + 1) : SimpleWorkItem.GetMissingString("Sprint");

                        // Remove the sprint part for determining quarter, if needed.
                        quarter = quarter.Contains("Sprint") ? quarter.Substring(0, quarter.LastIndexOf('\\')) : quarter;
                        simpleWorkItem.Quarter = quarter;

                        // Normally would remove the quarter/year part for determining sprint here, but I need that for sorting by date.
                        simpleWorkItem.Sprint = sprint;
                        break;
                    case "State":
                        simpleWorkItem.State = fieldValue;
                        break;
                    case "Node Name":
                        simpleWorkItem.Team = fieldValue;
                        switch (simpleWorkItem.Team)
                        {
                            // Some hardcoded team names in TFS.
                            case "Advantage":
                            case "Identity":
                                simpleWorkItem.Team = SimpleWorkItem.GetMissingString("Team");
                                break;
                            case "STL":
                                simpleWorkItem.Team = "Vulcan";
                                break;
                            case "T1":
                                simpleWorkItem.Team = "Loki";
                                break;
                            case "T2":
                                simpleWorkItem.Team = "Anubis";
                                break;
                            case "T3":
                                simpleWorkItem.Team = "Janus";
                                break;
                            case "TC":
                                simpleWorkItem.Team = "Atlas";
                                break;
                            case "TS":
                                simpleWorkItem.Team = "Athena";
                                break;
                        }
                        break;
                    case "Product":
                        simpleWorkItem.Product = fieldValue == string.Empty ? SimpleWorkItem.GetMissingString("Product") : fieldValue;
                        break;
                    case "Program Theme":
                        simpleWorkItem.Theme = fieldValue == string.Empty ? SimpleWorkItem.GetMissingString("Theme") : fieldValue;
                        break;
                    case "Title":
                        simpleWorkItem.Title = fieldValue;
                        break;
                    case "Work Item Type":
                        simpleWorkItem.Type = fieldValue;
                        break;
                    case "Description HTML": // Not PBI-related, used for main theme coloring.
                        var color = ThemeManager.ParseHtmlForColor(fieldValue);
                        simpleWorkItem.MainThemeColor = color;
                        break;
                    case "Mean Velocity": // Not PBI-related, used for teams.
                        double velocity;
                        double.TryParse(fieldValue, out velocity);
                        simpleWorkItem.MeanVelocity = velocity;
                        break;
                }
            }

            // Themes are currently the only optional field for the PBIs query so if it's not an included column, assign it the missing theme value.
            if (simpleWorkItem.Theme == null)
            {
                simpleWorkItem.Theme = SimpleWorkItem.GetMissingString("Theme");
            }

            return simpleWorkItem;
        }

        /// <summary>
        /// Handles null values as well as missing field values on work items that don't support the field.
        /// </summary>
        static string GetFieldValue(WorkItem workItem, string fieldDefinitionName)
        {
            try
            {
                return workItem[fieldDefinitionName].ToString();
            }
            catch (Exception exception)
            {
                if (exception is NullReferenceException || exception is FieldDefinitionNotExistException)
                {
                    return string.Empty;
                }
                throw; // A different exception should still be thrown.
            }
        }
    }
}