using System;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Managers
{
    /// <summary>
    /// Establishes connections to TFS 2010 and retrieves collections of work items.
    /// </summary>
    class ConnectionManager
    {
        readonly StorageManager _storageManager;

        const string CollectionUri = "http://tfsapps:8080/tfs/defaultcollection";

        internal ConnectionManager(GlobalManager globalManager)
        {
            _storageManager = new StorageManager(globalManager);
        }

        /// <summary>
        /// Handles pulling and storing data from TFS.
        /// </summary>
        internal void PrepareWorkItems(string type)
        {
            // Retrieve the particular collection of work items.
            var workItemCollection = GetWorkItems(type);

            // For each work item, find the relevant information and store it in useful collections.
            foreach (WorkItem workItem in workItemCollection)
            {
                // Pull out the information we need into a simplified object.
                var simpleWorkItem = ItemManager.BuildSimpleWorkItem(workItem, workItemCollection.Query.DisplayFieldList);

                // Store it in its proper location.
                _storageManager.StoreWorkItem(type, simpleWorkItem);
            }
        }

        /// <summary>
        /// Finds a stored query in TFS, runs it, and returns the results.
        /// </summary>
        static WorkItemCollection GetWorkItems(string type)
        {
            // Find in Url property in Properties window when query is highlighted in Team Explorer window.
            string queryGuid;
            switch (type)
            {
                case "PBIs":
                case "Filters": // Use the PBI collection for determining filters.
                    queryGuid = "0d795575-d87c-461d-9292-1ba60cbfbf5d";
                    break;
                case "Themes":
                    queryGuid = "3f8df213-6356-4da1-9d02-8a32b4b07afc";
                    break;
                case "Teams":
                    queryGuid = "9a4dbef0-2f60-4504-9159-a9e62ab8738b";
                    break;
                default:
                    throw new Exception("Work item type: " + type + ", is not supported.");
            }

            // Retrieve the stored query.
            var projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(CollectionUri));
            var workItemStore = projectCollection.GetService<WorkItemStore>();
            var query = workItemStore.GetQueryDefinition(new Guid(queryGuid));

            // Run the query and store the results.
            var workItemCollection = workItemStore.Query(query.QueryText);

            return workItemCollection;
        }
    }
}