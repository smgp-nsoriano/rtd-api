using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.PI;
using OSIsoft.AF.Collective;
using OSIsoft.AF.Time;
using OSIsoft.AF.Data;
using New_Trading_API.Models;
using System.Data;
using System.Web;
using System.Data.SqlClient;

namespace New_Trading_API.Controllers
{
    public class ManualEntryController : ApiController
    {
        [HttpPost]
        [Authorize]
        public IHttpActionResult RTDValue([FromBody] List<ManualEntry> values)
        {
            string message = "", status = "";
            try
            {
                PIServers piServers = new PIServers();
                PIServer piServer = piServers[GlobalFunctions.piServerName];
                IList<AFValue> valuesToWrite = new List<AFValue>();

                foreach (var val in values)
                {
                    PIPoint pt = PIPoint.FindPIPoint(piServer, val.RTDTagName);
                    PIPoint ptPrice = PIPoint.FindPIPoint(piServer, val.PriceTagName);
                    PIPoint ptHap = PIPoint.FindPIPoint(piServer, val.HAPTagName);

                    AFTime time = new AFTime(val.TimeStamp);
                    AFValue afValue = new AFValue(val.Value, time);
                    AFValue priceValue = new AFValue(val.PriceValue, time);
                    AFValue hapValue = new AFValue(val.Value, time);

                    afValue.PIPoint = pt;
                    priceValue.PIPoint = ptPrice;
                    hapValue.PIPoint = ptHap;

                    valuesToWrite.Add(afValue);
                    valuesToWrite.Add(priceValue);
                    valuesToWrite.Add(hapValue);
                }

                piServer.UpdateValues(valuesToWrite, AFUpdateOption.Replace, AFBufferOption.BufferIfPossible);
                message = "Value(s) successfully saved!";
                status = "success";
            }
            catch(Exception e)
            {
                message = e.Message.ToString();
                status = "error";
            }
            
            return Content(HttpStatusCode.OK, new { message = message, status = status});

        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetUnit(int SiteID)
        {
            List<Unit> unitList = new List<Unit>();
            List<ManualEntry> manualEntryList = new List<ManualEntry>();
            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select * from m_Unit where siteID = @siteID and isActive = 1";
                var paramSiteID = new SqlParameter("@siteID", SiteID);
                //if(db.Database.SqlQuery<Unit>(Str, paramSiteID).ToList<Unit>().Count > 0)
                //{
                unitList = db.Database.SqlQuery<Unit>(Str, paramSiteID).ToList<Unit>();
                //}

                foreach(var unit in unitList)
                {
                    manualEntryList.Add(new ManualEntry
                    {
                        UnitID = unit.UnitID
                        ,SiteID = unit.SiteID
                        ,UnitNumber = unit.UnitNumber
                        ,RTDTagName = unit.RTDTagName
                        ,HAPTagName = unit.HAPTagName
                        ,PriceTagName = unit.EAPTagName
                        ,TypeID = unit.TypeID
                    });
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, manualEntryList);
        }
    }
}