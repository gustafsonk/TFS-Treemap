VISUAL STUDIO 2010 SETUP (for development):
1. File > New > Web Site...
2. Pick ASP.NET Empty Web Site.
3. Place all files into this project, replacing web.config.
4. Right-click Treemap.cshtml in the Solution Explorer window and select Set As Start Page.
5. Run the project using Ctrl+F5, or F5 for debugging.

TFS QUERY SETUP (for development and/or deployment):
---PBIs---
1. Connect to TFS, select a project, select Work Items, select Team Queries, and create a new folder called Treemap (this is simply for organization).
2. Right-click this folder and select New Query.
3. Change the value '@Project' in the first row to the relevant project's name, which currently is 'Advantage'.
4. Make the 2nd row state 'And Work Item Type = Product Backlog Item'.
5. Make the 3rd row state 'And State <> Removed'.
6. Add any additional And clauses as desired to ignore work items, such as for Iteration Path and Area Path.
7. Save the query as 'PBIs' and place it under the Treemap folder.
8. Double-click the query and click the Column Options button near the top.
9. Make the Selected columns list have the following columns: ID, Work Item Type, Title, State, Effort, Backlog Priority, Program Theme (special field, see section under Work Item Setup), Product (same as Program Theme), Node Name, Iteration Path.

---Themes---
1. Repeat the Steps 1-8 above, replacing 'Product Backlog Item' with 'Theme' and 'PBIs' with 'Themes'.
2. Make the Selected columns list have the following columns: ID, Work Item Type, Title, Backlog Priority, Description HTML (for scraping colors).

---Adding to the Project---
1. Highlight the PBIs query with a single click and find the Url property in the Properties window.
2. Copy the string of numbers and dashes at the end of the Url property after the last forward slash. This is the query's GUID.
3. Open App_Code\Controllers\RequestController.cs, look under the ConnectionManager class, and replace the existing GUID for the "PBI" case with your GUID.
4. Repeat Steps 1-3 for the Themes query, replacing the existing GUID for the "Theme" case.

---Supporting multiple team projects (optional)---
1. Right-click the PBIs query and select Edit Query.
2. Using the column to the left of the And/Or column, select every row in this query by clicking the first row, holding down the Shift key, and clicking the last row.
3. Right-click this column and select Group Clauses.
4. Add a new clause to the bottom of the query so that it states 'Or Team Project = your-other-team-project-name', replacing the value with your appropriate project name. Note the 'Or'.
5. Make the next row state 'And Work Item Type = Product Backlog Item'.
6. Make the next row state 'And State <> Removed'.
7. Do Step 6 in the initial PBIs query setup here as well.
8. Do Step 2 in this setup, selecting only the newly created rows instead of every row in this query.

SERVER SETUP (for deployment, this assumes IIS has already been initially configured on the machine):
---Configuring the Site---
1. Install the necessary libraries needed for reference. I did this by installing Visual Studio 2010 Ultimate and the ASP.NET MVC3 update.
2. Make the directory C:\ProgramData\Microsoft\Team Foundation\3.0\Cache. The ProgramData folder is hidden on a default Windows install.
3. Right-click the newly created Cache folder and select Properties.
4. Go to the Security tab, click Edit..., and give the Users group the permission of Full Control.
5. Create a folder to place Treemap's project code, such as C:\treemap, and place the entire Treemap project code here.
6. Open the Internet Information Services (IIS) Manager in Windows and select the server > Sites > Default Web Site.
7. Right-click Default Web Site and select Add Application....
8. Enter 'treemap' for the Alias field, set the Application pool field to ASP.NET v4.0, set the Physical path field to the location of the project code, such as C:\treemap, and click OK.
9. The project should now be accessible at http://your-server's-ip/treemap/Treemap.cshtml if everything is networked and configured correctly.

