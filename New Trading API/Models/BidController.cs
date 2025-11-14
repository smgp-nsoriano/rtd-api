using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace New_Trading_API.Models
{
    public class BidController : Controller
    {
        // GET: Bid
        public ActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public JsonResult GetFile(string BidType, string UnitID)
        {
            string message = "";

            message = GlobalFunctions.SaveFile(BidType, UnitID);

            return Json(message, JsonRequestBehavior.AllowGet);
        }

    }
}