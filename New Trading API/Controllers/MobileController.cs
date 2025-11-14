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
    public class MobileController : ApiController
    {
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetUnitPerRegionCurrent()
        {
            var Luz = GetRegionPIValue("Luzon",0);
            var Vis = GetRegionPIValue("Vizayas",0);
            var Min = GetRegionPIValue("Mindanao",0);
            return Request.CreateResponse(HttpStatusCode.OK, new { Luz = Luz, Vis = Vis, Min = Min });
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetUnitPerRegionPrevious()
        {
            var Luz = GetRegionPIValue("Luzon", 5);
            var Vis = GetRegionPIValue("Vizayas", 5);
            var Min = GetRegionPIValue("Mindanao", 5);
            return Request.CreateResponse(HttpStatusCode.OK, new { Luz = Luz, Vis = Vis, Min = Min });
        }

        public List<RegionPIValue> GetRegionPIValue(string Region, int minute)
        {
            List<RegionPIValue> regionPIValue = new List<RegionPIValue>();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            int minMod = 0;
            minMod = ((DateTime.Now.Minute) % 5);

            using (TradingEntities db = new TradingEntities())
            {
                string query = $"exec sp_UnitPerRegion '{Region}'";
                var data = db.Database
                    .SqlQuery<UnitPerRegionDB>(query)
                    .ToList<UnitPerRegionDB>();


                foreach (var item in data)
                {
                    Nullable<Double> rtd = null;
                    Nullable<Double> actual = null;
                    Nullable<Double> price = null;
                    Nullable<Double> percentage = null;
                    string remarks = "";
                    Nullable<bool> isLimit;
                    //get pi data
                    PIPoint ptRtd = PIPoint.FindPIPoint(piServer, item.RTDTagName);
                    PIPoint ptPrice = PIPoint.FindPIPoint(piServer, item.EAPTagName);
                    PIPoint ptActual = PIPoint.FindPIPoint(piServer, item.ActualTagName);
                    //PIPoint ptRemarks = PIPoint.FindPIPoint(piServer, item.ComplianceTagName);

                    var ptRTDValue = ptRtd.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + minute)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + minute)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + minute)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + minute)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    var ptActualValue = ptActual.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + minute)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + minute)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    //var ptRemarksValue = ptRemarks.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + 5)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + 5)).ToString("HH:mm:00")), AFRetrievalMode.AtOrBefore);

                    //get remarks

                    try
                    {
                        query = $"exec [sp_GeneralComment] '{item.UnitNumber}'";
                        var genRemarks = db.Database.SqlQuery<GenRemarks>(query).FirstOrDefault<GenRemarks>();
                        if (genRemarks != null)
                        {
                            remarks = genRemarks.Value.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        remarks = ex.Message;
                    }


                    if (ptRTDValue.Value.ToString() == "No Data" || ptRTDValue.Value.ToString() == "Pt Created")
                    {
                        rtd = null;
                    }
                    else
                    {
                        rtd = Convert.ToDouble(ptRTDValue.Value);
                    }

                    if (ptPriceValue.Value.ToString() == "No Data" || ptPriceValue.Value.ToString() == "Pt Created")
                    {
                        price = null;
                    }
                    else
                    {
                        price = Convert.ToDouble(ptPriceValue.Value);
                    }

                    if (ptActualValue.Value.ToString() == "No Data" || ptActualValue.Value.ToString() == "Pt Created")
                    {
                        actual = null;
                    }
                    else
                    {
                        actual = Convert.ToDouble(ptActualValue.Value);
                    }

                    //if (ptRemarksValue.Value.ToString() == "No Data" || ptRemarksValue.Value.ToString() == "Pt Created")
                    //{
                    //    remarks = null;
                    //}
                    //else
                    //{
                    //    remarks = ptRemarksValue.Value.ToString();
                    //}

                    try
                    {
                        if (actual >= (rtd - (rtd * 0.03)) && actual <= (rtd + (rtd * 0.015)))
                        {
                            isLimit = true;
                        }
                        else
                        {
                            isLimit = false;
                        }
                    }
                    catch
                    {
                        isLimit = null;
                    }

                    try
                    {
                        if (rtd == 0)
                        {
                            percentage = 0;
                        }
                        else
                        {
                            percentage = ((actual - rtd) / rtd) * 100;
                        }
                    }
                    catch
                    {
                        percentage = null;
                    }
                    regionPIValue.Add(new RegionPIValue
                    {
                        RTD = rtd,
                        Price = price,
                        Actual = actual,
                        Remarks = remarks,
                        Percentage = percentage,
                        IsLimit = isLimit,
                        SiteName = item.SiteName,
                        UnitNumber = item.UnitNumber,
                        Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + minute)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + minute)).ToString("HH:mm:00")),
                        TimestampLabel = DateTime.Now.AddMinutes(-(minMod + minute)).ToString("HH:mm")
                    });
                }
            }

            return regionPIValue;
        }
    }
}