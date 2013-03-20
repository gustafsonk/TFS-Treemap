<%@ Application Language="C#" %>
<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="System.Web.Routing" %>

<script runat="server">

    void Application_Start(object sender, EventArgs e) 
    {
        RegisterRoutes(RouteTable.Routes);
    }

    // Maps the controller to URLs.
    public static void RegisterRoutes(RouteCollection routes)
    {
        routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
        
        routes.MapRoute(
            "GetTree", // Route name
            "{controller}/{action}", // URL with parameters
            new { controller = "RequestController", action = "GetTree" } // Parameter defaults
        );

        routes.MapRoute(
            "GetFilter", // Route name
            "{controller}/{action}", // URL with parameters
            new { controller = "RequestController", action = "GetFilter" } // Parameter defaults
        );

        routes.MapRoute(
            "SaveTree", // Route name
            "{controller}/{action}", // URL with parameters
            new { controller = "RequestController", action = "SaveTree" } // Parameter defaults
        );

        routes.MapRoute(
            "GetSaves", // Route name
            "{controller}/{action}", // URL with parameters
            new { controller = "RequestController", action = "GetSaves" } // Parameter defaults
        );

        routes.MapRoute(
            "DeleteSave", // Route name
            "{controller}/{action}", // URL with parameters
            new { controller = "RequestController", action = "DeleteSave" } // Parameter defaults
        );
    }
    
    void Application_End(object sender, EventArgs e) 
    {
        //  Code that runs on application shutdown

    }
        
    void Application_Error(object sender, EventArgs e) 
    { 
        // Code that runs when an unhandled error occurs

    }

    void Session_Start(object sender, EventArgs e) 
    {
        // Code that runs when a new session is started

    }

    void Session_End(object sender, EventArgs e) 
    {
        // Code that runs when a session ends. 
        // Note: The Session_End event is raised only when the sessionstate mode
        // is set to InProc in the Web.config file. If session mode is set to StateServer 
        // or SQLServer, the event is not raised.

    }
       
</script>