---Making a Better URL (optional)---
1. Repeat Step 6 above.
2. Under the IIS grouping, double-click the HTTP Redirect option.
3. Checkmark all the boxes, set the URL field to http://your-server's-ip/treemap/Treemap.cshtml, set the status code field to Permanent (301).
4. Click Apply in the top-right corner.
5. Select the treemap application listed under Default Web Site and repeat Step 2 in this setup.
6. Make sure the first box is unchecked and repeat Step 4 in this setup.
7. The project should now be accessible at http://your-server's-ip, or http://your-server's-name once its domain name has propagated.

---Enabling tree saving (optional)---
1. Create C:\treemap.txt. This can be changed in App_Code/Managers/SaveManager.cs.
2. Right-click on the file and select Properties.
3. Repeat Step 4 in the Configuring the Site section above.

TFS SECURITY SETUP (for developing and/or deployment):
---Query Access---
1. Right-click the Treemap folder in the Team Explorer window that was created in the TFS query setup and select Security....
2. Select Windows User or Group in the Add users and groups grouping and click the Add... button.
3. Click the Object Types... button, checkmark the Computers option, and close this window.
4. Change the Location if needed and enter the name of the server in the field.
5. Click the Check Names button and click OK when it's successful.
6. Give the newly added server the Read permission and close this window.

---Area/Iteration Access (repeat for each team project involved)---
1. Right-click the relevant team project in the Team Explorer window, and select Team Project Settings > Areas and Iterations....
2. In the Area tab, select the highest node, Area, and click the Security... button.
3. Repeat Steps 2-5 in the Query Access setup here as well.
4. Give the newly added server the View this node and View work items in this node permissions.
5. Close this window and select the Iteration tab in the previous window.
6. Now repeat Steps 2-4 in this setup here as well, with the highest node being Iteration and without the 2nd permission.

WORK ITEM SETUP (for reading in Theme data):
Option 1: (replacing the 'Program Theme' field mentioned in the PBIs query setup)
1. On Step 9 of the PBIs query setup, use any field of your choosing to represent the PBI theme.
2. In App_Code\Managers\ItemManager.cs, find and replace 'Program Theme' with the name of the field you chose.

Option 2: (used by Panoptix)
---Creating a 'Program Theme' field---
1. Install TFS Power Tools for Visual Studio, particularly for the Process Editor portion included.
2. Go to Tools > Process Editor > Work Item Types > Open WIT from Server.
3. Select the server to connect to, select the relevant team project, and select the Product Backlog Item work item type.
4. With the Fields tab selected, click the New button.
5. Enter 'Program Theme' in the Name field.
6. Enter a unique value for the Reference Name field, something like CompanyName.VSTS.ProgramTheme would be thematically consistent with Microsoft.
7. Set the Reportable field to 'Dimension' and click OK.
8. Now go to the Layout tab, right-click one of the many Column elements on the left where you would like to place this new field, and select New Control.
9. In the Field Name field of this new control, select the Reference Name value of the field you just created.
10. Change the Label field value to 'Program Theme:' for consistency, and reposition the control by right-clicking it on the left and selecting Move Up or Move Down.
11. Saving the file should automatically upload it to the server and should show up for everyone using TFS very shortly.
12. If you would like this change to the Product Backlog Item work item type to be shared across multiple team projects, repeat Steps 2-3 in this setup, selecting Export WIT instead of Open WIT from Server.
13. After selecting a file location, select No when asked to include the global list definition.
14. Now repeat Steps 2-3, selecting Import WIT instead of Open WIT from Server, and selecting the file you just exported.

---Adding Work Item Types to TFS (optional, for reading in Theme priorities or Team mean velocities)---
1. Go to File > Open > File... and select the Theme.xml file or Team.xml file that is in the root directory of the source code.
2. Make any edits to this form as necessary, primarily removing and renaming fields and controls to the fields you would like to use.
3. Save it locally and repeat Steps 1-2 above, selecting Import WIT instead of Open WIT from Server.
4. Select the server, the team project, and the file you just edited and click OK. It should show up for everyone using TFS very shortly.