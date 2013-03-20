using System;
using System.Web.Mvc;
using App_Code.Managers;
using App_Code.Objects;

namespace App_Code.Controllers
{
    /// <summary>
    /// All threads (people requesting the page) begin here for accessing the data.
    /// </summary>
    public class RequestController : Controller
    {
        // POST because of possibility of exceeding GET character limit.
        [HttpPost]
        public string GetTree(string type, string color, string size, string sort, string filter)
        {
            return new RequestManager(type, color, size, sort, filter).GetData("Tree");
        }

        [HttpGet]
        public string GetFilter()
        {
            return new RequestManager().GetData("Filter");
        }

        [HttpPost]
        public string SaveTree(string name, string type, string color, string size, string sort, string filter, string velocity, string level, string label, string layout, string overwrite)
        {
            return RequestManager.SaveTree(new Save(name, type, color, size, sort, filter, velocity, level, label, layout), Convert.ToBoolean(overwrite));
        }

        [HttpGet]
        public string GetSaves()
        {
            return RequestManager.GetSaves();
        }

        [HttpPost]
        public void DeleteSave(string name)
        {
            RequestManager.DeleteSave(name);
        }

        // POST because of possibility of exceeding GET character limit.
        [HttpPost]
        public FileResult GetPng(string base64, string filename)
        {
            var bytes = Convert.FromBase64String(base64);
            return File(bytes, "image/png", filename);
        }
    }
}