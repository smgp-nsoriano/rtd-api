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
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;
using Newtonsoft.Json;
using System.Xml;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.Entity.Core.EntityClient;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Configuration;
using New_Trading_API.Constant;
using System.Security.Claims;

namespace New_Trading_API.Controllers
{
    public class MarketDataController : ApiController
    {
        const int AHCSiteID = 1009;

        #region Main
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetCurrentRTD(int SiteID, int UnitID)
        {
            TagValue currentValue = new TagValue();
            Unit unit = new Unit();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> rru = null;
            Nullable<Double> rrd = null;
            Nullable<Double> cont = null;

            int minMod = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            piServer.Connect(true);
            string rruTagname = string.Empty;
            string rrdTagname = string.Empty;
            string contingencyTagname = string.Empty;

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, currentValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagName);
                PIPoint ptHAP = PIPoint.FindPIPoint(piServer, unit.HAPTagName);
                minMod = 5 - ((DateTime.Now.Minute) % 5);
                string ruDataStatus = string.Empty;
                string rdDataStatus = string.Empty;
                string frDataStatus = string.Empty;
                
                if (unit.TypeID.ToUpper().Contains("BATT"))
                {
                    rruTagname = String.Format(ReserveDefaults.SchedualeTagNames["RTD"], "RU", unit.UnitNumber);
                    rrdTagname = String.Format(ReserveDefaults.SchedualeTagNames["RTD"], "RD", unit.UnitNumber);
                    contingencyTagname = String.Format(ReserveDefaults.SchedualeTagNames["RTD"], "FR", unit.UnitNumber);
                    PIPoint ptRU = PIPoint.FindPIPoint(piServer, rruTagname);
                    PIPoint ptRD = PIPoint.FindPIPoint(piServer, rrdTagname);
                    PIPoint ptCont = PIPoint.FindPIPoint(piServer, contingencyTagname);

                    AFValue afValueRU = ptRU.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    AFValue afValueRD = ptRD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    AFValue afValueCont = ptCont.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);

                    if (afValueRU.Value.ToString() == "No Data" || afValueRU.Value.ToString() == "Pt Created")
                    {
                        rru = CheckOverride(db, "RU", unit.UnitNumber, out ruDataStatus);
                    }
                    else
                    {
                        rru = Convert.ToDouble(afValueRU.Value);
                        ruDataStatus = "R";
                    }

                    if (afValueRD.Value.ToString() == "No Data" || afValueRD.Value.ToString() == "Pt Created")
                    {
                        rrd = CheckOverride(db, "RD", unit.UnitNumber, out rdDataStatus);
                    }
                    else
                    {
                        rrd = Convert.ToDouble(afValueRD.Value);
                        rdDataStatus = "R";
                    }

                    if (afValueCont.Value.ToString() == "No Data" || afValueCont.Value.ToString() == "Pt Created")
                    {
                        cont = CheckOverride(db, "FR", unit.UnitNumber, out frDataStatus);
                    }
                    else
                    {
                        cont = Convert.ToDouble(afValueCont.Value);
                        frDataStatus = "R";
                    }
                }
                

                if (ptRTD != null && ptPrice != null)
                {
                    
                    AFValue afValueRTD = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    AFValue afValuePrice = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    
                    string dataStatus = "R";

                    string queryMOT = $"exec[sp_MOT] '{unit.UnitNumber}'";
                    var dataMOT = db.Database
                        .SqlQuery<Override>(queryMOT)
                        .FirstOrDefault<Override>();
                    if (dataMOT.IsUse == true)
                    {
                        rtd = dataMOT.Value;
                        dataStatus = "M";
                    }
                    else
                    {
                        if (afValueRTD.Value.ToString() == "No Data" || afValueRTD.Value.ToString() == "Pt Created")
                        {
                            string query = $"exec[sp_OverrideValue] '{unit.UnitNumber}'";
                            var data = db.Database
                                .SqlQuery<Override>(query)
                                .FirstOrDefault<Override>();
                            if (data.IsUse == true)
                            {
                                rtd = data.Value;
                                dataStatus = "O";
                            }
                            else
                            {
                                var ptHAPValue = ptHAP.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                                if (ptHAPValue.Value.ToString() == "No Data" || ptHAPValue.Value.ToString() == "Pt Created")
                                {
                                    rtd = null;
                                    dataStatus = "R";
                                }
                                else
                                {
                                    rtd = Convert.ToDouble(ptHAPValue.Value);
                                    dataStatus = "H";
                                }
                            }

                            
                        }
                        else
                        {
                            //disable override
                            string query = $@"update t_OverrideValue 
                                            set IsUse = 0 
                                        where UnitNumber = '{unit.UnitNumber}'";
                            db.Database.ExecuteSqlCommand(query);

                            rtd = Convert.ToDouble(afValueRTD.Value);
                        }
                    }


                    if (afValuePrice.Value.ToString() == "No Data" || afValuePrice.Value.ToString() == "Pt Created")
                    {
                        eap = null;
                    }
                    else
                    {
                        eap = Convert.ToDouble(afValuePrice.Value);
                    }

                    currentValue = new TagValue
                    {
                        RTDValue = rtd,
                        PriceValue = eap,
                        TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                        DateTIme = DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00"),
                        DataStatus = dataStatus,
                        IsWithAlarm = unit.IsWithAlarm,
                        IsWithDecimal = unit.IsWithDecimal,
                        RRU = rru,
                        RRD = rrd,
                        Contingency = cont,
                        RUDataStatus = ruDataStatus,
                        RDDataStatus = rdDataStatus,
                        ConDataStatus = frDataStatus
                    };
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, currentValue);
        }

        private double? CheckOverride(TradingEntities db,string commodity,string unitNumber, out string dataStatus)
        {
            double? val = null;
            dataStatus = "R";
            string query = $"exec[sp_OverrideValue] '{unitNumber}'";
            var data = db.Database
                .SqlQuery<Override>(query)
                .FirstOrDefault<Override>();

            switch (commodity)
            {
                case "RU":
                    if (data.RuIsUse == true)
                    {
                        val = data.RuValue;
                        dataStatus = "O";
                    }
                    break;
                case "RD":
                    if (data.RdIsUse == true)
                    {
                        val = data.RdValue;
                        dataStatus = "O";
                    }
                    break;
                case "FR":
                    if (data.FrIsUse == true)
                    {
                        val = data.FrValue;
                        dataStatus = "O";
                    }
                    break;
            }

            return val;

        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetRTDPriceCurrentByUnit(string Unit)
        {
            TagValue currentValue = new TagValue();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            int minMod = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            piServer.Connect(true);
            Unit unit = new Unit();
            string dataStatus = "R";
            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '" + Unit + "'";
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagName);
                PIPoint ptHAP = PIPoint.FindPIPoint(piServer, unit.HAPTagName);
                if (ptRTD != null && ptPrice != null)
                {
                    minMod = 5 - ((DateTime.Now.Minute) % 5);
                    AFValue afValueRTD = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToString("MM/dd/yyyy HH:mm:00")), AFRetrievalMode.Exact);
                    AFValue afValuePrice = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToString("MM/dd/yyyy HH:mm:00")), AFRetrievalMode.Exact);

                    if (afValueRTD.Value.ToString() == "No Data" || afValueRTD.Value.ToString() == "Pt Created")
                    {
                        string query = $"exec[sp_OverrideValue] '{Unit}'";
                        var data = db.Database
                            .SqlQuery<Override>(query)
                            .FirstOrDefault<Override>();
                        if (data.IsUse == true)
                        {
                            rtd = data.Value;
                            dataStatus = "O";
                        }
                        else
                        {
                            var ptHAPValue = ptHAP.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToString("MM/dd/yyyy HH:mm:00")), AFRetrievalMode.Exact);
                            if (ptHAPValue.Value.ToString() == "No Data" || ptHAPValue.Value.ToString() == "Pt Created")
                            {
                                rtd = null;
                                dataStatus = "R";
                            }
                            else
                            {
                                rtd = Convert.ToDouble(ptHAPValue.Value);
                                dataStatus = "H";
                            }
                        }
                        eap = null;
                    }
                    else
                    {
                        //disable override 
                        string query = $@"update t_OverrideValue 
                                            set IsUse = 0 
                                        where UnitNumber = '{Unit}'";
                        db.Database.ExecuteSqlCommand(query);

                        rtd = Convert.ToDouble(afValueRTD.Value);
                        eap = Convert.ToDouble(afValuePrice.Value);
                    }

                    currentValue = new TagValue
                    {
                        RTDValue = rtd,
                        PriceValue = eap,
                        TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                        DateTIme = DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00"),
                        DataStatus = dataStatus
                    };
                }
            }


            return Request.CreateResponse(HttpStatusCode.OK, currentValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAheadRTD(int SiteID, int UnitID)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Unit unit = new Unit();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            int minMod = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, TagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                //PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.UnitNumber + "_HAP_V");
                //PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.UnitNumber + "_HAP_P");
                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.HAPTagName);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.HAPPriceTagName);

                minMod = 5 - ((DateTime.Now.Minute) % 5);


                try
                {
                    for (int x = 15; x >= 5; x = x - 5)
                    {

                        var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                        var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);

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
                            eap = null;
                        }
                        else
                        {
                            eap = Convert.ToDouble(ptPriceValue.Value);
                        }

                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd
                                        ,
                            PriceValue = eap
                                        ,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00"))
                                        ,
                            DateTIme = DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")
                        });
                    }
                }
                catch
                { }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAheadRTDDesc(int SiteID, int UnitID)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Unit unit = new Unit();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            int minMod = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, TagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                //PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.UnitNumber + "_HAP_V");
                //PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.UnitNumber + "_HAP_P");
                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.HAPTagName);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.HAPPriceTagName);
                PIPoint ptRTDPlus10 = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                PIPoint ptRUPlus10 = PIPoint.FindPIPoint(piServer, unit.RTDTagName.Replace("5MINS","RU"));
                PIPoint ptRDPlus10 = PIPoint.FindPIPoint(piServer, unit.RTDTagName.Replace("5MINS", "RD"));
                minMod = 5 - ((DateTime.Now.Minute) % 5);
                int aheadMin = 5;

                var ptRTDValue10 = ptRTDPlus10.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod + 5).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod + 5).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                if (ptRTDValue10.Value.ToString() == "No Data" || ptRTDValue10.Value.ToString() == "Pt Created")
                {
                    aheadMin = 5;
                }
                else
                {
                    aheadMin = 10;
                }
                double? ruVal = null;
                double? rdVal = null;

                if(unit.TypeID == "BATT" || unit.TypeID == "BAT")
                {
                    var ptRUValue10 = ptRUPlus10.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod + 5).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod + 5).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    if (ptRUValue10.Value.ToString() != "No Data" && ptRUValue10.Value.ToString() != "Pt Created")
                        ruVal = Convert.ToDouble(ptRUValue10.Value);

                    var ptRDValue10 = ptRUPlus10.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod + 5).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod + 5).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    if (ptRDValue10.Value.ToString() != "No Data" && ptRDValue10.Value.ToString() != "Pt Created")
                        rdVal = Convert.ToDouble(ptRDValue10.Value);
                }

                try
                {
                    for (int x = 15; x >= aheadMin; x = x - 5)
                    {

                        var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                        var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);

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
                            eap = null;
                        }
                        else
                        {
                            eap = Convert.ToDouble(ptPriceValue.Value);
                        }

                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd
                                        ,
                            PriceValue = eap
                                        ,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00"))
                                        ,
                            DateTIme = DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")
                        });
                    }

                    if (aheadMin == 10)
                    {
                        var val = Convert.ToDouble(ptRTDValue10.Value);
                        TagValue.Add(new TagValue
                        {
                            RTDValue = val,
                            RRU = ruVal,
                            RRD = rdVal,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(5 + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(5 + minMod).ToString("HH:mm:00")),
                            DateTIme = DateTime.Now.AddMinutes(5 + minMod).ToString("HH:mm")
                        });
                    }
                }
                catch
                { }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue.OrderBy(x => x.TimeStamp));
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAheadRTDByUnit(string Unit)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            int minMod = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            Unit unit = new Unit();
            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '" + Unit + "'";
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.HAPTagName);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.HAPPriceTagName);

                minMod = 5 - ((DateTime.Now.Minute) % 5);

                try
                {
                    for (int x = 15; x >= 5; x = x - 5)
                    {
                        var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(x + minMod).ToString("MM/dd/yyyy HH:mm:00")), AFRetrievalMode.Exact);
                        var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(x + minMod).ToString("MM/dd/yyyy HH:mm:00")), AFRetrievalMode.Exact);

                        if (ptRTDValue.Value.ToString() == "No Data" || ptRTDValue.Value.ToString() == "Pt Created")
                        {
                            rtd = null;
                            eap = null;
                        }
                        else
                        {
                            rtd = Convert.ToDouble(ptRTDValue.Value);
                            eap = Convert.ToDouble(ptPriceValue.Value);
                        }

                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd
                            ,
                            PriceValue = eap
                            ,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00"))
                            ,
                            DateTIme = DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")
                        });
                    }

                }
                catch
                { }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetPastRTD(int SiteID, int UnitID)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> actual = null;
            Nullable<Boolean> isLimit = null;
            Unit unit = new Unit();
            int minMod = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, TagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagName);
                PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);
                PIPoint ptCompliance = PIPoint.FindPIPoint(piServer, unit.ComplianceTagName);

                minMod = ((DateTime.Now.Minute) % 5);

                for (int x = 0; x <= 25; x = x + 5)
                {
                    var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    var ptActualValue = ptActual.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    var ptComplianceValue = ptCompliance.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);

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
                        eap = null;
                    }
                    else
                    {
                        eap = Convert.ToDouble(ptPriceValue.Value);
                    }

                    if (ptActualValue.Value.ToString() == "No Data" || ptActualValue.Value.ToString() == "Pt Created")
                    {
                        actual = null;
                    }
                    else
                    {
                        actual = Convert.ToDouble(ptActualValue.Value);
                    }

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
                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = eap,
                            ActualValue = actual,
                            IsLimit = isLimit,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            DateTIme = DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")
                        });
                    }
                    catch
                    { }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetPastRTDAHC(int SiteID, int UnitID)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> actual = null;
            Nullable<Boolean> isLimit = null;
            Unit unit = new Unit();
            int minMod = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, TagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagName);
                PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);
                PIPoint ptCompliance = PIPoint.FindPIPoint(piServer, unit.ComplianceTagName);

                minMod = ((DateTime.Now.Minute) % 5);

                for (int x = 0; x <= 25; x = x + 5)
                {
                    var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    var ptActualValue = ptActual.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    var ptComplianceValue = ptCompliance.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);

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
                        eap = null;
                    }
                    else
                    {
                        eap = Convert.ToDouble(ptPriceValue.Value);
                    }

                    if (ptActualValue.Value.ToString() == "No Data" || ptActualValue.Value.ToString() == "Pt Created")
                    {
                        actual = null;
                    }
                    else
                    {
                        actual = Convert.ToDouble(ptActualValue.Value);
                    }

                    try
                    {
                        if (actual >= (rtd - 1) && actual <= (rtd + 1))
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
                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd
                            ,
                            PriceValue = eap
                            ,
                            ActualValue = actual
                            ,
                            IsLimit = isLimit
                            ,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00"))
                            ,
                            DateTIme = DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")
                        });
                    }
                    catch
                    { }
                }

            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetPastRTDDesc(int SiteID, int UnitID)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> actual = null;
            Nullable<Boolean> isLimit = null;
            Unit unit = new Unit();
            int minMod = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, TagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagName);
                PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);
                PIPoint ptCompliance = PIPoint.FindPIPoint(piServer, unit.ComplianceTagName);

                minMod = ((DateTime.Now.Minute) % 5);

                for (int x = 0; x <= 25; x = x + 5)
                {
                    var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    var ptActualValue = ptActual.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    var ptComplianceValue = ptCompliance.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);

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
                        eap = null;
                    }
                    else
                    {
                        eap = Convert.ToDouble(ptPriceValue.Value);
                    }

                    if (ptActualValue.Value.ToString() == "No Data" || ptActualValue.Value.ToString() == "Pt Created")
                    {
                        actual = null;
                    }
                    else
                    {
                        actual = Convert.ToDouble(ptActualValue.Value);
                    }

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
                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd
                            ,
                            PriceValue = eap
                            ,
                            ActualValue = actual
                            ,
                            IsLimit = isLimit
                            ,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00"))
                            ,
                            DateTIme = DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")
                        });
                    }
                    catch
                    { }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue.OrderBy(x=> x.TimeStamp));
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetPastRTDByUnit(string Unit)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> actual = null;
            Nullable<Boolean> isLimit = null;
            int minMod = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            Unit unit = new Unit();
            using (TradingEntities db = new TradingEntities())
            {
                Console.WriteLine(Unit);
                string Str = "select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '" + Unit + "'";
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagName);
                PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);
                PIPoint ptCompliance = PIPoint.FindPIPoint(piServer, unit.ComplianceTagName);

                minMod = ((DateTime.Now.Minute) % 5);

                for (int x = 0; x <= 25; x = x + 5)
                {
                    var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToString("MM/dd/yyyy HH:mm:00")), AFRetrievalMode.Exact);
                    var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToString("MM/dd/yyyy HH:mm:00")), AFRetrievalMode.Exact);
                    var ptActualValue = ptActual.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToString("MM/dd/yyyy HH:mm:00")), AFRetrievalMode.Exact);
                    var ptComplianceValue = ptCompliance.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToString("MM/dd/yyyy HH:mm:00")), AFRetrievalMode.Exact);

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
                        eap = null;
                    }
                    else
                    {
                        eap = Convert.ToDouble(ptPriceValue.Value);
                    }

                    if (ptActualValue.Value.ToString() == "No Data" || ptActualValue.Value.ToString() == "Pt Created")
                    {
                        actual = null;
                    }
                    else
                    {
                        actual = Convert.ToDouble(ptActualValue.Value);
                    }

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
                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = eap,
                            ActualValue = actual,
                            IsLimit = isLimit,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            DateTIme = DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")
                        });
                    }
                    catch
                    { }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage ErrorMessage(int SiteID, int UnitID)
        {
            TagValue currentValue = new TagValue();
            Unit unit = new Unit();
            string msg = null;
            //PIServers piServers = new PIServers();
            //PIServer piServer = piServers[GlobalFunctions.piServerName];
            //piServer.Connect(true);

            //if (SiteID == 0 && UnitID == 00)
            //{
            //    return Request.CreateResponse(HttpStatusCode.OK, new { message = msg });
            //}

            //using (TradingEntities db = new TradingEntities())
            //{
            //    string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
            //    unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

            //    PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
            //    PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagName);
            //    PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);
                
            //    if (ptRTD != null && ptPrice != null)
            //    {
            //        int minMod = 5 - ((DateTime.Now.Minute) % 5);
            //        AFValue afValueRTD = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
            //        AFValue afValuePrice = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
            //        AFValue afValueActual = ptActual.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-((DateTime.Now.Minute) % 5)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-((DateTime.Now.Minute) % 5)).ToString("HH:mm:00")), AFRetrievalMode.Exact);

            //        if (afValueRTD.Value.ToString() == "No Data" || afValueRTD.Value.ToString() == "Pt Created")
            //        {
            //            msg = "RTD,";
            //        }
            //        if (afValuePrice.Value.ToString() == "No Data" || afValuePrice.Value.ToString() == "Pt Created")
            //        {
            //            msg += "Price,";
            //        }
            //        if (afValueActual.Value.ToString() == "No Data" || afValueActual.Value.ToString() == "Pt Created")
            //        {
            //            msg += "Actual";
            //        }
            //    }
            //}

            return Request.CreateResponse(HttpStatusCode.OK, new { message = msg });
        }


        // Hourly Interval

        [HttpGet]
        [Authorize]
        public HttpResponseMessage TestHourly(int SiteID, int UnitID)
        {
            Unit unit = new Unit();
            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, unit);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetCurrentRTDHourly(int SiteID, int UnitID)
        {
            Unit unit = new Unit();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;

            TagValue currentValueHourly = new TagValue();
            int getHour = 0;


            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            piServer.Connect(true);

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, currentValueHourly);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagNameH);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagNameH);
                if (ptRTD != null && ptPrice != null)
                {
                    // Retrieve data in hourly based. (CURRENT) // Current Hour is always 1 hour ahead
                    AFValue afValueRTD = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddHours(getHour).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                    AFValue afValuePrice = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddHours(getHour).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);

                    if (afValueRTD.Value.ToString() == "No Data" || afValueRTD.Value.ToString() == "Pt Created")
                    {
                        rtd = null;
                    }
                    else
                    {
                        rtd = Convert.ToDouble(afValueRTD.Value);
                    }

                    if (afValuePrice.Value.ToString() == "No Data" || afValuePrice.Value.ToString() == "Pt Created")
                    {
                        eap = null;
                    }
                    else
                    {
                        eap = Convert.ToDouble(afValuePrice.Value);
                    }

                    currentValueHourly = new TagValue
                    {
                        RTDValue = rtd
       ,
                        PriceValue = eap
       ,
                        TimeStamp = Convert.ToDateTime(DateTime.Now.AddHours(getHour+1).ToShortDateString() + " " + DateTime.Now.AddHours(getHour+1).ToString("HH:00:00"))
       ,
                        DateTIme = DateTime.Now.AddHours(getHour+1).ToShortDateString() + " " + DateTime.Now.AddHours(getHour +1).ToString("HH:00:00")
                    };
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, currentValueHourly);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetRTDPriceCurrentByUnitHourlyFTP(string Unit)
        {
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> dap = null;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            piServer.Connect(true);
            Unit unit = new Unit();

            TagValue currentValueHourly = new TagValue();
            int getHour = 0;

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '" + Unit + "'";
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                string DAPRTD = unit.DAPTagName;
                int checkHour = DateTime.Now.Hour;
                DateTime currentDate = DateTime.Now;
                DateTime checkDate = Convert.ToDateTime(DateTime.Now.ToShortDateString() + " 00:00:00");
                DateTime.Now.AddHours(1).AddMinutes(30);


                if (currentDate <= checkDate.AddHours(1).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_20_");
                }
                else if (currentDate >= checkDate.AddHours(1).AddMinutes(30)
                    && currentDate < checkDate.AddHours(5).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_00_");
                }
                else if (currentDate >= checkDate.AddHours(5).AddMinutes(30)
                    && currentDate < checkDate.AddHours(9).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_04_");
                 
                }
                else if (currentDate >= checkDate.AddHours(9).AddMinutes(30)
                    && currentDate < checkDate.AddHours(13).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_08_");
                 
                }
                else if (currentDate >= checkDate.AddHours(13).AddMinutes(30)
                    && currentDate < checkDate.AddHours(17).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_12_");
                }
                else if (currentDate >= checkDate.AddHours(17).AddMinutes(30)
                    && currentDate < checkDate.AddHours(21).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_16_");
                }
                else if (currentDate >= checkDate.AddHours(21).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_20_");
                }


                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagNameH);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagNameH);
                PIPoint ptDAP = PIPoint.FindPIPoint(piServer, DAPRTD);

                if (ptRTD != null && ptPrice != null)
                {
                    // Retrieve data in hourly based. (CURRENT) // Current Hour is always 1 hour ahead
                    AFValue afValueRTD = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddHours(getHour).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                    AFValue afValuePrice = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddHours(getHour).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                    AFValue afValueDAP = ptDAP.RecordedValue(new AFTime(DateTime.Now.AddHours(getHour).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);

                    if (afValueRTD.Value.ToString() == "No Data" || afValueRTD.Value.ToString() == "Pt Created")
                    {
                        rtd = null;
                        eap = null;
                    }
                    else
                    {
                        rtd = Convert.ToDouble(afValueRTD.Value);
                        eap = Convert.ToDouble(afValuePrice.Value);
                    }

                    if (afValueDAP.Value.ToString() == "No Data" || afValueDAP.Value.ToString() == "Pt Created")
                    {
                        dap = null;
                    }
                    else
                    {
                        dap = Convert.ToDouble(afValueDAP.Value);
                    }

                    currentValueHourly = new TagValue
                    {
                        RTDValue = rtd,
                        PriceValue = eap,
                        DAPValue = dap,
                        TimeStamp = Convert.ToDateTime(DateTime.Now.AddHours(getHour+1).ToShortDateString() + " " + DateTime.Now.AddHours(getHour+1).ToString("HH:00:00")),
                        DateTIme = DateTime.Now.AddHours(getHour+1).ToShortDateString() + " " + DateTime.Now.AddHours(getHour+1).ToString("HH:00:00")
                    };
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, currentValueHourly);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetRTDPriceCurrentByUnitHourly(string Unit)
        {
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> dap = null;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            piServer.Connect(true);
            Unit unit = new Unit();

            TagValue currentValueHourly = new TagValue();
            int getHour = 0;

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '" + Unit + "'";
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                string DAPRTD = unit.DAPTagName;
                int checkHour = DateTime.Now.Hour;
                DateTime currentDate = DateTime.Now;
                DateTime checkDate = Convert.ToDateTime(DateTime.Now.ToShortDateString() + " 00:00:00");

                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagNameH);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagNameH);
                PIPoint ptDAP = PIPoint.FindPIPoint(piServer, DAPRTD);

                if (ptRTD != null && ptPrice != null)
                {
                    // Retrieve data in hourly based. (CURRENT) // Current Hour is always 1 hour ahead
                    AFValue afValueRTD = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddHours(getHour).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                    AFValue afValuePrice = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddHours(getHour).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                    AFValue afValueDAP = ptDAP.RecordedValue(new AFTime(DateTime.Now.AddHours(getHour + 1).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);

                    if (afValueRTD.Value.ToString() == "No Data" || afValueRTD.Value.ToString() == "Pt Created")
                    {
                        rtd = null;
                        eap = null;
                    }
                    else
                    {
                        rtd = Convert.ToDouble(afValueRTD.Value);
                        eap = Convert.ToDouble(afValuePrice.Value);
                    }

                    if (afValueDAP.Value.ToString() == "No Data" || afValueDAP.Value.ToString() == "Pt Created")
                    {
                        dap = null;
                    }
                    else
                    {
                        dap = Convert.ToDouble(afValueDAP.Value);
                    }

                    currentValueHourly = new TagValue
                    {
                        RTDValue = rtd,
                        PriceValue = eap,
                        DAPValue = dap,
                        TimeStamp = Convert.ToDateTime(DateTime.Now.AddHours(getHour + 1).ToShortDateString() + " " + DateTime.Now.AddHours(getHour + 1).ToString("HH:00:00")),
                        DateTIme = DateTime.Now.AddHours(getHour + 1).ToShortDateString() + " " + DateTime.Now.AddHours(getHour + 1).ToString("HH:00:00")
                    };
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, currentValueHourly);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAheadRTDHourly(int SiteID, int UnitID)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Unit unit = new Unit();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            int minMod = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, TagValue);
            }
            TradingEntities d = new TradingEntities();
            

            using (TradingEntities db = new TradingEntities())
            {
               

                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                string DAPRTD = unit.DAPTagName;
                string DAPPrice = unit.DAPPriceTagName;
                int checkHour = DateTime.Now.Hour;
                DateTime currentDate = DateTime.Now;
                DateTime checkDate = Convert.ToDateTime(DateTime.Now.ToShortDateString() + " 00:00:00");

                if (currentDate <= checkDate.AddHours(1).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_20_");
                    DAPPrice = DAPPrice.Replace("_00_", "_20_");
                   
                }
                else if (currentDate >= checkDate.AddHours(1).AddMinutes(30)
                    && currentDate < checkDate.AddHours(5).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_00_");
                    DAPPrice = DAPPrice.Replace("_00_", "_00_");
                    
                }
                else if (currentDate >= checkDate.AddHours(5).AddMinutes(30)
                    && currentDate < checkDate.AddHours(9).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_04_");
                    DAPPrice = DAPPrice.Replace("_00_", "_04_");
                    
                }
                else if (currentDate >= checkDate.AddHours(9).AddMinutes(30)
                    && currentDate < checkDate.AddHours(13).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_08_");
                    DAPPrice = DAPPrice.Replace("_00_", "_08_");
                   
                }
                else if (currentDate >= checkDate.AddHours(13).AddMinutes(30)
                    && currentDate < checkDate.AddHours(17).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_12_");
                    DAPPrice = DAPPrice.Replace("_00_", "_12_");
            
                }
                else if (currentDate >= checkDate.AddHours(17).AddMinutes(30)
                    && currentDate < checkDate.AddHours(21).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_16_");
                    DAPPrice = DAPPrice.Replace("_00_", "_16_");
                    
                }
                else if (currentDate >= checkDate.AddHours(21).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_20_");
                    DAPPrice = DAPPrice.Replace("_00_", "_20_");
                    
                }

                //PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.UnitNumber + "_HAP_V");
                //PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.UnitNumber + "_HAP_P");
                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, DAPRTD);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, DAPPrice);

                var getHour =0;

                try
                {
                    for (int x = 3; x >= 1; x--)
                    {
                        var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddHours(x + getHour).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                        var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddHours(x + getHour).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);

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
                            eap = null;
                        }
                        else
                        {
                            eap = Convert.ToDouble(ptPriceValue.Value);
                        }

                        TagValue.Add(
                            new TagValue
                            {
                                RTDValue = rtd
                                ,
                                PriceValue = eap
                                ,
                                TimeStamp = Convert.ToDateTime(DateTime.Now.AddHours(x + getHour + 1).ToShortDateString() + " " + DateTime.Now.AddHours(x + getHour + 1).ToString("HH:00:00"))
                                ,
                                DateTIme = DateTime.Now.AddHours(x + getHour + 1).ToShortDateString() + " " + DateTime.Now.AddHours(x + getHour + 1).ToString("HH:00:00")
                            });
                    }
                }
                catch
                { }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAheadRTDByUnitHourlyFTP(string Unit)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> dap = null;
            int getHour = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            Unit unit = new Unit();
            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '" + Unit + "'";
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                string DAPRTD = unit.DAPTagName;
                string DAPPrice = unit.DAPPriceTagName;
                int checkHour = DateTime.Now.Hour;
                DateTime currentDate = DateTime.Now;
                DateTime checkDate = Convert.ToDateTime(DateTime.Now.ToShortDateString() + " 00:00:00");
                DateTime.Now.AddHours(1).AddMinutes(30);


                if (currentDate <= checkDate.AddHours(1).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_20_");
                    DAPPrice = DAPPrice.Replace("_00_", "_20_");

                }
                else if (currentDate >= checkDate.AddHours(1).AddMinutes(30)
                    && currentDate < checkDate.AddHours(5).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_00_");
                    DAPPrice = DAPPrice.Replace("_00_", "_00_");

                }
                else if (currentDate >= checkDate.AddHours(5).AddMinutes(30)
                    && currentDate < checkDate.AddHours(9).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_04_");
                    DAPPrice = DAPPrice.Replace("_00_", "_04_");

                }
                else if (currentDate >= checkDate.AddHours(9).AddMinutes(30)
                    && currentDate < checkDate.AddHours(13).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_08_");
                    DAPPrice = DAPPrice.Replace("_00_", "_08_");

                }
                else if (currentDate >= checkDate.AddHours(13).AddMinutes(30)
                    && currentDate < checkDate.AddHours(17).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_12_");
                    DAPPrice = DAPPrice.Replace("_00_", "_12_");

                }
                else if (currentDate >= checkDate.AddHours(17).AddMinutes(30)
                    && currentDate < checkDate.AddHours(21).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_16_");
                    DAPPrice = DAPPrice.Replace("_00_", "_16_");

                }
                else if (currentDate >= checkDate.AddHours(21).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_20_");
                    DAPPrice = DAPPrice.Replace("_00_", "_20_");

                }


                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, DAPRTD);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, DAPPrice);
                PIPoint ptDAP = PIPoint.FindPIPoint(piServer, DAPRTD);

                try
                {
                    for (int x = 3; x >= 1; x--)
                    {
                        var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddHours(x + getHour).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                        var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddHours(x + getHour).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                        var ptDAPValue = ptDAP.RecordedValue(new AFTime(DateTime.Now.AddHours(x + getHour).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);

                        if (ptRTDValue.Value.ToString() == "No Data" || ptRTDValue.Value.ToString() == "Pt Created")
                        {
                            rtd = null;
                            eap = null;
                        }
                        else
                        {
                            rtd = Convert.ToDouble(ptRTDValue.Value);
                            eap = Convert.ToDouble(ptPriceValue.Value);
                        }

                        if (ptDAPValue.Value.ToString() == "No Data" || ptDAPValue.Value.ToString() == "Pt Created")
                        {
                            dap = null;
                        }
                        else
                        {
                            dap = Convert.ToDouble(ptDAPValue.Value);
                        }

                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = eap,
                            DAPValue = dap,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddHours(x + getHour + 1).ToShortDateString() + " " + DateTime.Now.AddHours(x + getHour + 1).ToString("HH:00:00")),
                            DateTIme = DateTime.Now.AddHours(x + getHour + 1).ToShortDateString() + " " + DateTime.Now.AddHours(x + getHour + 1).ToString("HH:00:00")
                        });
                    }

                }
                catch
                { }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAheadRTDByUnitHourly(string Unit)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> dap = null;
            int getHour = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            Unit unit = new Unit();
            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '" + Unit + "'";
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                string DAPRTD = unit.DAPTagName;
                string DAPPrice = unit.DAPPriceTagName;
                int checkHour = DateTime.Now.Hour;
                DateTime currentDate = DateTime.Now;
                DateTime checkDate = Convert.ToDateTime(DateTime.Now.ToShortDateString() + " 00:00:00");

                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, DAPRTD);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, DAPPrice);
                PIPoint ptDAP = PIPoint.FindPIPoint(piServer, DAPRTD);

                try
                {
                    for (int x = 3; x >= 1; x--)
                    {
                        var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddHours(x + getHour).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                        var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddHours(x + getHour).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                        var ptDAPValue = ptDAP.RecordedValue(new AFTime(DateTime.Now.AddHours(x + getHour + 1).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);

                        if (ptRTDValue.Value.ToString() == "No Data" || ptRTDValue.Value.ToString() == "Pt Created")
                        {
                            rtd = null;
                            eap = null;
                        }
                        else
                        {
                            rtd = Convert.ToDouble(ptRTDValue.Value);
                            eap = Convert.ToDouble(ptPriceValue.Value);
                        }

                        if (ptDAPValue.Value.ToString() == "No Data" || ptDAPValue.Value.ToString() == "Pt Created")
                        {
                            dap = null;
                        }
                        else
                        {
                            dap = Convert.ToDouble(ptDAPValue.Value);
                        }

                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = eap,
                            DAPValue = dap,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddHours(x + getHour + 1).ToShortDateString() + " " + DateTime.Now.AddHours(x + getHour + 1).ToString("HH:00:00")),
                            DateTIme = DateTime.Now.AddHours(x + getHour + 1).ToShortDateString() + " " + DateTime.Now.AddHours(x + getHour + 1).ToString("HH:00:00")
                        });
                    }

                }
                catch
                { }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetPastRTDHourly(int SiteID, int UnitID)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> actual = null;
            Nullable<Boolean> isLimit = null;
            Unit unit = new Unit();
            int getHour = 2;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, TagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagNameH);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagNameH);
                PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagNameH);

                for (int x = -1; x <= 4; x++)
                {
                    var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddHours(-(getHour + x)).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                    var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddHours(-(getHour + x)).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                    var ptActualValue = ptActual.RecordedValue(new AFTime(DateTime.Now.AddHours(-(getHour + x -1)).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);

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
                        eap = null;
                    }
                    else
                    {
                        eap = Convert.ToDouble(ptPriceValue.Value);
                    }

                    if (ptActualValue.Value.ToString() == "No Data" || ptActualValue.Value.ToString() == "Pt Created")
                    {
                        actual = null;
                    }
                    else
                    {
                        actual = Convert.ToDouble(ptActualValue.Value);
                    }

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
                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd
                            ,
                            PriceValue = eap
                            ,
                            ActualValue = actual
                            ,
                            IsLimit = isLimit
                            ,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddHours(-(getHour + x -1)).ToShortDateString() + " " + DateTime.Now.AddHours(-(getHour + x-1)).ToString("HH:00:00"))
                            ,
                            DateTIme = DateTime.Now.AddHours(-(getHour + x -1)).ToShortDateString() + " " + DateTime.Now.AddHours(-(getHour + x-1)).ToString("HH:00:00")
                        });
                    }
                    catch
                    { }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetPastRTDByUnitHourlyFTP(string Unit)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> dap = null;
            Nullable<Double> actual = null;
            Nullable<Boolean> isLimit = null;

            int getHour = 2;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            Unit unit = new Unit();
            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '" + Unit + "'";
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                string DAPRTD = unit.DAPTagName;
                int checkHour = DateTime.Now.Hour;
                DateTime currentDate = DateTime.Now;
                DateTime checkDate = Convert.ToDateTime(DateTime.Now.ToShortDateString() + " 00:00:00");
                DateTime.Now.AddHours(1).AddMinutes(30);


                if (currentDate <= checkDate.AddHours(1).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_20_");
                }
                else if (currentDate >= checkDate.AddHours(1).AddMinutes(30)
                    && currentDate < checkDate.AddHours(5).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_00_");
                }
                else if (currentDate >= checkDate.AddHours(5).AddMinutes(30)
                    && currentDate < checkDate.AddHours(9).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_04_");

                }
                else if (currentDate >= checkDate.AddHours(9).AddMinutes(30)
                    && currentDate < checkDate.AddHours(13).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_08_");

                }
                else if (currentDate >= checkDate.AddHours(13).AddMinutes(30)
                    && currentDate < checkDate.AddHours(17).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_12_");
                }
                else if (currentDate >= checkDate.AddHours(17).AddMinutes(30)
                    && currentDate < checkDate.AddHours(21).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_16_");
                }
                else if (currentDate >= checkDate.AddHours(21).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_20_");
                }


                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagNameH);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagNameH);
                PIPoint ptDAP = PIPoint.FindPIPoint(piServer, DAPRTD);
                PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagNameH);

                for (int x = -1; x <= 4; x++)
                {
                    var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddHours(-(getHour + x)).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                    var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddHours(-(getHour + x)).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                    var ptActualValue = ptActual.RecordedValue(new AFTime(DateTime.Now.AddHours(-(getHour + x)).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                    var ptDAPValue = ptDAP.RecordedValue(new AFTime(DateTime.Now.AddHours(-(getHour + x)).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);

                    if (ptRTDValue.Value.ToString() == "No Data" || ptRTDValue.Value.ToString() == "Pt Created")
                    {
                        rtd = null;
                    }
                    else
                    {
                        rtd = Convert.ToDouble(ptRTDValue.Value);
                    }

                    if (ptDAPValue.Value.ToString() == "No Data" || ptDAPValue.Value.ToString() == "Pt Created")
                    {
                        dap = null;
                    }
                    else
                    {
                        dap = Convert.ToDouble(ptDAPValue.Value);
                    }

                    if (ptPriceValue.Value.ToString() == "No Data" || ptPriceValue.Value.ToString() == "Pt Created")
                    {
                        eap = null;
                    }
                    else
                    {
                        eap = Convert.ToDouble(ptPriceValue.Value);
                    }

                    if (ptActualValue.Value.ToString() == "No Data" || ptActualValue.Value.ToString() == "Pt Created")
                    {
                        actual = null;
                    }
                    else
                    {
                        actual = Convert.ToDouble(ptActualValue.Value);
                    }

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
                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = eap,
                            DAPValue = dap,
                            ActualValue = actual,
                            IsLimit = isLimit,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddHours(-(getHour + x-1)).ToShortDateString() + " " + DateTime.Now.AddHours(-(getHour + x-1)).ToString("HH:00:00")),
                            DateTIme = DateTime.Now.AddHours(-(getHour + x-1)).ToShortDateString() + " " + DateTime.Now.AddHours(-(getHour + x-1)).ToString("HH:00:00")
                        });
                    }
                    catch
                    { }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetPastRTDByUnitHourly(string Unit)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> dap = null;
            Nullable<Double> actual = null;
            Nullable<Boolean> isLimit = null;

            int getHour = 2;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            Unit unit = new Unit();
            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '" + Unit + "'";
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                string DAPRTD = unit.DAPTagName;
                int checkHour = DateTime.Now.Hour;
                DateTime currentDate = DateTime.Now;
                DateTime checkDate = Convert.ToDateTime(DateTime.Now.ToShortDateString() + " 00:00:00");

                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagNameH);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagNameH);
                PIPoint ptDAP = PIPoint.FindPIPoint(piServer, DAPRTD);
                PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagNameH);

                for (int x = -1; x <= 4; x++)
                {
                    var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddHours(-(getHour + x)).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                    var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddHours(-(getHour + x)).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                    var ptActualValue = ptActual.RecordedValue(new AFTime(DateTime.Now.AddHours(-(getHour + x -1)).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);
                    var ptDAPValue = ptDAP.RecordedValue(new AFTime(DateTime.Now.AddHours(-(getHour + x - 1)).ToString("MM/dd/yyyy HH:00:00")), AFRetrievalMode.Exact);

                    if (ptRTDValue.Value.ToString() == "No Data" || ptRTDValue.Value.ToString() == "Pt Created")
                    {
                        rtd = null;
                    }
                    else
                    {
                        rtd = Convert.ToDouble(ptRTDValue.Value);
                    }

                    if (ptDAPValue.Value.ToString() == "No Data" || ptDAPValue.Value.ToString() == "Pt Created")
                    {
                        dap = null;
                    }
                    else
                    {
                        dap = Convert.ToDouble(ptDAPValue.Value);
                    }

                    if (ptPriceValue.Value.ToString() == "No Data" || ptPriceValue.Value.ToString() == "Pt Created")
                    {
                        eap = null;
                    }
                    else
                    {
                        eap = Convert.ToDouble(ptPriceValue.Value);
                    }

                    if (ptActualValue.Value.ToString() == "No Data" || ptActualValue.Value.ToString() == "Pt Created")
                    {
                        actual = null;
                    }
                    else
                    {
                        actual = Convert.ToDouble(ptActualValue.Value);
                    }

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
                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = eap,
                            DAPValue = dap,
                            ActualValue = actual,
                            IsLimit = isLimit,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddHours(-(getHour + x - 1)).ToShortDateString() + " " + DateTime.Now.AddHours(-(getHour + x - 1)).ToString("HH:00:00")),
                            DateTIme = DateTime.Now.AddHours(-(getHour + x - 1)).ToShortDateString() + " " + DateTime.Now.AddHours(-(getHour + x - 1)).ToString("HH:00:00")
                        });
                    }
                    catch
                    { }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetBidOffer(int UnitID, string UnitNumber)
        {
            List<Bid> bidList = new List<Bid>();
            bidList = ReadExcel(@"c:\BIDS\BidOffer" + UnitID.ToString() + ".xlsx", UnitNumber);

            return Request.CreateResponse(HttpStatusCode.OK, bidList);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetBidOfferNew(int UnitID, string UnitNumber)
        {
            List<Bid> bidList = new List<Bid>();
            bidList = ReadExcel(@"c:\BIDS\BidOffer" + UnitID.ToString() + ".xlsx", UnitNumber);
            bidList = SaveOffers(bidList, UnitNumber);
            return Request.CreateResponse(HttpStatusCode.OK, bidList);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetBidOfferByDateNew(string UnitNumber, string DateSchedule)
        {
            List<Bid> bidList = new List<Bid>();
            bidList = GetOffers(UnitNumber, Convert.ToDateTime(DateSchedule));
            return Request.CreateResponse(HttpStatusCode.OK, bidList);
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult CreateOfferNew([FromBody] OfferEntry offer)
        {
            List<Bid> offers = new List<Bid>();
            using (TradingEntities db = new TradingEntities())
            {
                string query = "";
                try
                {
                    query = $@"delete from t_BidOffer where UnitNumber = '{offer.UnitNumber}' and DateSchedule = '{offer.Date.ToShortDateString()}'";
                    db.Database.ExecuteSqlCommand(query);
                }
                catch { }

                if (offer.IsWithOffer == true)
                {
                    query = $@"exec [sp_InsertBidOffers] '{offer.UnitNumber}','{(offer.PreviousDate ?? DateTime.Now).ToShortDateString()}','{offer.Date.ToShortDateString()}'";
                    db.Database.ExecuteSqlCommand(query);
                }
                else
                {
                    for (int i = 1; i <= 24; i++)
                    {
                        query = $@"insert into t_BidOffer
                                ([UnitNumber],[DateSchedule],[Interval],
                                [P1],[P2],[P3],[P4],[P5],[P6],[P7],[P8],
                                [P9],[P10],[P11],[Q1],[Q2],[Q3],[Q4],[Q5],
                                [Q6],[Q7],[Q8],[Q9],[Q10],[Q11],
                                [AS_RU_Q1],[AS_RU_Q2],[AS_RU_Q3],[AS_RU_Q4],[AS_RU_Q5],
                                [AS_RU_P1],[AS_RU_P2],[AS_RU_P3],[AS_RU_P4],[AS_RU_P5],
                                [AS_RD_Q1],[AS_RD_Q2],[AS_RD_Q3],[AS_RD_Q4],[AS_RD_Q5],
                                [AS_RD_P1],[AS_RD_P2],[AS_RD_P3],[AS_RD_P4],[AS_RD_P5],
                                [AS_FR_Q1],[AS_FR_Q2],[AS_FR_Q3],[AS_FR_Q4],[AS_FR_Q5],
                                [AS_FR_P1],[AS_FR_P2],[AS_FR_P3],[AS_FR_P4],[AS_FR_P5],
                                [AS_DR_Q1],[AS_DR_Q2],[AS_DR_Q3],[AS_DR_Q4],[AS_DR_Q5],
                                [AS_DR_P1],[AS_DR_P2],[AS_DR_P3],[AS_DR_P4],[AS_DR_P5])

                                values('{offer.UnitNumber}','{offer.Date.ToShortDateString()}',{i.ToString()},
                                NULL,NULL,NULL,NULL,NULL,NULL,
                                NULL,NULL,NULL,NULL,NULL,
                                NULL,NULL,NULL,NULL,NULL,NULL,
                                NULL,NULL,NULL,NULL,NULL,
                                NULL,NULL,NULL,NULL,NULL,
                                NULL,NULL,NULL,NULL,NULL,
                                NULL,NULL,NULL,NULL,NULL,
                                NULL,NULL,NULL,NULL,NULL,
                                NULL,NULL,NULL,NULL,NULL,
                                NULL,NULL,NULL,NULL,NULL,
                                NULL,NULL,NULL,NULL,NULL,
                                NULL,NULL,NULL,NULL,NULL)";
                        db.Database.ExecuteSqlCommand(query);
                    }
                }
                

                if (offer.IsWithRampRate == true)
                {
                    query = $@"exec [sp_InsertBidRampRate] '{offer.UnitNumber}','{(offer.PreviousDate ?? DateTime.Now).ToShortDateString()}','{offer.Date.ToShortDateString()}'";
                    db.Database.ExecuteSqlCommand(query);
                }
                else
                {
                    query = $@"exec [sp_InsertRampRate] '{offer.UnitNumber}','{offer.Date.ToShortDateString()}',
                          NULL,NULL,NULL,NULL,NULL,
                          NULL,NULL,NULL,NULL,NULL,
                          NULL,NULL,NULL,NULL,NULL";
                    db.Database.ExecuteSqlCommand(query);
                }
                
                offers = GetOffers(offer.UnitNumber, offer.Date);
            }
            return Content(HttpStatusCode.OK, offers);
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult CreateBidNew([FromBody] BidUpload bid)
        {
            string message = "";
            string query = "";
            string webpUrl = "https://mpiwebp.iemop.ph/SiemensServices/SAFServiceImpl";
            string webbUrl = "https://mpiwebb.iemop.ph/SiemensServices/SAFServiceImpl";
            string url = webpUrl;
            bool isPrimary = true;
            WebServiceSet webServiceSetting = new WebServiceSet();
            WebServiceUrl webUrl = new WebServiceUrl();
            List<BidDataTable> bidList = new List<BidDataTable>();
            Bid offer = new Bid();
            List<Bid> offerList = new List<Bid>();
            offer = bid.Offers[0];

            using (TradingEntities db = new TradingEntities())
            {
                //query = "select * from [m_WebServiceURL] where isactive = 1 and Type = 'Upload'";
                //webUrl = db.Database.SqlQuery<WebServiceUrl>(query).FirstOrDefault<WebServiceUrl>();

                query = "select top 1 * from m_WebServiceSettings" +
                            " where UnitID = " + bid.UnitID + " and IsActive = 1";
                webServiceSetting = db.Database.SqlQuery<WebServiceSet>(query).FirstOrDefault<WebServiceSet>();
                bidList = GetOffersForDataTable(bid.Offers);

                if (webServiceSetting.BidType == "Offer")
                {
                    message = CreateOfferXML(bidList, webServiceSetting.TpUser.ToString(), HttpContext.Current.Server.MapPath(@"~\Files\XML Offer\BidOffer" + bid.UnitID + ".xml"), bid.UnitNumber);
                }
                else if (webServiceSetting.BidType == "BatteryOffer")
                {
                    message = CreateOfferBatteryXML(bidList, webServiceSetting.TpUser.ToString(), HttpContext.Current.Server.MapPath(@"~\Files\XML Offer\BidOffer" + bid.UnitID + ".xml"), bid.UnitNumber, bid.ControlMode);
                }
                else
                {
                    message = CreateOfferXML(bidList, webServiceSetting.TpUser.ToString(), HttpContext.Current.Server.MapPath(@"~\Files\XML Offer\BidOffer" + bid.UnitID + ".xml"), bid.UnitNumber);
                }

                System.Threading.Thread.Sleep(7000);

                int maxRetries = 3;
                int attempt = 0;
                

                while (attempt < maxRetries)
                {
                    message = GlobalFunctions.invoke(
                        webServiceSetting.Operation,
                        url,
                        HttpContext.Current.Server.MapPath(@"~\Files\Certificates\" + webServiceSetting.CertificateURL),
                        webServiceSetting.Password,
                        webServiceSetting.FriendlyName,
                        HttpContext.Current.Server.MapPath(@"~\Files\XML Offer\BidOffer" + bid.UnitID + ".xml"),
                        HttpContext.Current.Server.MapPath(@"~\Files\submitBidResponse.txt"));

                    if (message.Substring(0, 7) == "Success")
                        break;

                    isPrimary = !isPrimary;
                    url = isPrimary ? webpUrl : webbUrl;
                    attempt++;
                }

                //message = GlobalFunctions
                //    .invoke(webServiceSetting.Operation,
                //    webUrl.URL,
                //    HttpContext.Current.Server.MapPath(@"~\Files\Certificates\" + webServiceSetting.CertificateURL),
                //    webServiceSetting.Password, webServiceSetting.FriendlyName,
                //    HttpContext.Current.Server.MapPath(@"~\Files\XML Offer\BidOffer" + bid.UnitID + ".xml"),
                //    HttpContext.Current.Server.MapPath(@"~\Files\submitBidResponse.txt"));
                //if (message == "problem in IEMOP Webservice connection!")
                //{
                //    message = GlobalFunctions
                //        .invoke(webServiceSetting.Operation,
                //        webUrl.URL,
                //        HttpContext.Current.Server.MapPath(@"~\Files\Certificates\" + webServiceSetting.CertificateURL),
                //        webServiceSetting.Password, webServiceSetting.FriendlyName,
                //        HttpContext.Current.Server.MapPath(@"~\Files\XML Offer\BidOffer" + bid.UnitID + ".xml"),
                //        HttpContext.Current.Server.MapPath(@"~\Files\submitBidResponse.txt"));
                //}

                if (message.Substring(0, 7) == "Success")
                {
                    query = $@"update [t_BidRampRate]
                               set TransID = '{message.Substring(9, 7)}'
                                  ,DateTimeSubmitted = getdate()
                                  ,UploadedBy = '{offer.UploadedBy}'
                                where UnitNumber = '{bid.UnitNumber}' and DateSchedule = convert(date,'{offer.Date}')";
                    db.Database.ExecuteSqlCommand(query);

                    GlobalFunctions.Log(logType: "CREATE_BID",
                            action: "Create Bid Success",
                            message: $"Bid successfully created.UnitNumber: {bid.UnitNumber}, Schedule:{offer.Date}, TransID: {message.Substring(9, 7)},UploadedBy:{offer.UploadedBy}, DateTimeSubmitted: {DateTime.Now} ",
                            newValues: JsonConvert.SerializeObject(bidList),
                            userName: GetUsername());
                }
                else
                {
                    GlobalFunctions.Log(logType: "CREATE_BID",
                            action: "Create Bid Failed",
                            message: $"Bid failed to create. UnitNumber: {bid.UnitNumber}, Schedule:{offer.Date} ",
                            newValues: message,
                            userName: GetUsername());
                }

                offerList = GetOffers(bid.UnitNumber, offer.Date);
            }

            return Content(HttpStatusCode.OK,new {message=message,offers= offerList } );

        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult BidXML([FromBody] BidUpload bid)
        {
            string message = "";
            string query = "";
            WebServiceSet webServiceSetting = new WebServiceSet();
            WebServiceUrl webUrl = new WebServiceUrl();
            List<BidDataTable> bidList = new List<BidDataTable>();
            Bid offer = new Bid();
            List<Bid> offerList = new List<Bid>();

            offer = bid.Offers[0];

            using (TradingEntities db = new TradingEntities())
            {
                query = "select * from [m_WebServiceURL] where isactive = 1 and Type = 'Upload'";
                webUrl = db.Database.SqlQuery<WebServiceUrl>(query).FirstOrDefault<WebServiceUrl>();

                query = "select top 1 * from m_WebServiceSettings" +
                            " where UnitID = " + bid.UnitID + " and IsActive = 1";
                webServiceSetting = db.Database.SqlQuery<WebServiceSet>(query).FirstOrDefault<WebServiceSet>();
                bidList = GetOffersForDataTable(bid.Offers);

                if (webServiceSetting.BidType == "Offer")
                {
                    message = CreateOfferXML(bidList, webServiceSetting.TpUser.ToString(), HttpContext.Current.Server.MapPath(@"~\Files\XML Offer\BidOffer" + bid.UnitID + ".xml"), bid.UnitNumber);
                }
                else if (webServiceSetting.BidType == "BatteryOffer")
                {
                    message = CreateOfferBatteryXML(bidList, webServiceSetting.TpUser.ToString(), HttpContext.Current.Server.MapPath(@"~\Files\XML Offer\BidOffer" + bid.UnitID + ".xml"), bid.UnitNumber, bid.ControlMode);
                }
                else
                {
                    message = CreateOfferXML(bidList, webServiceSetting.TpUser.ToString(), HttpContext.Current.Server.MapPath(@"~\Files\XML Offer\BidOffer" + bid.UnitID + ".xml"), bid.UnitNumber);
                }
            }

            GlobalFunctions.Log(logType: "CREATE_BID_XML",
                            action: "Create Bid XML Success",
                            message: $"Bid xml successfully created. UnitNumber: {bid.UnitNumber}, Schedule:{offer.Date} ",
                            newValues: message,
                            userName: GetUsername());

            return Content(HttpStatusCode.OK, "success");
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult UpdateOffer([FromBody] BidUpload bid)
        {
            List<Bid> offers = new List<Bid>();
            offers = SaveOffers(bid.Offers, bid.UnitNumber);

            return Content(HttpStatusCode.OK, offers);
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult CreateBid([FromBody] BidUpload bid)
        {
            string message = "";
            string StrQuery = "";
            WebServiceSet webServiceSetting = new WebServiceSet();
            WebServiceUrl webUrl = new WebServiceUrl();

            using (TradingEntities db = new TradingEntities())
            {
                StrQuery = "select * from [m_WebServiceURL] where isactive = 1 and Type = 'Upload'";
                webUrl = db.Database.SqlQuery<WebServiceUrl>(StrQuery).FirstOrDefault<WebServiceUrl>();

                StrQuery = "select top 1 * from m_WebServiceSettings" +
                            " where UnitID = " + bid.UnitID + " and IsActive = 1";
                webServiceSetting = db.Database.SqlQuery<WebServiceSet>(StrQuery).FirstOrDefault<WebServiceSet>();
                List<BidDataTable> bidList = new List<BidDataTable>();
                bidList = ReadExcelForDataTable(@"c:\BIDS\BidOffer" + bid.UnitID + ".xlsx", bid.UnitNumber);
                if(webServiceSetting.BidType == "Offer")
                {
                    message = CreateOfferXML(bidList, webServiceSetting.TpUser.ToString(), HttpContext.Current.Server.MapPath(@"~\Files\XML Offer\BidOffer" + bid.UnitID + ".xml"), bid.UnitNumber);
                }
                else if(webServiceSetting.BidType == "BatteryOffer")
                {
                    message = CreateOfferBatteryXML(bidList, webServiceSetting.TpUser.ToString(), HttpContext.Current.Server.MapPath(@"~\Files\XML Offer\BidOffer" + bid.UnitID + ".xml"), bid.UnitNumber, bid.ControlMode);
                }
                else
                {
                    message = CreateOfferXML(bidList, webServiceSetting.TpUser.ToString(), HttpContext.Current.Server.MapPath(@"~\Files\XML Offer\BidOffer" + bid.UnitID + ".xml"), bid.UnitNumber);
                }
                
                System.Threading.Thread.Sleep(80000);

                message = GlobalFunctions
                    .invoke(webServiceSetting.Operation,
                    webUrl.URL,
                    HttpContext.Current.Server.MapPath(@"~\Files\Certificates\" + webServiceSetting.CertificateURL),
                    webServiceSetting.Password, webServiceSetting.FriendlyName,
                    HttpContext.Current.Server.MapPath(@"~\Files\XML Offer\BidOffer" + bid.UnitID + ".xml"),
                    HttpContext.Current.Server.MapPath(@"~\Files\submitBidResponse.txt"));
                if (message == "problem in IEMOP Webservice connection!")
                {
                    message = GlobalFunctions
                        .invoke(webServiceSetting.Operation,
                        webUrl.URL, 
                        HttpContext.Current.Server.MapPath(@"~\Files\Certificates\" + webServiceSetting.CertificateURL), 
                        webServiceSetting.Password, webServiceSetting.FriendlyName, 
                        HttpContext.Current.Server.MapPath(@"~\Files\XML Offer\BidOffer" + bid.UnitID + ".xml"), 
                        HttpContext.Current.Server.MapPath(@"~\Files\submitBidResponse.txt"));
                }
            }

            return Content(HttpStatusCode.OK, new { message = message });

        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetRRStandard(string UnitNumber)
        {
            RRStandard rrStandard = new RRStandard();
            rrStandard = SingleRRStandard(UnitNumber);
            return Request.CreateResponse(HttpStatusCode.OK, rrStandard);
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult SetRRStandard([FromBody] RRStandard standard)
        {
            RRStandard rrStandard = new RRStandard();
            using(TradingEntities db = new TradingEntities())
            {
                string query = $@"update m_Unit
                                    set RRUp = {standard.RRUp},
                                        RRDown = {standard.RRDown},
                                        RRMax = {standard.RRMax},
                                        PMax = {standard.PMax}
                                 where UnitNumber = '{standard.UnitNumber}' and isactive = 1";
                db.Database.ExecuteSqlCommand(query);
                rrStandard = SingleRRStandard(standard.UnitNumber);
            }
            GlobalFunctions.Log(logType: "SET_RR_STANDARD",
                           action: "Set RR Standard Success",
                           message: $"RR Standard updated successfully",
                           newValues: JsonConvert.SerializeObject(standard),
                           userName: GetUsername());
            return Content(HttpStatusCode.OK, rrStandard);
        }


        public RRStandard SingleRRStandard(string UnitNumber)
        {
            RRStandard rrStandard = new RRStandard();
            using(TradingEntities db = new TradingEntities())
            {
                string query = $@"select top 1 RRUp,RRDown,RRMax,PMax from m_Unit where UnitNumber = '{UnitNumber}' and IsActive = 1";
                rrStandard = db.Database.SqlQuery<RRStandard>(query).FirstOrDefault();
            }

            GlobalFunctions.Log(logType: "GET_RR_STANDARD",
                           action: "Get RR Standard Success",
                           message: $"RR Standard retrieved successfully",
                           newValues: $"UnitNumber: {UnitNumber}",
                           userName: GetUsername());
            return rrStandard;
        }

        public List<Bid> ReadExcel(string FilePath, string UnitNumber)
        {
            List<Bid> bid = new List<Bid>();
            string fileName = Path.GetFileNameWithoutExtension(FilePath);

            IWorkbook workbook = null;
            FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (FilePath.IndexOf(".xlsx") > 0)
                workbook = new XSSFWorkbook(fs);

            else if (FilePath.IndexOf(".xls") > 0)
                workbook = new HSSFWorkbook(fs);

            else if (FilePath.IndexOf(".XLS") > 0)
                workbook = new HSSFWorkbook(fs);
            //First sheet
            ISheet sheet = workbook.GetSheet("Sheet1");

            if (sheet != null)
            {
                int rowCount = 24;
                for (int i = 1; i <= rowCount; i++)
                {
                    IRow curRow = sheet.GetRow(i);
                    IRow rr1 = sheet.GetRow(1);
                    IRow rr2 = sheet.GetRow(2);
                    IRow rr3 = sheet.GetRow(3);
                    IRow rr4 = sheet.GetRow(4);
                    IRow rr5 = sheet.GetRow(5);
                    //Date Column
                    int col = 0;
                    bid.Add(new Bid
                    {
                        Date = GetCellDateValue(curRow, rowCount, col++),
                        DateFormatted = GetCellDateValue(curRow, rowCount, col).ToString("yyyy-MM-dd"),
                        Interval = Convert.ToInt32(GetCellValue(curRow, rowCount, col++)),
                        ResourceID = UnitNumber,
                        ProducType = "EN",

                        P1 = GetCellValue(curRow, rowCount, col++),
                        Q1 = GetCellValue(curRow, rowCount, col++),

                        P2 = GetCellValue(curRow, rowCount, col++),
                        Q2 = GetCellValue(curRow, rowCount, col++),

                        P3 = GetCellValue(curRow, rowCount, col++),
                        Q3 = GetCellValue(curRow, rowCount, col++),

                        P4 = GetCellValue(curRow, rowCount, col++),
                        Q4 = GetCellValue(curRow, rowCount, col++),

                        P5 = GetCellValue(curRow, rowCount, col++),
                        Q5 = GetCellValue(curRow, rowCount, col++),

                        P6 = GetCellValue(curRow, rowCount, col++),
                        Q6 = GetCellValue(curRow, rowCount, col++),

                        P7 = GetCellValue(curRow, rowCount, col++),
                        Q7 = GetCellValue(curRow, rowCount, col++),

                        P8 = GetCellValue(curRow, rowCount, col++),
                        Q8 = GetCellValue(curRow, rowCount, col++),

                        P9 = GetCellValue(curRow, rowCount, col++),
                        Q9 = GetCellValue(curRow, rowCount, col++),

                        P10 = GetCellValue(curRow, rowCount, col++),
                        Q10 = GetCellValue(curRow, rowCount, col++),

                        P11 = GetCellValue(curRow, rowCount, col++),
                        Q11 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P5 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P5 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P5 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P5 = GetCellValue(curRow, rowCount, col++),

                        ControlMode = GetCellStringValue(curRow, rowCount, col++),

                        RampQuantity1 = GetCellValue(rr1, rowCount, 66),
                        RRU1 = GetCellValue(rr1, rowCount, 67),
                        RRD1 = GetCellValue(rr1, rowCount, 68),

                        RampQuantity2 = GetCellValue(rr2, rowCount, 66),
                        RRU2 = GetCellValue(rr2, rowCount, 67),
                        RRD2 = GetCellValue(rr2, rowCount, 68),

                        RampQuantity3 = GetCellValue(rr3, rowCount, 66),
                        RRU3 = GetCellValue(rr3, rowCount, 67),
                        RRD3 = GetCellValue(rr3, rowCount, 68),

                        RampQuantity4 = GetCellValue(rr4, rowCount, 66),
                        RRU4 = GetCellValue(rr4, rowCount, 67),
                        RRD4 = GetCellValue(rr4, rowCount, 68),

                        RampQuantity5 = GetCellValue(rr5, rowCount, 66),
                        RRU5 = GetCellValue(rr5, rowCount, 67),
                        RRD5 = GetCellValue(rr5, rowCount, 68)
                    });

                }
            }

            return bid;
        }

        public List<Bid> SaveOffers(List<Bid> bids, string UnitNumber)
        {
            List<Bid> offers = new List<Bid>();
            Bid bid = bids.FirstOrDefault();
            DateTime dateSched = bid.Date;

            using (TradingEntities db = new TradingEntities())
            {
                string query = "";
                query = $@"delete from t_BidOffer where UnitNumber = '{UnitNumber}' and DateSchedule = '{dateSched}'";
                db.Database.ExecuteSqlCommand(query);

                foreach (var item in bids)
                {
                    query = $@"insert into t_BidOffer
                                ([UnitNumber],[DateSchedule],[Interval],
                                [P1],[P2],[P3],[P4],[P5],[P6],[P7],[P8],
                                [P9],[P10],[P11],[Q1],[Q2],[Q3],[Q4],[Q5],
                                [Q6],[Q7],[Q8],[Q9],[Q10],[Q11],
                                [AS_RU_Q1],[AS_RU_Q2],[AS_RU_Q3],[AS_RU_Q4],[AS_RU_Q5],
                                [AS_RU_P1],[AS_RU_P2],[AS_RU_P3],[AS_RU_P4],[AS_RU_P5],
                                [AS_RD_Q1],[AS_RD_Q2],[AS_RD_Q3],[AS_RD_Q4],[AS_RD_Q5],
                                [AS_RD_P1],[AS_RD_P2],[AS_RD_P3],[AS_RD_P4],[AS_RD_P5],
                                [AS_FR_Q1],[AS_FR_Q2],[AS_FR_Q3],[AS_FR_Q4],[AS_FR_Q5],
                                [AS_FR_P1],[AS_FR_P2],[AS_FR_P3],[AS_FR_P4],[AS_FR_P5],
                                [AS_DR_Q1],[AS_DR_Q2],[AS_DR_Q3],[AS_DR_Q4],[AS_DR_Q5],
                                [AS_DR_P1],[AS_DR_P2],[AS_DR_P3],[AS_DR_P4],[AS_DR_P5],ControlMode)

                                values('{UnitNumber}','{dateSched}',{item.Interval},
                                {BidValue(item.P1)},{BidValue(item.P2)},{BidValue(item.P3)},{BidValue(item.P4)},{BidValue(item.P5)},{BidValue(item.P6)},
                                {BidValue(item.P7)},{BidValue(item.P8)},{BidValue(item.P9)},{BidValue(item.P10)},{BidValue(item.P11)},
                                {BidValue(item.Q1)},{BidValue(item.Q2)},{BidValue(item.Q3)},{BidValue(item.Q4)},{BidValue(item.Q5)},{BidValue(item.Q6)},
                                {BidValue(item.Q7)},{BidValue(item.Q8)},{BidValue(item.Q9)},{BidValue(item.Q10)},{BidValue(item.Q11)},
                                {BidValue(item.AS_RU_Q1)},{BidValue(item.AS_RU_Q2)},{BidValue(item.AS_RU_Q3)},{BidValue(item.AS_RU_Q4)},{BidValue(item.AS_RU_Q5)},
                                {BidValue(item.AS_RU_P1)},{BidValue(item.AS_RU_P2)},{BidValue(item.AS_RU_P3)},{BidValue(item.AS_RU_P4)},{BidValue(item.AS_RU_P5)},
                                {BidValue(item.AS_RD_Q1)},{BidValue(item.AS_RD_Q2)},{BidValue(item.AS_RD_Q3)},{BidValue(item.AS_RD_Q4)},{BidValue(item.AS_RD_Q5)},
                                {BidValue(item.AS_RD_P1)},{BidValue(item.AS_RD_P2)},{BidValue(item.AS_RD_P3)},{BidValue(item.AS_RD_P4)},{BidValue(item.AS_RD_P5)},
                                {BidValue(item.AS_FR_Q1)},{BidValue(item.AS_FR_Q2)},{BidValue(item.AS_FR_Q3)},{BidValue(item.AS_FR_Q4)},{BidValue(item.AS_FR_Q5)},
                                {BidValue(item.AS_FR_P1)},{BidValue(item.AS_FR_P2)},{BidValue(item.AS_FR_P3)},{BidValue(item.AS_FR_P4)},{BidValue(item.AS_FR_P5)},
                                {BidValue(item.AS_DR_Q1)},{BidValue(item.AS_DR_Q2)},{BidValue(item.AS_DR_Q3)},{BidValue(item.AS_DR_Q4)},{BidValue(item.AS_DR_Q5)},
                                {BidValue(item.AS_DR_P1)},{BidValue(item.AS_DR_P2)},{BidValue(item.AS_DR_P3)},{BidValue(item.AS_DR_P4)},{BidValue(item.AS_DR_P5)},'{item.ControlMode}')";
                    db.Database.ExecuteSqlCommand(query);
                }

                query = $@"exec [sp_InsertRampRate] '{UnitNumber}','{dateSched}',
                          {BidValue(bid.RampQuantity1)},{BidValue(bid.RampQuantity2)},{BidValue(bid.RampQuantity3)},{BidValue(bid.RampQuantity4)},{BidValue(bid.RampQuantity5)},
                          {BidValue(bid.RRU1)},{BidValue(bid.RRU2)},{BidValue(bid.RRU3)},{BidValue(bid.RRU4)},{BidValue(bid.RRU5)},
                          {BidValue(bid.RRD1)},{BidValue(bid.RRD2)},{BidValue(bid.RRD3)},{BidValue(bid.RRD4)},{BidValue(bid.RRD5)}";
                db.Database.ExecuteSqlCommand(query);

                offers = GetOffers(UnitNumber, dateSched);
            }

            GlobalFunctions.Log(logType: "UPDATE_BID_OFFER",
                            action: "Update Bid Offer Success",
                            message: $"Bid offer successfully updated. UnitNumber: {UnitNumber}, Schedule:{dateSched} ",
                            newValues: JsonConvert.SerializeObject(bids),
                            userName: GetUsername());

            return offers;
        }

        public string BidValue(Nullable<double> val)
        {
            string valStr = "";
            if(val == null)
            {
                valStr = "NULL";
            }
            else
            {
                valStr = val.ToString();
            }

            return valStr;
        }

        public List<Bid> GetOffers(string UnitNumber, DateTime dt)
        {
            List<Bid> offers = new List<Bid>();
            List<OfferDB> offerDb = new List<OfferDB>();
            RampRateDB rrDb = new RampRateDB();

            using (TradingEntities db = new TradingEntities())
            {
                string query = $@"exec [sp_Offers] '{UnitNumber}','{dt.ToShortDateString()}'";
                offerDb = db.Database.SqlQuery<OfferDB>(query).ToList<OfferDB>();

                query = $@"exec [sp_RampRate] '{UnitNumber}','{dt.ToShortDateString()}'";
                rrDb = db.Database.SqlQuery<RampRateDB>(query).FirstOrDefault<RampRateDB>();

                foreach (var item in offerDb)
                {
                    offers.Add(new Bid {
                        Date = offerDb[0].DateSchedule,
                        DateFormatted = offerDb[0].DateSchedule.ToString("yyyy-MM-dd"),
                        Interval = item.Interval,
                        ResourceID = item.UnitNumber,
                        ProducType = "EN",
                        P1 = item.P1,
                        P2 = item.P2,
                        P3 = item.P3,
                        P4 = item.P4,
                        P5 = item.P5,
                        P6 = item.P6,
                        P7 = item.P7,
                        P8 = item.P8,
                        P9 = item.P9,
                        P10 = item.P10,
                        P11 = item.P11,
                        Q1 = item.Q1,
                        Q2 = item.Q2,
                        Q3 = item.Q3,
                        Q4 = item.Q4,
                        Q5 = item.Q5,
                        Q6 = item.Q6,
                        Q7 = item.Q7,
                        Q8 = item.Q8,
                        Q9 = item.Q9,
                        Q10 = item.Q10,
                        Q11 = item.Q11,
                        AS_RU_Q1 = item.AS_RU_Q1,
                        AS_RU_P1 = item.AS_RU_P1,
                        AS_RU_Q2 = item.AS_RU_Q2,
                        AS_RU_P2 = item.AS_RU_P2,
                        AS_RU_Q3 = item.AS_RU_Q3,
                        AS_RU_P3 = item.AS_RU_P3,
                        AS_RU_Q4 = item.AS_RU_Q4,
                        AS_RU_P4 = item.AS_RU_P4,
                        AS_RU_Q5 = item.AS_RU_Q5,
                        AS_RU_P5 = item.AS_RU_P5,
                        AS_RD_Q1 = item.AS_RD_Q1,
                        AS_RD_P1 = item.AS_RD_P1,
                        AS_RD_Q2 = item.AS_RD_Q2,
                        AS_RD_P2 = item.AS_RD_P2,
                        AS_RD_Q3 = item.AS_RD_Q3,
                        AS_RD_P3 = item.AS_RD_P3,
                        AS_RD_Q4 = item.AS_RD_Q4,
                        AS_RD_P4 = item.AS_RD_P4,
                        AS_RD_Q5 = item.AS_RD_Q5,
                        AS_RD_P5 = item.AS_RD_P5,
                        AS_FR_Q1 = item.AS_FR_Q1,
                        AS_FR_P1 = item.AS_FR_P1,
                        AS_FR_Q2 = item.AS_FR_Q2,
                        AS_FR_P2 = item.AS_FR_P2,
                        AS_FR_Q3 = item.AS_FR_Q3,
                        AS_FR_P3 = item.AS_FR_P3,
                        AS_FR_Q4 = item.AS_FR_Q4,
                        AS_FR_P4 = item.AS_FR_P4,
                        AS_FR_Q5 = item.AS_FR_Q5,
                        AS_FR_P5 = item.AS_FR_P5,
                        AS_DR_Q1 = item.AS_DR_Q1,
                        AS_DR_P1 = item.AS_DR_P1,
                        AS_DR_Q2 = item.AS_DR_Q2,
                        AS_DR_P2 = item.AS_DR_P2,
                        AS_DR_Q3 = item.AS_DR_Q3,
                        AS_DR_P3 = item.AS_DR_P3,
                        AS_DR_Q4 = item.AS_DR_Q4,
                        AS_DR_P4 = item.AS_DR_P4,
                        AS_DR_Q5 = item.AS_DR_Q5,
                        AS_DR_P5 = item.AS_DR_P5,
                        ControlMode = item.ControlMode,
                        RampQuantity1 = rrDb.RRMW1,
                        RRU1 = rrDb.RRRU1,
                        RRD1 = rrDb.RRRD1,
                        RampQuantity2 = rrDb.RRMW2,
                        RRU2 = rrDb.RRRU2,
                        RRD2 = rrDb.RRRD2,
                        RampQuantity3 = rrDb.RRMW3,
                        RRU3 = rrDb.RRRU3,
                        RRD3 = rrDb.RRRD3,
                        RampQuantity4 = rrDb.RRMW4,
                        RRU4 = rrDb.RRRU4,
                        RRD4 = rrDb.RRRD4,
                        RampQuantity5 = rrDb.RRMW5,
                        RRU5 = rrDb.RRRU5,
                        RRD5 = rrDb.RRRD5,
                        TransID = rrDb.TransID,
                        DateTimeSubmitted = Convert.ToString(rrDb.DateTimeSubmitted),
                        UploadedBy = rrDb.UploadedBy
                    });
                }
            }

            GlobalFunctions.Log(logType: "GET_BID_OFFER",
                           action: "Bid Offer Retrieved Success",
                           message: "Bid Offer retrieved successfully",
                           newValues: $"Date: {dt.ToShortDateString()}",
                           userName: GetUsername());
            return offers;
        }

        public string UpdateExcel(List<Bid> bidList, string FilePath)
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }

            string fileName = Path.GetFileNameWithoutExtension(FilePath);
            string msg="";
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("Sheet1");
            try
            {
                using (FileStream fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
                {
                    int r = 1;
                    foreach (var item in bidList)
                    {
                        IRow curRow = sheet.CreateRow(r);
                        int col = 0;
                        CreateCell(curRow,col++, Convert.ToDateTime(item.DateFormatted).ToShortDateString());
                        CreateCell(curRow,col++, r.ToString());
                        CreateCell(curRow, col++, item.P1.ToString());
                        CreateCell(curRow, col++, item.Q1.ToString());
                        CreateCell(curRow, col++, item.P2.ToString());
                        CreateCell(curRow, col++, item.Q2.ToString());
                        CreateCell(curRow, col++, item.P3.ToString());
                        CreateCell(curRow, col++, item.Q3.ToString());
                        CreateCell(curRow, col++, item.P4.ToString());
                        CreateCell(curRow, col++, item.Q4.ToString());
                        CreateCell(curRow, col++, item.P5.ToString());
                        CreateCell(curRow, col++, item.Q5.ToString());
                        CreateCell(curRow, col++, item.P6.ToString());
                        CreateCell(curRow, col++, item.Q6.ToString());
                        CreateCell(curRow, col++, item.P7.ToString());
                        CreateCell(curRow, col++, item.Q7.ToString());
                        CreateCell(curRow, col++, item.P8.ToString());
                        CreateCell(curRow, col++, item.Q8.ToString());
                        CreateCell(curRow, col++, item.P9.ToString());
                        CreateCell(curRow, col++, item.Q9.ToString());
                        CreateCell(curRow, col++, item.P10.ToString());
                        CreateCell(curRow, col++, item.Q10.ToString());
                        CreateCell(curRow, col++, item.P11.ToString());
                        CreateCell(curRow, col++, item.Q11.ToString());

                        CreateCell(curRow, col++, item.AS_RU_Q1.ToString());
                        CreateCell(curRow, col++, item.AS_RU_P1.ToString());
                        CreateCell(curRow, col++, item.AS_RU_Q2.ToString());
                        CreateCell(curRow, col++, item.AS_RU_P2.ToString());
                        CreateCell(curRow, col++, item.AS_RU_Q3.ToString());
                        CreateCell(curRow, col++, item.AS_RU_P3.ToString());
                        CreateCell(curRow, col++, item.AS_RU_Q4.ToString());
                        CreateCell(curRow, col++, item.AS_RU_P4.ToString());
                        CreateCell(curRow, col++, item.AS_RU_Q5.ToString());
                        CreateCell(curRow, col++, item.AS_RU_P5.ToString());

                        CreateCell(curRow, col++, item.AS_RD_Q1.ToString());
                        CreateCell(curRow, col++, item.AS_RD_P1.ToString());
                        CreateCell(curRow, col++, item.AS_RD_Q2.ToString());
                        CreateCell(curRow, col++, item.AS_RD_P2.ToString());
                        CreateCell(curRow, col++, item.AS_RD_Q3.ToString());
                        CreateCell(curRow, col++, item.AS_RD_P3.ToString());
                        CreateCell(curRow, col++, item.AS_RD_Q4.ToString());
                        CreateCell(curRow, col++, item.AS_RD_P4.ToString());
                        CreateCell(curRow, col++, item.AS_RD_Q5.ToString());
                        CreateCell(curRow, col++, item.AS_RD_P5.ToString());

                        CreateCell(curRow, col++, item.AS_FR_Q1.ToString());
                        CreateCell(curRow, col++, item.AS_FR_P1.ToString());
                        CreateCell(curRow, col++, item.AS_FR_Q2.ToString());
                        CreateCell(curRow, col++, item.AS_FR_P2.ToString());
                        CreateCell(curRow, col++, item.AS_FR_Q3.ToString());
                        CreateCell(curRow, col++, item.AS_FR_P3.ToString());
                        CreateCell(curRow, col++, item.AS_FR_Q4.ToString());
                        CreateCell(curRow, col++, item.AS_FR_P4.ToString());
                        CreateCell(curRow, col++, item.AS_FR_Q5.ToString());
                        CreateCell(curRow, col++, item.AS_FR_P5.ToString());

                        CreateCell(curRow, col++, item.AS_DR_Q1.ToString());
                        CreateCell(curRow, col++, item.AS_DR_P1.ToString());
                        CreateCell(curRow, col++, item.AS_DR_Q2.ToString());
                        CreateCell(curRow, col++, item.AS_DR_P2.ToString());
                        CreateCell(curRow, col++, item.AS_DR_Q3.ToString());
                        CreateCell(curRow, col++, item.AS_DR_P3.ToString());
                        CreateCell(curRow, col++, item.AS_DR_Q4.ToString());
                        CreateCell(curRow, col++, item.AS_DR_P4.ToString());
                        CreateCell(curRow, col++, item.AS_DR_Q5.ToString());
                        CreateCell(curRow, col++, item.AS_DR_P5.ToString());


                        if (r == 1)
                        {
                            CreateCell(curRow,66, item.RampQuantity1.ToString());
                            CreateCell(curRow,67, item.RRU1.ToString());
                            CreateCell(curRow,68, item.RRD1.ToString());
                        }else if (r == 2)
                        {
                            CreateCell(curRow,66, item.RampQuantity2.ToString());
                            CreateCell(curRow,67, item.RRU2.ToString());
                            CreateCell(curRow,68, item.RRD2.ToString());
                        }
                        else if (r == 3)
                        {
                            CreateCell(curRow,66, item.RampQuantity3.ToString());
                            CreateCell(curRow,67, item.RRU3.ToString());
                            CreateCell(curRow,68, item.RRD3.ToString());
                        }
                        else if (r == 4)
                        {
                            CreateCell(curRow,66, item.RampQuantity4.ToString());
                            CreateCell(curRow,67, item.RRU4.ToString());
                            CreateCell(curRow,68, item.RRD4.ToString());
                        }
                        else if (r == 5)
                        {
                            CreateCell(curRow,66, item.RampQuantity5.ToString());
                            CreateCell(curRow,67, item.RRU5.ToString());
                            CreateCell(curRow,68, item.RRD5.ToString());
                        }

                        r++;
                    }

                    workbook.Write(fs);
                }
            }
            catch(Exception e)
            {
                msg = e.Message.ToString();
            }

            return msg;
        }

        public void CreateCell(IRow curRow,int col, string value)
        {
            ICell cell = curRow.CreateCell(col);
            if(value != null || value != "")
            {
                cell.SetCellValue(value);
            }
            
        }

        public List<BidDataTable> ReadExcelForDataTable(string FilePath, string UnitNumber)
        {
            List<BidDataTable> bid = new List<BidDataTable>();
            string fileName = Path.GetFileNameWithoutExtension(FilePath);

            IWorkbook workbook = null;
            FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);

            if (FilePath.IndexOf(".xlsx") > 0)
                workbook = new XSSFWorkbook(fs);

            else if (FilePath.IndexOf(".xls") > 0)
                workbook = new HSSFWorkbook(fs);

            else if (FilePath.IndexOf(".XLS") > 0)
                workbook = new HSSFWorkbook(fs);
            //First sheet

            ISheet sheet = workbook.GetSheet("Sheet1");

            if (sheet != null)
            {
                int rowCount = 24;
                for (int i = 1; i <= rowCount; i++)
                {
                    IRow curRow = sheet.GetRow(i);
                    //Date Column
                    int col = 0;
                    bid.Add(new BidDataTable
                    {
                        Date = GetCellDateValue(curRow, rowCount, col++),
                        Hour = Convert.ToInt32(GetCellValue(curRow, rowCount, col++)),

                        P1 = GetCellValue(curRow, rowCount, col++),
                        Q1 = GetCellValue(curRow, rowCount, col++),

                        P2 = GetCellValue(curRow, rowCount, col++),
                        Q2 = GetCellValue(curRow, rowCount, col++),

                        P3 = GetCellValue(curRow, rowCount, col++),
                        Q3 = GetCellValue(curRow, rowCount, col++),

                        P4 = GetCellValue(curRow, rowCount, col++),
                        Q4 = GetCellValue(curRow, rowCount, col++),

                        P5 = GetCellValue(curRow, rowCount, col++),
                        Q5 = GetCellValue(curRow, rowCount, col++),

                        P6 = GetCellValue(curRow, rowCount, col++),
                        Q6 = GetCellValue(curRow, rowCount, col++),

                        P7 = GetCellValue(curRow, rowCount, col++),
                        Q7 = GetCellValue(curRow, rowCount, col++),

                        P8 = GetCellValue(curRow, rowCount, col++),
                        Q8 = GetCellValue(curRow, rowCount, col++),

                        P9 = GetCellValue(curRow, rowCount, col++),
                        Q9 = GetCellValue(curRow, rowCount, col++),

                        P10 = GetCellValue(curRow, rowCount, col++),
                        Q10 = GetCellValue(curRow, rowCount, col++),

                        P11 = GetCellValue(curRow, rowCount, col++),
                        Q11 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_RU_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_RU_P5 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_RD_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_RD_P5 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_FR_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_FR_P5 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q1 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P1 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q2 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P2 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q3 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P3 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q4 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P4 = GetCellValue(curRow, rowCount, col++),

                        AS_DR_Q5 = GetCellValue(curRow, rowCount, col++),
                        AS_DR_P5 = GetCellValue(curRow, rowCount, col++),

                        ControlMode = GetCellStringValue(curRow, rowCount, col++),

                        RampRate = "",
                        MW = i <= 5 ? GetCellValue(curRow, rowCount, 66) : null,
                        RU = i <= 5 ? GetCellValue(curRow, rowCount, 67) : null,
                        RD = i <= 5 ? GetCellValue(curRow, rowCount, 68) : null
                        
                    });

                }
            }

            return bid;
        }

        public List<BidDataTable> GetOffersForDataTable(List<Bid> offers)
        {
            List<BidDataTable> bid = new List<BidDataTable>();
            foreach(var item in offers)
            {
                bid.Add(new BidDataTable
                {
                    Date = offers[0].Date,
                    Hour = item.Interval,

                    P1 = item.P1,
                    Q1 = item.Q1,

                    P2 = item.P2,
                    Q2 = item.Q2,

                    P3 = item.P3,
                    Q3 = item.Q3,

                    P4 = item.P4,
                    Q4 = item.Q4,

                    P5 = item.P5,
                    Q5 = item.Q5,

                    P6 = item.P6,
                    Q6 = item.Q6,

                    P7 = item.P7,
                    Q7 = item.Q7,

                    P8 = item.P8,
                    Q8 = item.Q8,

                    P9 = item.P9,
                    Q9 = item.Q9,

                    P10 = item.P10,
                    Q10 = item.Q10,

                    P11 = item.P11,
                    Q11 = item.Q11,

                    AS_RU_Q1 = item.AS_RU_Q1,
                    AS_RU_P1 = item.AS_RU_P1,

                    AS_RU_Q2 = item.AS_RU_Q2,
                    AS_RU_P2 = item.AS_RU_P2,

                    AS_RU_Q3 = item.AS_RU_Q3,
                    AS_RU_P3 = item.AS_RU_P3,

                    AS_RU_Q4 = item.AS_RU_Q4,
                    AS_RU_P4 = item.AS_RU_P4,

                    AS_RU_Q5 = item.AS_RU_Q5,
                    AS_RU_P5 = item.AS_RU_P5,

                    AS_RD_Q1 = item.AS_RD_Q1,
                    AS_RD_P1 = item.AS_RD_P1,

                    AS_RD_Q2 = item.AS_RD_Q2,
                    AS_RD_P2 = item.AS_RD_P2,

                    AS_RD_Q3 = item.AS_RD_Q3,
                    AS_RD_P3 = item.AS_RD_P3,

                    AS_RD_Q4 = item.AS_RD_Q4,
                    AS_RD_P4 = item.AS_RD_P4,

                    AS_RD_Q5 = item.AS_RD_Q5,
                    AS_RD_P5 = item.AS_RD_P5,

                    AS_FR_Q1 = item.AS_FR_Q1,
                    AS_FR_P1 = item.AS_FR_P1,

                    AS_FR_Q2 = item.AS_FR_Q2,
                    AS_FR_P2 = item.AS_FR_P2,

                    AS_FR_Q3 = item.AS_FR_Q3,
                    AS_FR_P3 = item.AS_FR_P3,

                    AS_FR_Q4 = item.AS_FR_Q4,
                    AS_FR_P4 = item.AS_FR_P4,

                    AS_FR_Q5 = item.AS_FR_Q5,
                    AS_FR_P5 = item.AS_FR_P5,

                    AS_DR_Q1 = item.AS_DR_Q1,
                    AS_DR_P1 = item.AS_DR_P1,

                    AS_DR_Q2 = item.AS_DR_Q2,
                    AS_DR_P2 = item.AS_DR_P2,

                    AS_DR_Q3 = item.AS_DR_Q3,
                    AS_DR_P3 = item.AS_DR_P3,

                    AS_DR_Q4 = item.AS_DR_Q4,
                    AS_DR_P4 = item.AS_DR_P4,

                    AS_DR_Q5 = item.AS_DR_Q5,
                    AS_DR_P5 = item.AS_DR_P5,

                    ControlMode = item.ControlMode,

                    RampRate = "",
                });
            }

            bid[0].MW = offers[0].RampQuantity1;
            bid[0].RD = offers[0].RRD1;
            bid[0].RU = offers[0].RRU1;

            bid[1].MW = offers[0].RampQuantity2;
            bid[1].RD = offers[0].RRD2;
            bid[1].RU = offers[0].RRU2;

            bid[2].MW = offers[0].RampQuantity3;
            bid[2].RD = offers[0].RRD3;
            bid[2].RU = offers[0].RRU3;

            bid[3].MW = offers[0].RampQuantity4;
            bid[3].RD = offers[0].RRD4;
            bid[3].RU = offers[0].RRU4;

            bid[4].MW = offers[0].RampQuantity5;
            bid[4].RD = offers[0].RRD5;
            bid[4].RU = offers[0].RRU5;

            return bid;
        }

        public string CreateOfferXML(List<BidDataTable> bid, string Tpuser, string Path, string UnitNumber)
        {
            string message = "";
            DataTable dt = new DataTable();
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(bid);
            dt = JsonConvert.DeserializeObject<DataTable>(json);
            try
            {
                XmlTextWriter writer = new XmlTextWriter(Path, System.Text.Encoding.UTF8);
                writer.WriteStartDocument(true);
                writer.Formatting = System.Xml.Formatting.Indented;
                writer.Indentation = 2;

                writer.WriteStartElement("m:RawBidSet");
                writer.WriteAttributeString("xmlns:m", "http://pemc/soa/RawBidSet.xsd");
                writer.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteAttributeString("xsi:schemaLocation", "http://pemc/soa/RawBidSet.xsd RawBidSet.xsd");
                //Start MessageHeader
                writer.WriteStartElement("m:MessageHeader");
                writer.WriteStartElement("m:TimeDate");
                writer.WriteString(DateTime.UtcNow.ToString("s") + "Z");
                writer.WriteEndElement();

                writer.WriteStartElement("m:Source");
                writer.WriteString("DEFAULT");
                writer.WriteEndElement();

                writer.WriteEndElement();
                //End MessageHeader

                //Start MessagePayload
                writer.WriteStartElement("m:MessagePayload");
                writer.WriteStartElement("m:GeneratingBid");

                writer.WriteStartElement("m:startTime");
                writer.WriteString(DateTime.Parse(dt.Rows[1][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                writer.WriteEndElement();

                writer.WriteStartElement("m:stopTime");
                writer.WriteString(DateTime.Parse(dt.Rows[1][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                writer.WriteEndElement();

                writer.WriteStartElement("m:RegisteredGenerator");
                writer.WriteStartElement("m:mrid");
                writer.WriteString(UnitNumber);
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("m:MarketParticipant");
                writer.WriteStartElement("m:mrid");
                writer.WriteString(Tpuser);
                writer.WriteEndElement();
                writer.WriteEndElement();
                //try
                //{

                //start ProductBid
                writer.WriteStartElement("m:ProductBid");

                writer.WriteStartElement("m:MarketProduct");
                writer.WriteStartElement("m:marketProductType");
                writer.WriteString("EN");
                writer.WriteEndElement();
                writer.WriteEndElement();

                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    //start BidSchedule
                    writer.WriteStartElement("m:BidSchedule");

                    writer.WriteStartElement("m:timeIntervalStart");
                    string timeFromStr;
                    if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                    {
                        timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                    }
                    else
                    {
                        timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                    }
                    writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                    writer.WriteEndElement();

                    string timeToStr;
                    writer.WriteStartElement("m:timeIntervalEnd");
                    if (dt.Rows[i][1].ToString() == "24")
                    {
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                    }
                    else
                    {
                        if (dt.Rows[i][1].ToString().Length == 1)
                        {
                            timeToStr = "T0" + dt.Rows[i][1].ToString();
                        }
                        else
                        {
                            timeToStr = "T" + dt.Rows[i][1].ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                    }
                    writer.WriteEndElement();

                    //control mode
                    writer.WriteStartElement("m:ControlMode");
                    writer.WriteString($"{dt.Rows[i][68].ToString()}");
                    writer.WriteEndElement();

                    //start BidPriceCurve
                    writer.WriteStartElement("m:BidPriceCurve");

                    for (int x = 2; x <= 22; x += 2)
                    {
                        if (dt.Rows[i][x].ToString() != null && dt.Rows[i][x].ToString() != "")
                        {
                            //start CurveSchedData
                            writer.WriteStartElement("m:CurveSchedData");

                            writer.WriteStartElement("m:xAxisData");
                            writer.WriteString(dt.Rows[i][x + 1].ToString());
                            writer.WriteEndElement();
                            writer.WriteStartElement("m:y1AxisData");
                            writer.WriteString(dt.Rows[i][x].ToString());
                            writer.WriteEndElement();

                            writer.WriteEndElement();
                            //End CurveSchedData
                        }
                    }

                    writer.WriteEndElement();
                    //end BidPriceCurve
                    writer.WriteEndElement();
                    //end BidSchedule
                }
                writer.WriteEndElement();
                //end ProductBid

                int ru = 0;
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][24].ToString() != null && dt.Rows[i][24].ToString() != "")
                    {
                        ru = ru + 1;
                    }
                }

                if (ru > 0)
                {
                    //start ProductBid
                    writer.WriteStartElement("m:ProductBid");

                    writer.WriteStartElement("m:MarketProduct");
                    writer.WriteStartElement("m:marketProductType");
                    writer.WriteString("RU");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                // AS Product Type
                // AS RU
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][24].ToString() != null && dt.Rows[i][24].ToString() != "")
                    {


                        //start BidSchedule
                        writer.WriteStartElement("m:BidSchedule");

                        writer.WriteStartElement("m:timeIntervalStart");
                        string timeFromStr;
                        if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                        {
                            timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        else
                        {
                            timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                        writer.WriteEndElement();

                        string timeToStr;
                        writer.WriteStartElement("m:timeIntervalEnd");
                        if (dt.Rows[i][1].ToString() == "24")
                        {
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                        }
                        else
                        {
                            if (dt.Rows[i][1].ToString().Length == 1)
                            {
                                timeToStr = "T0" + dt.Rows[i][1].ToString();
                            }
                            else
                            {
                                timeToStr = "T" + dt.Rows[i][1].ToString();
                            }
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                        }
                        writer.WriteEndElement();
                        //start BidPriceCurve
                        writer.WriteStartElement("m:BidPriceCurve");

                        for (int x = 24; x <= 32; x += 2)
                        {
                            if (dt.Rows[i][x].ToString() != null && dt.Rows[i][x].ToString() != "")
                            {
                                //start CurveSchedData
                                writer.WriteStartElement("m:CurveSchedData");

                                writer.WriteStartElement("m:xAxisData");
                                writer.WriteString(dt.Rows[i][x + 1].ToString());
                                writer.WriteEndElement();

                                writer.WriteStartElement("m:y1AxisData");
                                writer.WriteString(dt.Rows[i][x].ToString());
                                writer.WriteEndElement();

                                writer.WriteEndElement();
                                //End CurveSchedData
                            }
                        }


                        writer.WriteEndElement();
                        //end BidPriceCurve
                        writer.WriteEndElement();
                        //end BidSchedule

                    }
                }

                if (ru > 0)
                {
                    writer.WriteEndElement();
                    //end ProductBid
                }



                int rd = 0;
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][34].ToString() != null && dt.Rows[i][34].ToString() != "")
                    {
                        rd = rd + 1;
                    }
                }

                if (rd > 0)
                {
                    //start ProductBid
                    writer.WriteStartElement("m:ProductBid");

                    writer.WriteStartElement("m:MarketProduct");
                    writer.WriteStartElement("m:marketProductType");
                    writer.WriteString("RD");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                // AS Product Type
                // AS RD
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][34].ToString() != null && dt.Rows[i][34].ToString() != "")
                    {

                        //start BidSchedule
                        writer.WriteStartElement("m:BidSchedule");

                        writer.WriteStartElement("m:timeIntervalStart");
                        string timeFromStr;
                        if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                        {
                            timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        else
                        {
                            timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                        writer.WriteEndElement();

                        string timeToStr;
                        writer.WriteStartElement("m:timeIntervalEnd");
                        if (dt.Rows[i][1].ToString() == "24")
                        {
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                        }
                        else
                        {
                            if (dt.Rows[i][1].ToString().Length == 1)
                            {
                                timeToStr = "T0" + dt.Rows[i][1].ToString();
                            }
                            else
                            {
                                timeToStr = "T" + dt.Rows[i][1].ToString();
                            }
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                        }
                        writer.WriteEndElement();
                        //start BidPriceCurve
                        writer.WriteStartElement("m:BidPriceCurve");

                        for (int x = 34; x <= 42; x += 2)
                        {
                            if (dt.Rows[i][x].ToString() != null && dt.Rows[i][x].ToString() != "")
                            {
                                //start CurveSchedData
                                writer.WriteStartElement("m:CurveSchedData");

                                writer.WriteStartElement("m:xAxisData");
                                writer.WriteString(dt.Rows[i][x + 1].ToString());
                                writer.WriteEndElement();

                                writer.WriteStartElement("m:y1AxisData");
                                writer.WriteString(dt.Rows[i][x].ToString());
                                writer.WriteEndElement();

                                writer.WriteEndElement();
                                //End CurveSchedData
                            }
                        }


                        writer.WriteEndElement();
                        //end BidPriceCurve
                        writer.WriteEndElement();
                        //end BidSchedule
                    }
                }

                if (rd > 0)
                {
                    writer.WriteEndElement();
                    //end ProductBid
                }


                int fr = 0;
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][44].ToString() != null && dt.Rows[i][44].ToString() != "")
                    {
                        fr = fr + 1;
                    }
                }

                if (fr > 0)
                {
                    //start ProductBid
                    writer.WriteStartElement("m:ProductBid");

                    writer.WriteStartElement("m:MarketProduct");
                    writer.WriteStartElement("m:marketProductType");
                    writer.WriteString("FR");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }


                // AS Product Type
                // AS FR
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][44].ToString() != null && dt.Rows[i][44].ToString() != "")
                    {
                        //start BidSchedule
                        writer.WriteStartElement("m:BidSchedule");

                        writer.WriteStartElement("m:timeIntervalStart");
                        string timeFromStr;
                        if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                        {
                            timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        else
                        {
                            timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                        writer.WriteEndElement();

                        string timeToStr;
                        writer.WriteStartElement("m:timeIntervalEnd");
                        if (dt.Rows[i][1].ToString() == "24")
                        {
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                        }
                        else
                        {
                            if (dt.Rows[i][1].ToString().Length == 1)
                            {
                                timeToStr = "T0" + dt.Rows[i][1].ToString();
                            }
                            else
                            {
                                timeToStr = "T" + dt.Rows[i][1].ToString();
                            }
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                        }
                        writer.WriteEndElement();
                        //start BidPriceCurve
                        writer.WriteStartElement("m:BidPriceCurve");

                        for (int x = 44; x <= 52; x += 2)
                        {
                            if (dt.Rows[i][x].ToString() != null && dt.Rows[i][x].ToString() != "")
                            {
                                //start CurveSchedData
                                writer.WriteStartElement("m:CurveSchedData");

                                writer.WriteStartElement("m:xAxisData");
                                writer.WriteString(dt.Rows[i][x + 1].ToString());
                                writer.WriteEndElement();

                                writer.WriteStartElement("m:y1AxisData");
                                writer.WriteString(dt.Rows[i][x].ToString());
                                writer.WriteEndElement();

                                writer.WriteEndElement();
                                //End CurveSchedData
                            }
                        }


                        writer.WriteEndElement();
                        //end BidPriceCurve
                        writer.WriteEndElement();
                        //end BidSchedule

                    }
                }

                if (fr > 0)
                {
                    writer.WriteEndElement();
                    //end ProductBid
                }


                int dr = 0;
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][54].ToString() != null && dt.Rows[i][54].ToString() != "")
                    {
                        dr = dr + 1;
                    }
                }

                if (dr > 0)
                {
                    //start ProductBid
                    writer.WriteStartElement("m:ProductBid");

                    writer.WriteStartElement("m:MarketProduct");
                    writer.WriteStartElement("m:marketProductType");
                    writer.WriteString("DR");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }


                // AS Product Type
                // AS DR
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][54].ToString() != null && dt.Rows[i][54].ToString() != "")
                    {
                        //start BidSchedule
                        writer.WriteStartElement("m:BidSchedule");

                        writer.WriteStartElement("m:timeIntervalStart");
                        string timeFromStr;
                        if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                        {
                            timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        else
                        {
                            timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                        writer.WriteEndElement();

                        string timeToStr;
                        writer.WriteStartElement("m:timeIntervalEnd");
                        if (dt.Rows[i][1].ToString() == "24")
                        {
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                        }
                        else
                        {
                            if (dt.Rows[i][1].ToString().Length == 1)
                            {
                                timeToStr = "T0" + dt.Rows[i][1].ToString();
                            }
                            else
                            {
                                timeToStr = "T" + dt.Rows[i][1].ToString();
                            }
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                        }
                        writer.WriteEndElement();
                        //start BidPriceCurve
                        writer.WriteStartElement("m:BidPriceCurve");

                        for (int x = 54; x <= 62; x += 2)
                        {
                            if (dt.Rows[i][x].ToString() != null && dt.Rows[i][x].ToString() != "")
                            {
                                //start CurveSchedData
                                writer.WriteStartElement("m:CurveSchedData");

                                writer.WriteStartElement("m:xAxisData");
                                writer.WriteString(dt.Rows[i][x + 1].ToString());
                                writer.WriteEndElement();

                                writer.WriteStartElement("m:y1AxisData");
                                writer.WriteString(dt.Rows[i][x].ToString());
                                writer.WriteEndElement();

                                writer.WriteEndElement();
                                //End CurveSchedData
                            }
                        }


                        writer.WriteEndElement();
                        //end BidPriceCurve
                        writer.WriteEndElement();
                        //end BidSchedule

                    }
                }
                if (dr > 0)
                {
                    writer.WriteEndElement();
                    //end ProductBid
                }



                //start RampRateCurve
                writer.WriteStartElement("m:RampRateCurve");
                writer.WriteStartElement("m:description");
                writer.WriteString("RAMP_RATE");
                writer.WriteEndElement();

                if ((dt.Rows[0][65].ToString() != null && dt.Rows[0][65].ToString() != "") && (dt.Rows[0][66].ToString() != null && dt.Rows[0][66].ToString() != "") && (dt.Rows[0][67].ToString() != null && dt.Rows[0][67].ToString() != ""))
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[0][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[0][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[0][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                if ((dt.Rows[1][65].ToString() != null && dt.Rows[1][65].ToString() != "") && (dt.Rows[1][66].ToString() != null && dt.Rows[1][66].ToString() != "") && (dt.Rows[1][67].ToString() != null && dt.Rows[1][67].ToString() != ""))
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[1][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[1][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[1][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                if ((dt.Rows[2][65].ToString() != null && dt.Rows[2][65].ToString() != "") && (dt.Rows[2][66].ToString() != null && dt.Rows[2][66].ToString() != "") && (dt.Rows[2][67].ToString() != null && dt.Rows[2][67].ToString() != ""))
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[2][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[2][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[2][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                if ((dt.Rows[3][65].ToString() != null && dt.Rows[3][65].ToString() != "") && (dt.Rows[3][66].ToString() != null && dt.Rows[3][66].ToString() != "") && (dt.Rows[3][67].ToString() != null && dt.Rows[3][67].ToString() != ""))
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[3][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[3][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[3][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                if ((dt.Rows[4][65].ToString() != null && dt.Rows[4][65].ToString() != "") && (dt.Rows[4][66].ToString() != null && dt.Rows[4][66].ToString() != "") && (dt.Rows[4][67].ToString() != null && dt.Rows[4][67].ToString() != ""))
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[4][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[4][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[4][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                //end RampRateCurve


                writer.WriteEndElement();
                writer.WriteEndElement();
                //End MessagePayload
                writer.WriteEndElement();
                writer.WriteEndDocument();

                writer.Close();

                message = "xml successfully created";
            }
            catch (Exception e)
            {
                message = e.Message.ToString();
            }

            return message;
        }
        public string CreateOfferBatteryXML(List<BidDataTable> bid, string Tpuser, string Path, string UnitNumber, string ControlMode)
        {
            string message = "";
            DataTable dt = new DataTable();
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(bid);
            dt = JsonConvert.DeserializeObject<DataTable>(json);
            try
            {
                XmlTextWriter writer = new XmlTextWriter(Path, System.Text.Encoding.UTF8);
                writer.WriteStartDocument(true);
                writer.Formatting = System.Xml.Formatting.Indented;
                writer.Indentation = 2;

                writer.WriteStartElement("m:RawBidSet");
                writer.WriteAttributeString("xmlns:m", "http://pemc/soa/RawBidSet.xsd");
                writer.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteAttributeString("xsi:schemaLocation", "http://pemc/soa/RawBidSet.xsd RawBidSet.xsd");
                //Start MessageHeader
                writer.WriteStartElement("m:MessageHeader");
                writer.WriteStartElement("m:TimeDate");
                writer.WriteString(DateTime.UtcNow.ToString("s") + "Z");
                writer.WriteEndElement();

                writer.WriteStartElement("m:Source");
                writer.WriteString("Default");
                writer.WriteEndElement();

                writer.WriteEndElement();
                //End MessageHeader

                //Start MessagePayload
                writer.WriteStartElement("m:MessagePayload");
                writer.WriteStartElement("m:GeneratingBid");

                //writer.WriteStartElement("m:description");
                //writer.WriteString("Generation Offer");
                //writer.WriteEndElement();

                //writer.WriteStartElement("m:name");
                //writer.WriteString(UnitNumber);
                //writer.WriteEndElement();

                writer.WriteStartElement("m:startTime");
                writer.WriteString(DateTime.Parse(dt.Rows[1][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                writer.WriteEndElement();

                writer.WriteStartElement("m:stopTime");
                writer.WriteString(DateTime.Parse(dt.Rows[1][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                writer.WriteEndElement();

                writer.WriteStartElement("m:RegisteredGenerator");
                writer.WriteStartElement("m:mrid");
                writer.WriteString(UnitNumber);
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("m:MarketParticipant");
                writer.WriteStartElement("m:mrid");
                writer.WriteString(Tpuser);
                writer.WriteEndElement();
                writer.WriteEndElement();
                //try
                //{

                //start ProductBid
                writer.WriteStartElement("m:ProductBid");

                writer.WriteStartElement("m:MarketProduct");
                writer.WriteStartElement("m:marketProductType");
                writer.WriteString("EN");
                writer.WriteEndElement();
                writer.WriteEndElement();

                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    //start BidSchedule
                    writer.WriteStartElement("m:BidSchedule");

                    writer.WriteStartElement("m:timeIntervalStart");
                    string timeFromStr;
                    if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                    {
                        timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                    }
                    else
                    {
                        timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                    }
                    writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                    writer.WriteEndElement();

                    string timeToStr;
                    writer.WriteStartElement("m:timeIntervalEnd");
                    if (dt.Rows[i][1].ToString() == "24")
                    {
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                    }
                    else
                    {
                        if (dt.Rows[i][1].ToString().Length == 1)
                        {
                            timeToStr = "T0" + dt.Rows[i][1].ToString();
                        }
                        else
                        {
                            timeToStr = "T" + dt.Rows[i][1].ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                    }
                    writer.WriteEndElement();

                    //control mode
                    writer.WriteStartElement("m:ControlMode");
                    writer.WriteString($"{dt.Rows[i][68].ToString()}");
                    writer.WriteEndElement();

                    //start BatteryPSPriceCurve
                    writer.WriteStartElement("m:BatteryPSPriceCurve");

                    for (int x = 2; x <= 22; x += 2)
                    {
                        if (dt.Rows[i][x].ToString() != null && dt.Rows[i][x].ToString() != "")
                        {
                            //start CurveSchedData
                            writer.WriteStartElement("m:CurveSchedData");

                            writer.WriteStartElement("m:xAxisData");
                            writer.WriteString(dt.Rows[i][x + 1].ToString());
                            writer.WriteEndElement();
                            writer.WriteStartElement("m:y1AxisData");
                            writer.WriteString(dt.Rows[i][x].ToString());
                            writer.WriteEndElement();

                            writer.WriteEndElement();
                            //End CurveSchedData
                        }
                    }

                    writer.WriteEndElement();
                    //end BatteryPSPriceCurve
                    writer.WriteEndElement();
                    //end BidSchedule
                }
                writer.WriteEndElement();
                //end ProductBid

                int ru = 0;
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][24].ToString() != null && dt.Rows[i][24].ToString() != "")
                    {
                        ru = ru + 1;
                    }
                }

                if (ru > 0)
                {
                    //start ProductBid
                    writer.WriteStartElement("m:ProductBid");

                    writer.WriteStartElement("m:MarketProduct");
                    writer.WriteStartElement("m:marketProductType");
                    writer.WriteString("RU");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                // AS Product Type
                // AS RU
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][24].ToString() != null && dt.Rows[i][24].ToString() != "")
                    {


                        //start BidSchedule
                        writer.WriteStartElement("m:BidSchedule");

                        writer.WriteStartElement("m:timeIntervalStart");
                        string timeFromStr;
                        if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                        {
                            timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        else
                        {
                            timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                        writer.WriteEndElement();

                        string timeToStr;
                        writer.WriteStartElement("m:timeIntervalEnd");
                        if (dt.Rows[i][1].ToString() == "24")
                        {
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                        }
                        else
                        {
                            if (dt.Rows[i][1].ToString().Length == 1)
                            {
                                timeToStr = "T0" + dt.Rows[i][1].ToString();
                            }
                            else
                            {
                                timeToStr = "T" + dt.Rows[i][1].ToString();
                            }
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                        }
                        writer.WriteEndElement();
                        //start BidPriceCurve
                        writer.WriteStartElement("m:BidPriceCurve");

                        for (int x = 24; x <= 32; x += 2)
                        {
                            if (dt.Rows[i][x].ToString() != null && dt.Rows[i][x].ToString() != "")
                            {
                                //start CurveSchedData
                                writer.WriteStartElement("m:CurveSchedData");

                                writer.WriteStartElement("m:xAxisData");
                                writer.WriteString(dt.Rows[i][x + 1].ToString());
                                writer.WriteEndElement();

                                writer.WriteStartElement("m:y1AxisData");
                                writer.WriteString(dt.Rows[i][x].ToString());
                                writer.WriteEndElement();

                                writer.WriteEndElement();
                                //End CurveSchedData
                            }
                        }


                        writer.WriteEndElement();
                        //end BidPriceCurve
                        writer.WriteEndElement();
                        //end BidSchedule

                    }
                }

                if (ru > 0)
                {
                    writer.WriteEndElement();
                    //end ProductBid
                }



                int rd = 0;
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][34].ToString() != null && dt.Rows[i][34].ToString() != "")
                    {
                        rd = rd + 1;
                    }
                }

                if (rd > 0)
                {
                    //start ProductBid
                    writer.WriteStartElement("m:ProductBid");

                    writer.WriteStartElement("m:MarketProduct");
                    writer.WriteStartElement("m:marketProductType");
                    writer.WriteString("RD");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                // AS Product Type
                // AS RD
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][34].ToString() != null && dt.Rows[i][34].ToString() != "")
                    {

                        //start BidSchedule
                        writer.WriteStartElement("m:BidSchedule");

                        writer.WriteStartElement("m:timeIntervalStart");
                        string timeFromStr;
                        if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                        {
                            timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        else
                        {
                            timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                        writer.WriteEndElement();

                        string timeToStr;
                        writer.WriteStartElement("m:timeIntervalEnd");
                        if (dt.Rows[i][1].ToString() == "24")
                        {
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                        }
                        else
                        {
                            if (dt.Rows[i][1].ToString().Length == 1)
                            {
                                timeToStr = "T0" + dt.Rows[i][1].ToString();
                            }
                            else
                            {
                                timeToStr = "T" + dt.Rows[i][1].ToString();
                            }
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                        }
                        writer.WriteEndElement();

                        //start BidPriceCurve
                        writer.WriteStartElement("m:BidPriceCurve");

                        for (int x = 34; x <= 42; x += 2)
                        {
                            if (dt.Rows[i][x].ToString() != null && dt.Rows[i][x].ToString() != "")
                            {
                                //start CurveSchedData
                                writer.WriteStartElement("m:CurveSchedData");

                                writer.WriteStartElement("m:xAxisData");
                                writer.WriteString(dt.Rows[i][x + 1].ToString());
                                writer.WriteEndElement();

                                writer.WriteStartElement("m:y1AxisData");
                                writer.WriteString(dt.Rows[i][x].ToString());
                                writer.WriteEndElement();

                                writer.WriteEndElement();
                                //End CurveSchedData
                            }
                        }


                        writer.WriteEndElement();
                        //end BidPriceCurve
                        writer.WriteEndElement();
                        //end BidSchedule
                    }
                }

                if (rd > 0)
                {
                    writer.WriteEndElement();
                    //end ProductBid
                }


                int fr = 0;
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][44].ToString() != null && dt.Rows[i][44].ToString() != "")
                    {
                        fr = fr + 1;
                    }
                }

                if (fr > 0)
                {
                    //start ProductBid
                    writer.WriteStartElement("m:ProductBid");

                    writer.WriteStartElement("m:MarketProduct");
                    writer.WriteStartElement("m:marketProductType");
                    writer.WriteString("FR");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }


                // AS Product Type
                // AS FR
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][44].ToString() != null && dt.Rows[i][44].ToString() != "")
                    {
                        //start BidSchedule
                        writer.WriteStartElement("m:BidSchedule");

                        writer.WriteStartElement("m:timeIntervalStart");
                        string timeFromStr;
                        if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                        {
                            timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        else
                        {
                            timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                        writer.WriteEndElement();

                        string timeToStr;
                        writer.WriteStartElement("m:timeIntervalEnd");
                        if (dt.Rows[i][1].ToString() == "24")
                        {
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                        }
                        else
                        {
                            if (dt.Rows[i][1].ToString().Length == 1)
                            {
                                timeToStr = "T0" + dt.Rows[i][1].ToString();
                            }
                            else
                            {
                                timeToStr = "T" + dt.Rows[i][1].ToString();
                            }
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                        }
                        writer.WriteEndElement();
                        //start BidPriceCurve
                        writer.WriteStartElement("m:BidPriceCurve");

                        for (int x = 44; x <= 52; x += 2)
                        {
                            if (dt.Rows[i][x].ToString() != null && dt.Rows[i][x].ToString() != "")
                            {
                                //start CurveSchedData
                                writer.WriteStartElement("m:CurveSchedData");

                                writer.WriteStartElement("m:xAxisData");
                                writer.WriteString(dt.Rows[i][x + 1].ToString());
                                writer.WriteEndElement();

                                writer.WriteStartElement("m:y1AxisData");
                                writer.WriteString(dt.Rows[i][x].ToString());
                                writer.WriteEndElement();

                                writer.WriteEndElement();
                                //End CurveSchedData
                            }
                        }


                        writer.WriteEndElement();
                        //end BidPriceCurve
                        writer.WriteEndElement();
                        //end BidSchedule

                    }
                }

                if (fr > 0)
                {
                    writer.WriteEndElement();
                    //end ProductBid
                }


                int dr = 0;
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][54].ToString() != null && dt.Rows[i][54].ToString() != "")
                    {
                        dr = dr + 1;
                    }
                }

                if (dr > 0)
                {
                    //start ProductBid
                    writer.WriteStartElement("m:ProductBid");

                    writer.WriteStartElement("m:MarketProduct");
                    writer.WriteStartElement("m:marketProductType");
                    writer.WriteString("DR");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }


                // AS Product Type
                // AS DR
                for (int i = 0; i <= dt.Rows.Count - 1; i++)
                {
                    if (dt.Rows[i][54].ToString() != null && dt.Rows[i][54].ToString() != "")
                    {
                        //start BidSchedule
                        writer.WriteStartElement("m:BidSchedule");

                        writer.WriteStartElement("m:timeIntervalStart");
                        string timeFromStr;
                        if ((Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString().Length == 1)
                        {
                            timeFromStr = "T0" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        else
                        {
                            timeFromStr = "T" + (Convert.ToInt32(dt.Rows[i][1].ToString()) - 1).ToString();
                        }
                        writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeFromStr + ":00:00.000+08:00");
                        writer.WriteEndElement();

                        string timeToStr;
                        writer.WriteStartElement("m:timeIntervalEnd");
                        if (dt.Rows[i][1].ToString() == "24")
                        {
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).AddDays(1).ToString("yyyy-MM-dd") + "T00:00:00.000+08:00");
                        }
                        else
                        {
                            if (dt.Rows[i][1].ToString().Length == 1)
                            {
                                timeToStr = "T0" + dt.Rows[i][1].ToString();
                            }
                            else
                            {
                                timeToStr = "T" + dt.Rows[i][1].ToString();
                            }
                            writer.WriteString(DateTime.Parse(dt.Rows[i][0].ToString().Replace(" 12:00:00 AMT", "")).ToString("yyyy-MM-dd") + timeToStr + ":00:00.000+08:00");
                        }
                        writer.WriteEndElement();
                        //start BidPriceCurve
                        writer.WriteStartElement("m:BidPriceCurve");

                        for (int x = 54; x <= 62; x += 2)
                        {
                            if (dt.Rows[i][x].ToString() != null && dt.Rows[i][x].ToString() != "")
                            {
                                //start CurveSchedData
                                writer.WriteStartElement("m:CurveSchedData");

                                writer.WriteStartElement("m:xAxisData");
                                writer.WriteString(dt.Rows[i][x + 1].ToString());
                                writer.WriteEndElement();

                                writer.WriteStartElement("m:y1AxisData");
                                writer.WriteString(dt.Rows[i][x].ToString());
                                writer.WriteEndElement();

                                writer.WriteEndElement();
                                //End CurveSchedData
                            }
                        }


                        writer.WriteEndElement();
                        //end BidPriceCurve
                        writer.WriteEndElement();
                        //end BidSchedule

                    }
                }
                if (dr > 0)
                {
                    writer.WriteEndElement();
                    //end ProductBid
                }



                //start RampRateCurve
                writer.WriteStartElement("m:RampRateCurve");
                writer.WriteStartElement("m:description");
                writer.WriteString("RAMP_RATE");
                writer.WriteEndElement();

                if ((dt.Rows[0][65].ToString() != null && dt.Rows[0][65].ToString() != "") && (dt.Rows[0][66].ToString() != null && dt.Rows[0][66].ToString() != "") && (dt.Rows[0][67].ToString() != null && dt.Rows[0][67].ToString() != ""))
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[0][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[0][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[0][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                if ((dt.Rows[1][65].ToString() != null && dt.Rows[1][65].ToString() != "") && (dt.Rows[1][66].ToString() != null && dt.Rows[1][66].ToString() != "") && (dt.Rows[1][67].ToString() != null && dt.Rows[1][67].ToString() != ""))
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[1][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[1][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[1][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                if ((dt.Rows[2][65].ToString() != null && dt.Rows[2][65].ToString() != "") && (dt.Rows[2][66].ToString() != null && dt.Rows[2][66].ToString() != "") && (dt.Rows[2][67].ToString() != null && dt.Rows[2][67].ToString() != ""))
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[2][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[2][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[2][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                if ((dt.Rows[3][65].ToString() != null && dt.Rows[3][65].ToString() != "") && (dt.Rows[3][66].ToString() != null && dt.Rows[3][66].ToString() != "") && (dt.Rows[3][67].ToString() != null && dt.Rows[3][67].ToString() != ""))
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[3][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[3][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[3][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                if ((dt.Rows[4][65].ToString() != null && dt.Rows[4][65].ToString() != "") && (dt.Rows[4][66].ToString() != null && dt.Rows[4][66].ToString() != "") && (dt.Rows[4][67].ToString() != null && dt.Rows[4][67].ToString() != ""))
                {
                    writer.WriteStartElement("m:CurveData");
                    writer.WriteStartElement("m:xAxisData");
                    writer.WriteString(dt.Rows[4][65].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y1AxisData");
                    writer.WriteString(dt.Rows[4][66].ToString());
                    writer.WriteEndElement();
                    writer.WriteStartElement("m:y2AxisData");
                    writer.WriteString(dt.Rows[4][67].ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                //end RampRateCurve


                writer.WriteEndElement();
                writer.WriteEndElement();
                //End MessagePayload
                writer.WriteEndElement();
                writer.WriteEndDocument();

                writer.Close();

                message = "xml successfully created";
            }
            catch (Exception e)
            {
                message = e.Message.ToString();
            }

            return message;
        }
        public DateTime GetCellDateValue(IRow row, int rowCount, int colCount)
        {
            DateTime value;
            if (row.GetCell(colCount).CellType == CellType.String)
            {
                value = Convert.ToDateTime(row.GetCell(colCount).StringCellValue);
            }
            else
            {
                value = row.GetCell(colCount).DateCellValue;
            }

            return value;
        }

        public Nullable<double> GetCellValue(IRow row, int rowCount, int colCount)
        {
            string value = "";
            Nullable<double> dvalue = null;
            if (row.GetCell(colCount).CellType == CellType.String)
            {
                value = row.GetCell(colCount).StringCellValue;
            }
            else if (row.GetCell(colCount).CellType == CellType.Numeric)
            {
                value = Convert.ToString(row.GetCell(colCount).NumericCellValue);
            }

            //value = value == "" || value == null ? null : value;
            if(value == "" || value == null)
            {
                dvalue = null;
            }
            else
            {
                dvalue = Convert.ToDouble(value);
            }

            return dvalue;
        }

        public string GetCellStringValue(IRow row, int rowCount, int colCount)
        {
            string value = "";

            if (row.GetCell(colCount).CellType == CellType.String)
            {
                value = row.GetCell(colCount).StringCellValue;
            }

            return value;
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetHAP(int SiteID, int UnitID)
        {
            List<TagValue> tagValue = new List<TagValue>();
            Unit unit = new Unit();
            string dateTime;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            Nullable<Double> rtd, price, actual;

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, tagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                dateTime = DateTime.Now.ToShortDateString() + " " +  DateTime.Now.ToString("HH");
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                PIPoint ptVolume = PIPoint.FindPIPoint(piServer, unit.HAPTagName);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.HAPPriceTagName);
                PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);

                if (ptVolume != null && ptPrice != null)
                {
                    for (int i = 0; i <= 55; i = i + 5)
                    {
                        string min;
                        if (i < 10)
                        {
                            min = "0".ToString() + i.ToString();
                        }
                        else
                        {
                            min = i.ToString();
                        }

                        var ptVolumeValue = ptVolume.RecordedValue(new AFTime(dateTime + ":" + min + ":00"), AFRetrievalMode.Exact);
                        var ptPriceValue = ptPrice.RecordedValue(new AFTime(dateTime + ":" + min + ":00"), AFRetrievalMode.Exact);
                        var ptActualValue = ptActual.RecordedValue(new AFTime(dateTime + ":" + min + ":00"), AFRetrievalMode.Exact);

                        if (ptVolumeValue.Value.ToString() == "No Data" || ptVolumeValue.Value.ToString() == "Pt Created")
                        {
                            rtd = null;
                        }
                        else
                        {
                            rtd = Convert.ToDouble(ptVolumeValue.Value);
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

                        tagValue.Add(new TagValue
                        {
                            RTDValue = rtd
                                ,PriceValue = price
                                ,ActualValue = actual
                                ,TimeStamp = Convert.ToDateTime(dateTime + ":" + min + ":00")
                                ,DateTIme = min
                        });

                    }

                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, tagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetHAPExtended(int SiteID, int UnitID)
        {
            List<TagValue> tagValue = new List<TagValue>();
            Unit unit = new Unit();
            string dateTime;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            Nullable<Double> rtd, price, actual;

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, tagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {

                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                PIPoint ptVolume = PIPoint.FindPIPoint(piServer, unit.HAPTagName);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.HAPPriceTagName);
                PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);

                if (ptVolume != null && ptPrice != null)
                {
                    int curr_min = Int32.Parse(DateTime.Now.ToString("mm"));
                    int starting_min = 0;
                    if ((curr_min % 10) <= 5)
                    {
                        starting_min = curr_min + (5 - (curr_min % 10));
                    }
                    else
                    {
                        starting_min = curr_min + (10 - (curr_min % 10));
                    }
                    int next_hour_over = 60 - (60 - starting_min);

                    for (int i = starting_min; i <= 60; i = i + 5)
                    {
                        string min;
                        dateTime = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH");
                        if (i < 10)
                        {
                            min = "0".ToString() + i.ToString();
                        }
                        else if (i == 60)
                        {
                            min = "00";
                            dateTime = DateTime.Now.AddHours(1).ToShortDateString() + " " + DateTime.Now.AddHours(1).ToString("HH");
                        }
                        else
                        {
                            min = i.ToString();
                        }

                        var ptVolumeValue = ptVolume.RecordedValue(new AFTime(dateTime + ":" + min + ":00"), AFRetrievalMode.Exact);
                        var ptPriceValue = ptPrice.RecordedValue(new AFTime(dateTime + ":" + min + ":00"), AFRetrievalMode.Exact);
                        var ptActualValue = ptActual.RecordedValue(new AFTime(dateTime + ":" + min + ":00"), AFRetrievalMode.Exact);

                        if (ptVolumeValue.Value.ToString() == "No Data" || ptVolumeValue.Value.ToString() == "Pt Created")
                        {
                            rtd = null;
                        }
                        else
                        {
                            rtd = Convert.ToDouble(ptVolumeValue.Value);
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

                        tagValue.Add(new TagValue
                        {
                            RTDValue = rtd
                                ,
                            PriceValue = price
                                ,
                            ActualValue = actual
                                ,
                            TimeStamp = Convert.ToDateTime(dateTime + ":" + min + ":00")
                                ,
                            DateTIme = i == 60 ? "60" : min
                        });

                    }
                    for (int i = 5; i < next_hour_over; i = i + 5)
                    {
                        string min;
                        dateTime = DateTime.Now.ToShortDateString() + " " + DateTime.Now.AddHours(1).ToString("HH");
                        if (i < 10)
                        {
                            min = "0".ToString() + i.ToString();
                        }
                        else if (i == 60)
                        {
                            min = "00";
                            dateTime = DateTime.Now.AddHours(1).ToShortDateString() + " " + DateTime.Now.AddHours(2).ToString("HH");
                        }
                        else
                        {
                            min = i.ToString();
                        }

                        var ptVolumeValue = ptVolume.RecordedValue(new AFTime(dateTime + ":" + min + ":00"), AFRetrievalMode.Exact);
                        var ptPriceValue = ptPrice.RecordedValue(new AFTime(dateTime + ":" + min + ":00"), AFRetrievalMode.Exact);
                        var ptActualValue = ptActual.RecordedValue(new AFTime(dateTime + ":" + min + ":00"), AFRetrievalMode.Exact);

                        if (ptVolumeValue.Value.ToString() == "No Data" || ptVolumeValue.Value.ToString() == "Pt Created")
                        {
                            rtd = null;
                        }
                        else
                        {
                            rtd = Convert.ToDouble(ptVolumeValue.Value);
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

                        tagValue.Add(new TagValue
                        {
                            RTDValue = rtd
                                ,
                            PriceValue = price
                                ,
                            ActualValue = actual
                                ,
                            TimeStamp = Convert.ToDateTime(dateTime + ":" + min + ":00")
                                ,
                            DateTIme = i == 60 ? "60" : min
                        });

                    }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, tagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetDAP(int SiteID, int UnitID, Boolean IsCurrent)
        {
            List<TagValue> tagValue = new List<TagValue>();
            Unit unit = new Unit();
            string dateTime;
            string DapHr = "";
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            Nullable<Double> rtd, price, actual;

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, tagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                if (IsCurrent == true)
                {
                    dateTime = DateTime.Now.ToShortDateString();
                }
                else
                {
                    dateTime = DateTime.Now.AddDays(1).ToShortDateString();
                }

                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                string DAPRTD = unit.DAPTagName;
                string DAPPrice = unit.DAPPriceTagName;
                int checkHour = DateTime.Now.Hour;
                DateTime currentDate = DateTime.Now;
                DateTime checkDate = Convert.ToDateTime(DateTime.Now.ToShortDateString() + " 00:00:00");
                
                if (currentDate <= checkDate.AddMinutes(21))
                {
                    DapHr = "20H";
                }
                else if(currentDate >= checkDate.AddMinutes(21)
                    && currentDate < checkDate.AddHours(4).AddMinutes(21))
                {
                    DapHr = "00H";
                }
                else if (currentDate >= checkDate.AddHours(4).AddMinutes(21)
                    && currentDate < checkDate.AddHours(8).AddMinutes(21))
                {
                    DapHr = "04H";
                }
                else if (currentDate >= checkDate.AddHours(8).AddMinutes(21)
                    && currentDate < checkDate.AddHours(12).AddMinutes(21))
                {
                    DapHr = "08H";
                }
                else if (currentDate >= checkDate.AddHours(12).AddMinutes(21)
                    && currentDate < checkDate.AddHours(16).AddMinutes(21))
                {
                    DapHr = "12H";
                }
                else if (currentDate >= checkDate.AddHours(16).AddMinutes(21)
                    && currentDate < checkDate.AddHours(20).AddMinutes(21))
                {
                    DapHr = "16H";
                }
                else if (currentDate >= checkDate.AddHours(20).AddMinutes(21))
                {
                    DapHr = "20H";
                }

                PIPoint ptVolume = PIPoint.FindPIPoint(piServer, DAPRTD);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, DAPPrice);
                PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);

                if (ptVolume != null && ptPrice != null)
                {
                    var ptVolumeValue = ptVolume.RecordedValue(new AFTime(dateTime + " 00:00:00"), AFRetrievalMode.Exact);
                    var ptPriceValue = ptPrice.RecordedValue(new AFTime(dateTime + " 00:00:00"), AFRetrievalMode.Exact);
                    var ptActualValue = ptActual.RecordedValue(new AFTime(dateTime + " 00:00:00"), AFRetrievalMode.Exact);

                    if (ptVolumeValue.Value.ToString() == "No Data" || ptVolumeValue.Value.ToString() == "Pt Created")
                    {
                        rtd = null;
                    }
                    else
                    {
                        rtd = Convert.ToDouble(ptVolumeValue.Value);
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

                    tagValue.Add(new TagValue
                    {
                        RTDValue = rtd
                            ,
                        PriceValue = price
                            //,ActualValue = actual
                            ,
                        TimeStamp = Convert.ToDateTime(dateTime + " 00:00:00")
                            ,
                        DateTIme = dateTime + " 00:00:00"
                    });



                    for (int i = 1; i <= 23; i++)
                    {
                        string hr;
                        if (i.ToString().Length == 1)
                        {
                            hr = "0" + i.ToString();
                        }
                        else
                        {
                            hr = i.ToString();
                        }

                        ptVolumeValue = ptVolume.RecordedValue(new AFTime(dateTime + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                        ptPriceValue = ptPrice.RecordedValue(new AFTime(dateTime + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                        ptActualValue = ptActual.RecordedValue(new AFTime(dateTime + " " + hr + ":00:00"), AFRetrievalMode.Exact);

                        if (ptVolumeValue.Value.ToString() == "No Data" || ptVolumeValue.Value.ToString() == "Pt Created")
                        {
                            rtd = null;
                        }
                        else
                        {
                            rtd = Convert.ToDouble(ptVolumeValue.Value);
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

                        tagValue.Add(new TagValue
                        {
                            RTDValue = rtd
                                ,
                            PriceValue = price
                                //,ActualValue = actual
                                ,
                            TimeStamp = Convert.ToDateTime(dateTime + " " + hr + ":00:00")
                                ,
                            DateTIme = dateTime + " " + hr + ":00:00"
                            ,DAPHour = DapHr
                        });

                    }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, tagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetDAPExtended(int SiteID, int UnitID, Boolean IsCurrent)
        {
            List<TagValue> tagValue = new List<TagValue>();
            Unit unit = new Unit();
            string DapHr = "";
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            Nullable<Double> rtd, price, actual;

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, tagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                string DAPRTD = unit.DAPTagName;
                string DAPPrice = unit.DAPPriceTagName;
                int checkHour = DateTime.Now.Hour;
                DateTime currentDate = DateTime.Now;
               
                PIPoint ptVolume = PIPoint.FindPIPoint(piServer, DAPRTD);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, DAPPrice);

                if (ptVolume != null && ptPrice != null)
                {
                    if(currentDate.Hour >= 13)
                    {
                        for (int i = 13; i <= 23; i++)
                        {
                            DateTime date = DateTime.Now;
                            string hr;
                            if (i.ToString().Length == 1)
                            {
                                hr = "0" + i.ToString();
                            }
                            else
                            {
                                hr = i.ToString();
                            }

                            var ptVolumeValue = ptVolume.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptPriceValue = ptPrice.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            
                            if (ptVolumeValue.Value.ToString() == "No Data" || ptVolumeValue.Value.ToString() == "Pt Created")
                            {
                                rtd = null;
                            }
                            else
                            {
                                rtd = Convert.ToDouble(ptVolumeValue.Value);
                            }

                            if (ptPriceValue.Value.ToString() == "No Data" || ptPriceValue.Value.ToString() == "Pt Created")
                            {
                                price = null;
                            }
                            else
                            {
                                price = Convert.ToDouble(ptPriceValue.Value);
                            }

                            tagValue.Add(new TagValue
                            {
                                RTDValue = rtd,
                                PriceValue = price,
                                TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                                DateTIme = date.ToShortDateString() + " " + hr + ":00:00",
                                DAPHour = Convert.ToString(i)
                            });

                        }

                        for (int i = 0; i <= 12; i++)
                        {
                            DateTime date = DateTime.Now.AddDays(1);
                            string hr;
                            if (i.ToString().Length == 1)
                            {
                                hr = "0" + i.ToString();
                            }
                            else
                            {
                                hr = i.ToString();
                            }

                            var ptVolumeValue = ptVolume.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptPriceValue = ptPrice.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            
                            if (ptVolumeValue.Value.ToString() == "No Data" || ptVolumeValue.Value.ToString() == "Pt Created")
                            {
                                rtd = null;
                            }
                            else
                            {
                                rtd = Convert.ToDouble(ptVolumeValue.Value);
                            }

                            if (ptPriceValue.Value.ToString() == "No Data" || ptPriceValue.Value.ToString() == "Pt Created")
                            {
                                price = null;
                            }
                            else
                            {
                                price = Convert.ToDouble(ptPriceValue.Value);
                            }

                            tagValue.Add(new TagValue
                            {
                                RTDValue = rtd,
                                PriceValue = price,
                                TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                                DateTIme = date.ToShortDateString() + " " + hr + ":00:00",
                                DAPHour = Convert.ToString(i) == "0" ? "24" : Convert.ToString(i)
                            });

                        }
                    }
                    else if(currentDate.Hour >= 1 && currentDate.Hour < 13)
                    {
                        DateTime date = DateTime.Now;
                        for (int i = 1; i <= 23; i++)
                        {
                            string hr;
                            if (i.ToString().Length == 1)
                            {
                                hr = "0" + i.ToString();
                            }
                            else
                            {
                                hr = i.ToString();
                            }

                            var ptVolumeValue = ptVolume.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptPriceValue = ptPrice.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            
                            if (ptVolumeValue.Value.ToString() == "No Data" || ptVolumeValue.Value.ToString() == "Pt Created")
                            {
                                rtd = null;
                            }
                            else
                            {
                                rtd = Convert.ToDouble(ptVolumeValue.Value);
                            }

                            if (ptPriceValue.Value.ToString() == "No Data" || ptPriceValue.Value.ToString() == "Pt Created")
                            {
                                price = null;
                            }
                            else
                            {
                                price = Convert.ToDouble(ptPriceValue.Value);
                            }

                            tagValue.Add(new TagValue
                            {
                                RTDValue = rtd
                                ,PriceValue = price
                                ,TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00")
                                ,DateTIme = date.ToShortDateString() + " " + hr + ":00:00"
                                ,DAPHour = Convert.ToString(i)
                            });

                        }

                        var ptVolumeVal = ptVolume.RecordedValue(new AFTime(date.AddDays(1).ToShortDateString() + " 00:00:00"), AFRetrievalMode.Exact);
                        var ptPriceVal = ptPrice.RecordedValue(new AFTime(date.AddDays(1).ToShortDateString() + " 00:00:00"), AFRetrievalMode.Exact);

                        if (ptVolumeVal.Value.ToString() == "No Data" || ptVolumeVal.Value.ToString() == "Pt Created")
                        {
                            rtd = null;
                        }
                        else
                        {
                            rtd = Convert.ToDouble(ptVolumeVal.Value);
                        }

                        if (ptPriceVal.Value.ToString() == "No Data" || ptPriceVal.Value.ToString() == "Pt Created")
                        {
                            price = null;
                        }
                        else
                        {
                            price = Convert.ToDouble(ptPriceVal.Value);
                        }


                        tagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = price,
                            TimeStamp = Convert.ToDateTime(date.AddDays(1).ToShortDateString() + " " + "00:00:00"),
                            DateTIme = date.AddDays(1).ToShortDateString() + " "  + "00:00:00",
                            DAPHour = "24"
                        });
                    }

                    
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, tagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetDAPBat(int SiteID, int UnitID, Boolean IsCurrent)
        {
            // No data yet for DAP
            List<TagValue> tagValue = new List<TagValue>();
            Unit unit = new Unit();
            string DapHr = "";
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            Nullable<Double> rtd, price, actual, ru, rd, cr;

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, tagValue);
            }

            var parseAFValue = new Func<AFValue, Nullable<Double>>(afval => {
                Nullable<Double> parsed;
                if (afval.Value.ToString() == "No Data" || afval.Value.ToString() == "Pt Created")
                {
                    parsed = null;
                }
                else
                {
                    parsed = Convert.ToDouble(afval.Value);
                }
                return parsed;
            });

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                string DAPRTD = unit.DAPTagName;
                string DAPPrice = unit.DAPPriceTagName;
                int checkHour = DateTime.Now.Hour;
                DateTime currentDate = DateTime.Now;

                PIPoint ptVolume = PIPoint.FindPIPoint(piServer, DAPRTD);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, DAPPrice);
                PIPoint ptRu = PIPoint.FindPIPoint(piServer, String.Format(Constant.ReserveDefaults.SchedualeTagNames["DAP"], "RU", unit.UnitNumber));
                PIPoint ptRd = PIPoint.FindPIPoint(piServer, String.Format(Constant.ReserveDefaults.SchedualeTagNames["DAP"], "RD", unit.UnitNumber));
                PIPoint ptCr = PIPoint.FindPIPoint(piServer, String.Format(Constant.ReserveDefaults.SchedualeTagNames["DAP"], "FR", unit.UnitNumber));


                if (ptVolume != null && ptPrice != null)
                {
                    if (currentDate.Hour >= 13)
                    {
                        for (int i = 13; i <= 23; i++)
                        {
                            DateTime date = DateTime.Now;
                            string hr;
                            if (i.ToString().Length == 1)
                            {
                                hr = "0" + i.ToString();
                            }
                            else
                            {
                                hr = i.ToString();
                            }

                            var ptVolumeValue = ptVolume.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptPriceValue = ptPrice.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptRuValue = ptRu.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptRdValue = ptRd.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptCrValue = ptCr.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);

                            rtd = parseAFValue(ptVolumeValue);
                            price = parseAFValue(ptPriceValue);
                            ru = parseAFValue(ptRuValue);
                            rd = parseAFValue(ptRdValue);
                            cr = parseAFValue(ptCrValue);

                            tagValue.Add(new TagValue
                            {
                                RTDValue = rtd,
                                PriceValue = price,
                                RRU = ru,
                                RRD = rd,
                                Contingency = cr,
                                TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                                DateTIme = date.ToShortDateString() + " " + hr + ":00:00",
                                DAPHour = Convert.ToString(i)
                            });

                        }

                        for (int i = 0; i <= 12; i++)
                        {
                            DateTime date = DateTime.Now.AddDays(1);
                            string hr;
                            if (i.ToString().Length == 1)
                            {
                                hr = "0" + i.ToString();
                            }
                            else
                            {
                                hr = i.ToString();
                            }

                            var ptVolumeValue = ptVolume.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptPriceValue = ptPrice.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptRuValue = ptRu.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptRdValue = ptRd.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptCrValue = ptCr.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);

                            rtd = parseAFValue(ptVolumeValue);
                            price = parseAFValue(ptPriceValue);
                            ru = parseAFValue(ptRuValue);
                            rd = parseAFValue(ptRdValue);
                            cr = parseAFValue(ptCrValue);

                            tagValue.Add(new TagValue
                            {
                                RTDValue = rtd,
                                PriceValue = price,
                                RRU = ru,
                                RRD = rd,
                                Contingency = cr,
                                TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                                DateTIme = date.ToShortDateString() + " " + hr + ":00:00",
                                DAPHour = Convert.ToString(i) == "0" ? "24" : Convert.ToString(i)
                            });

                        }
                    }
                    else if (currentDate.Hour >= 1 && currentDate.Hour < 13)
                    {
                        DateTime date = DateTime.Now;
                        for (int i = 1; i <= 23; i++)
                        {
                            string hr;
                            if (i.ToString().Length == 1)
                            {
                                hr = "0" + i.ToString();
                            }
                            else
                            {
                                hr = i.ToString();
                            }

                            var ptVolumeValue = ptVolume.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptPriceValue = ptPrice.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptRuValue = ptRu.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptRdValue = ptRd.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            var ptCrValue = ptCr.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);

                            rtd = parseAFValue(ptVolumeValue);
                            price = parseAFValue(ptPriceValue);
                            ru = parseAFValue(ptRuValue);
                            rd = parseAFValue(ptRdValue);
                            cr = parseAFValue(ptCrValue);

                            tagValue.Add(new TagValue
                            {
                                RTDValue = rtd,
                                PriceValue = price,
                                RRU = ru,
                                RRD = rd,
                                Contingency = cr,
                                TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                                DateTIme = date.ToShortDateString() + " " + hr + ":00:00",
                                DAPHour = Convert.ToString(i)
                            });

                        }

                        var ptVolumeVal = ptVolume.RecordedValue(new AFTime(date.AddDays(1).ToShortDateString() + " 00:00:00"), AFRetrievalMode.Exact);
                        var ptPriceVal = ptPrice.RecordedValue(new AFTime(date.AddDays(1).ToShortDateString() + " 00:00:00"), AFRetrievalMode.Exact);
                        var ptRuVal = ptRu.RecordedValue(new AFTime(date.AddDays(1).ToShortDateString() + " 00:00:00"), AFRetrievalMode.Exact);
                        var ptRdVal = ptRd.RecordedValue(new AFTime(date.AddDays(1).ToShortDateString() + " 00:00:00"), AFRetrievalMode.Exact);
                        var ptCrVal = ptCr.RecordedValue(new AFTime(date.AddDays(1).ToShortDateString() + " 00:00:00"), AFRetrievalMode.Exact);

                        rtd = parseAFValue(ptVolumeVal);
                        price = parseAFValue(ptPriceVal);
                        ru = parseAFValue(ptRuVal);
                        rd = parseAFValue(ptRdVal);
                        cr = parseAFValue(ptCrVal);

                        tagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = price,
                            RRU = ru,
                            RRD = rd,
                            Contingency = cr,
                            TimeStamp = Convert.ToDateTime(date.AddDays(1).ToShortDateString() + " " + "00:00:00"),
                            DateTIme = date.AddDays(1).ToShortDateString() + " " + "00:00:00",
                            DAPHour = "24"
                        });
                    }


                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, tagValue);
        }
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetDAP_FTP(int SiteID, int UnitID, Boolean IsCurrent)
        {
            List<TagValue> tagValue = new List<TagValue>();
            Unit unit = new Unit();
            string dateTime;
            string DapHr = "";
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            Nullable<Double> rtd, price, actual;

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, tagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                if (IsCurrent == true)
                {
                    dateTime = DateTime.Now.ToShortDateString();
                }
                else
                {
                    dateTime = DateTime.Now.AddDays(1).ToShortDateString();
                }

                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                string DAPRTD = unit.DAPTagName;
                string DAPPrice = unit.DAPPriceTagName;
                int checkHour = DateTime.Now.Hour;
                DateTime currentDate = DateTime.Now;
                DateTime checkDate = Convert.ToDateTime(DateTime.Now.ToShortDateString() + " 00:00:00");
                DateTime.Now.AddHours(1).AddMinutes(30);


                if (currentDate <= checkDate.AddHours(1).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_20_");
                    DAPPrice = DAPPrice.Replace("_00_", "_20_");
                    DapHr = "20H";
                }
                else if (currentDate >= checkDate.AddHours(1).AddMinutes(30)
                    && currentDate < checkDate.AddHours(5).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_00_");
                    DAPPrice = DAPPrice.Replace("_00_", "_00_");
                    DapHr = "00H";
                }
                else if (currentDate >= checkDate.AddHours(5).AddMinutes(30)
                    && currentDate < checkDate.AddHours(9).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_04_");
                    DAPPrice = DAPPrice.Replace("_00_", "_04_");
                    DapHr = "04H";
                }
                else if (currentDate >= checkDate.AddHours(9).AddMinutes(30)
                    && currentDate < checkDate.AddHours(13).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_08_");
                    DAPPrice = DAPPrice.Replace("_00_", "_08_");
                    DapHr = "08H";
                }
                else if (currentDate >= checkDate.AddHours(13).AddMinutes(30)
                    && currentDate < checkDate.AddHours(17).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_12_");
                    DAPPrice = DAPPrice.Replace("_00_", "_12_");
                    DapHr = "12H";
                }
                else if (currentDate >= checkDate.AddHours(17).AddMinutes(30)
                    && currentDate < checkDate.AddHours(21).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_16_");
                    DAPPrice = DAPPrice.Replace("_00_", "_16_");
                    DapHr = "16H";
                }
                else if (currentDate >= checkDate.AddHours(21).AddMinutes(30))
                {
                    DAPRTD = DAPRTD.Replace("_00_", "_20_");
                    DAPPrice = DAPPrice.Replace("_00_", "_20_");
                    DapHr = "20H";
                }

                PIPoint ptVolume = PIPoint.FindPIPoint(piServer, DAPRTD);
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, DAPPrice);
                PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);

                if (ptVolume != null && ptPrice != null)
                {
                    var ptVolumeValue = ptVolume.RecordedValue(new AFTime(dateTime + " 00:00:00"), AFRetrievalMode.Exact);
                    var ptPriceValue = ptPrice.RecordedValue(new AFTime(dateTime + " 00:00:00"), AFRetrievalMode.Exact);
                    var ptActualValue = ptActual.RecordedValue(new AFTime(dateTime + " 00:00:00"), AFRetrievalMode.Exact);

                    if (ptVolumeValue.Value.ToString() == "No Data" || ptVolumeValue.Value.ToString() == "Pt Created")
                    {
                        rtd = null;
                    }
                    else
                    {
                        rtd = Convert.ToDouble(ptVolumeValue.Value);
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

                    tagValue.Add(new TagValue
                    {
                        RTDValue = rtd
                            ,
                        PriceValue = price
                            //,ActualValue = actual
                            ,
                        TimeStamp = Convert.ToDateTime(dateTime + " 00:00:00")
                            ,
                        DateTIme = dateTime + " 00:00:00"
                    });



                    for (int i = 1; i <= 23; i++)
                    {
                        string hr;
                        if (i.ToString().Length == 1)
                        {
                            hr = "0" + i.ToString();
                        }
                        else
                        {
                            hr = i.ToString();
                        }

                        ptVolumeValue = ptVolume.RecordedValue(new AFTime(dateTime + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                        ptPriceValue = ptPrice.RecordedValue(new AFTime(dateTime + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                        ptActualValue = ptActual.RecordedValue(new AFTime(dateTime + " " + hr + ":00:00"), AFRetrievalMode.Exact);

                        if (ptVolumeValue.Value.ToString() == "No Data" || ptVolumeValue.Value.ToString() == "Pt Created")
                        {
                            rtd = null;
                        }
                        else
                        {
                            rtd = Convert.ToDouble(ptVolumeValue.Value);
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

                        tagValue.Add(new TagValue
                        {
                            RTDValue = rtd
                                ,
                            PriceValue = price
                                //,ActualValue = actual
                                ,
                            TimeStamp = Convert.ToDateTime(dateTime + " " + hr + ":00:00")
                                ,
                            DateTIme = dateTime + " " + hr + ":00:00"
                            ,
                            DAPHour = DapHr
                        });

                    }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, tagValue);
        }

        [HttpPost]
        [Authorize]
        public HttpResponseMessage GetFile([FromBody] BidUpload bid)
        {
            HttpResponseMessage result = null;
            var httpRequest = HttpContext.Current.Request;
            if (httpRequest.Files.Count > 0)
            {
                var docfiles = new List<string>();
                foreach (string file in httpRequest.Files)
                {
                    var postedFile = httpRequest.Files[file];
                    var filePath = "";
                    if (bid.BidType == "Offer")
                    {
                        filePath = HttpContext.Current.Server.MapPath(@"~\Files\BidOffer" + bid.UnitID + ".xlsx");
                    }
                    else
                    {
                        filePath = HttpContext.Current.Server.MapPath(@"~\Files\BidNomination" + bid.UnitID + ".xlsx");
                    }

                    postedFile.SaveAs(filePath);
                    docfiles.Add(filePath);
                }
                result = Request.CreateResponse(HttpStatusCode.Created, docfiles);
            }
            else
            {
                result = Request.CreateResponse(HttpStatusCode.BadRequest);
            }
            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<HttpResponseMessage> GetBidFile()
        {
            string message = "success";
            string DocsPath = WebConfigurationManager.AppSettings["FileUploadedRootPath"];
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }
            var provider = await Request.Content.ReadAsMultipartAsync<InMemoryMultipartFormDataStreamProvider>(new InMemoryMultipartFormDataStreamProvider());
            //access form data  
            NameValueCollection formData = provider.FormData;
            string unitID = formData["UnitID"];
            IList<HttpContent> files = provider.Files;

            string directoryName = String.Empty;
            string filename = String.Empty;
            string thisFileName = "";
            Stream input = null;

            if (files.Count > 0)
            {
                HttpContent file1 = files[0];
                thisFileName = $"BidOffer{unitID}.xlsx";
                input = await file1.ReadAsStreamAsync();
            }
            filename = System.IO.Path.Combine(DocsPath, thisFileName);
            //Deletion exists file  
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            using (Stream file = File.OpenWrite(filename))
            {
                input.CopyTo(file);
                //close file  
                file.Close();
            }
            return Request.CreateResponse(HttpStatusCode.OK, message);
        }
        #endregion

        #region SQL
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetCurrentRTDSQL(int SiteID, int UnitID)
        {
            TagValue currentValue = new TagValue();
            Unit unit = new Unit();
            SQLData sData = new SQLData();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            int minMod = 0;
            
            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, currentValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                
                minMod = 5 - ((DateTime.Now.Minute) % 5);
                DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00"));
                sData = GetSQLData(timestamp, "RTD", unit.UnitNumber);

                string dataStatus = "R";
                if (sData != null)
                {
                    rtd = sData.Quantity;
                    eap = sData.Price;
                    dataStatus = "R";
                }
                else
                {
                    string query = $"exec[sp_OverrideValue] '{unit.UnitNumber}'";
                    var oData = db.Database
                        .SqlQuery<Override>(query)
                        .FirstOrDefault<Override>();
                    if (oData.IsUse == true)
                    {
                        rtd = oData.Value;
                        dataStatus = "O";
                    }
                    else
                    {
                       
                        var hData = GetSQLData(timestamp, "HAP", unit.UnitNumber);
                        if (hData != null)
                        {
                            rtd = hData.Quantity;
                            eap = hData.Price;
                            dataStatus = "H";
                        }
                        else
                        {
                            rtd = null;
                            eap = null;
                            dataStatus = "R";
                        }
                    }
                    
                }

                currentValue = new TagValue
                {
                    RTDValue = rtd,
                    PriceValue = eap,
                    TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                    DateTIme = DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00"),
                    DataStatus = dataStatus
                };
            }

            return Request.CreateResponse(HttpStatusCode.OK, currentValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetRTDPriceCurrentByUnitSQL(string Unit)
        {
            TagValue currentValue = new TagValue();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            int minMod = 0;
            string dataStatus = "R";
            SQLData sData = new SQLData();

            using (TradingEntities db = new TradingEntities())
            {
                minMod = 5 - ((DateTime.Now.Minute) % 5);
                DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00"));
                sData = GetSQLData(timestamp, "RTD", Unit);
                if (sData != null)
                {
                    rtd = sData.Quantity;
                    eap = sData.Price;
                    dataStatus = "R";
                }
                else
                {
                    string query = $"exec[sp_OverrideValue] '{Unit}'";
                    var oData = db.Database
                        .SqlQuery<Override>(query)
                        .FirstOrDefault<Override>();
                    if (oData.IsUse == true)
                    {
                        rtd = oData.Value;
                        dataStatus = "O";
                    }
                    else
                    {
                        var hData = GetSQLData(timestamp, "HAP", Unit);
                        if (hData != null)
                        {
                            rtd = hData.Quantity;
                            eap = hData.Price;
                            dataStatus = "H";
                        }
                        else
                        {
                            rtd = null;
                            eap = null;
                            dataStatus = "R";
                        }
                    }

                }

                currentValue = new TagValue
                {
                    RTDValue = rtd,
                    PriceValue = eap,
                    TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                    DateTIme = DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00"),
                    DataStatus = dataStatus
                };
            }

            return Request.CreateResponse(HttpStatusCode.OK, currentValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAheadRTDSQL(int SiteID, int UnitID)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Unit unit = new Unit();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            int minMod = 0;

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, TagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                minMod = 5 - ((DateTime.Now.Minute) % 5);


                try
                {
                    for (int x = 15; x >= 5; x = x - 5)
                    {
                        DateTime timestamp = Convert.ToDateTime( DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00"));
                        var data = GetSQLData(timestamp, "HAP", unit.UnitNumber);
                        if (data != null)
                        {
                            rtd = data.Quantity;
                            eap = data.Price;
                        }
                        else
                        {
                            rtd = null;
                            eap = null;
                        }

                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = eap,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                            DateTIme = DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")
                        });
                    }
                }
                catch
                { }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAheadRTDDescSQL(int SiteID, int UnitID)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Unit unit = new Unit();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            int minMod = 0;
           
            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, TagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                minMod = 5 - ((DateTime.Now.Minute) % 5);

                try
                {
                    for (int x = 15; x >= 5; x = x - 5)
                    {
                        DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00"));
                        var data = GetSQLData(timestamp, "HAP", unit.UnitNumber);
                        if (data != null)
                        {
                            rtd = data.Quantity;
                            eap = data.Price;
                        }
                        else
                        {
                            rtd = null;
                            eap = null;
                        }

                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = eap,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                            DateTIme = DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")
                        });
                    }
                }
                catch
                { }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue.OrderBy(x => x.TimeStamp));
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAheadRTDByUnitSQL(string Unit)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            int minMod = 0;
           
            using (TradingEntities db = new TradingEntities())
            {
                minMod = 5 - ((DateTime.Now.Minute) % 5);

                try
                {
                    for (int x = 15; x >= 5; x = x - 5)
                    {
                        DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00"));
                        var data = GetSQLData(timestamp, "HAP", Unit);
                        if (data != null)
                        {
                            rtd = data.Quantity;
                            eap = data.Price;
                        }
                        else
                        {
                            rtd = null;
                            eap = null;
                        }

                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = eap,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                            DateTIme = DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")
                        });
                    }
                }
                catch
                { }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }


        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetPastRTDSQL(int SiteID, int UnitID)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> actual = null;
            Nullable<Boolean> isLimit = null;
            Unit unit = new Unit();
            int minMod = 0;
            
            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, TagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                minMod = ((DateTime.Now.Minute) % 5);

                for (int x = 0; x <= 25; x = x + 5)
                {
                    DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00"));
                    var data = GetSQLData(timestamp, "RTD", unit.UnitNumber);
                    var aData = GetSQLData(timestamp, "DTC", unit.UnitNumber);

                    if (data != null)
                    {
                        rtd = data.Quantity;
                        eap = data.Price;
                    }
                    else
                    {
                        rtd = null;
                        eap = null;
                    }

                    if (aData != null)
                    {
                        actual = aData.Quantity;
                    }
                    else
                    {
                        actual = null;
                    }

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
                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = eap,
                            ActualValue = actual,
                            IsLimit = isLimit,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            DateTIme = DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")
                        });
                    }
                    catch
                    { }
                }

            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetPastRTDAHCSQL(int SiteID, int UnitID)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> actual = null;
            Nullable<Boolean> isLimit = null;
            Unit unit = new Unit();
            int minMod = 0;
            
            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, TagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                minMod = ((DateTime.Now.Minute) % 5);

                for (int x = 0; x <= 25; x = x + 5)
                {
                    DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00"));
                    var data = GetSQLData(timestamp, "RTD", unit.UnitNumber);
                    var aData = GetSQLData(timestamp, "DTC", unit.UnitNumber);

                    if (data != null)
                    {
                        rtd = data.Quantity;
                        eap = data.Price;
                    }
                    else
                    {
                        rtd = null;
                        eap = null;
                    }

                    if (aData != null)
                    {
                        actual = aData.Quantity;
                    }
                    else
                    {
                        actual = null;
                    }

                    try
                    {
                        if (actual >= (rtd - 1) && actual <= (rtd + 1))
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
                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = eap,
                            ActualValue = actual,
                            IsLimit = isLimit,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            DateTIme = DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")
                        });
                    }
                    catch
                    { }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetPastRTDDescSQL(int SiteID, int UnitID)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> actual = null;
            Nullable<Boolean> isLimit = null;
            Unit unit = new Unit();
            int minMod = 0;
           
            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, TagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                minMod = ((DateTime.Now.Minute) % 5);

                for (int x = 0; x <= 25; x = x + 5)
                {
                    DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00"));
                    var data = GetSQLData(timestamp, "RTD", unit.UnitNumber);
                    var aData = GetSQLData(timestamp, "DTC", unit.UnitNumber);

                    if (data != null)
                    {
                        rtd = data.Quantity;
                        eap = data.Price;
                    }
                    else
                    {
                        rtd = null;
                        eap = null;
                    }

                    if (aData != null)
                    {
                        actual = aData.Quantity;
                    }
                    else
                    {
                        actual = null;
                    }

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
                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = eap,
                            ActualValue = actual,
                            IsLimit = isLimit,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            DateTIme = DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")
                        });
                    }
                    catch
                    { }
                }

            }


            return Request.CreateResponse(HttpStatusCode.OK, TagValue.OrderBy(x => x.TimeStamp));
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetPastRTDByUnitSQL(string Unit)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> actual = null;
            Nullable<Boolean> isLimit = null;
            int minMod = 0;


            using (TradingEntities db = new TradingEntities())
            {
                minMod = ((DateTime.Now.Minute) % 5);
                for (int x = 0; x <= 25; x = x + 5)
                {
                    DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00"));
                    var data = GetSQLData(timestamp, "RTD", Unit);
                    var aData = GetSQLData(timestamp, "DTC", Unit);

                    if (data != null)
                    {
                        rtd = data.Quantity;
                        eap = data.Price;
                    }
                    else
                    {
                        rtd = null;
                        eap = null;
                    }

                    if (aData != null)
                    {
                        actual = aData.Quantity;
                    }
                    else
                    {
                        actual = null;
                    }

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
                        TagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = eap,
                            ActualValue = actual,
                            IsLimit = isLimit,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            DateTIme = DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")
                        });
                    }
                    catch
                    { }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetRTDPriceCurrentByUnitHourlySQL(string Unit)
        {
            Nullable<Double> dap = null;
            TagValue currentValueHourly = new TagValue();
            int getHour = 0;

            using (TradingEntities db = new TradingEntities())
            {
                DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddHours(getHour + 1).ToString("MM/dd/yyyy HH:00:00"));
                var data = GetSQLData(timestamp, "DAP", Unit);
                if (data != null)
                {
                    dap = data.Quantity;
                }
                else
                {
                    dap = null;
                }

                currentValueHourly = new TagValue
                {
                    DAPValue = dap,
                    TimeStamp = Convert.ToDateTime(DateTime.Now.AddHours(getHour + 1).ToShortDateString() + " " + DateTime.Now.AddHours(getHour + 1).ToString("HH:00:00")),
                    DateTIme = DateTime.Now.AddHours(getHour + 1).ToShortDateString() + " " + DateTime.Now.AddHours(getHour + 1).ToString("HH:00:00")
                };
            }

            return Request.CreateResponse(HttpStatusCode.OK, currentValueHourly);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAheadRTDByUnitHourlySQL(string Unit)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> dap = null;
            int getHour = 0;

            using (TradingEntities db = new TradingEntities())
            {

                try
                {
                    for (int x = 3; x >= 1; x--)
                    {
                        DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddHours(x + getHour + 1).ToString("MM/dd/yyyy HH:00:00"));
                        var data = GetSQLData(timestamp, "DAP", Unit);
                        
                        if(data != null)
                        {
                            dap = data.Quantity;
                        }
                        else
                        {
                            dap = null;
                        }

                        TagValue.Add(new TagValue
                        {
                            DAPValue = dap,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddHours(x + getHour + 1).ToShortDateString() + " " + DateTime.Now.AddHours(x + getHour + 1).ToString("HH:00:00")),
                            DateTIme = DateTime.Now.AddHours(x + getHour + 1).ToShortDateString() + " " + DateTime.Now.AddHours(x + getHour + 1).ToString("HH:00:00")
                        });
                    }

                }
                catch
                { }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetPastRTDByUnitHourlySQL(string Unit)
        {
            List<TagValue> TagValue = new List<TagValue>();
            Nullable<Double> dap = null;
            int getHour = 2;
            using (TradingEntities db = new TradingEntities())
            {
                for (int x = -1; x <= 4; x++)
                {
                    DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddHours(-(getHour + x - 1)).ToString("MM/dd/yyyy HH:00:00"));
                    var data = GetSQLData(timestamp, "DAP", Unit);

                    if (data != null)
                    {
                        dap = data.Quantity;
                    }
                    else
                    {
                        dap = null;
                    }

                    try
                    {
                        TagValue.Add(new TagValue
                        {
                            DAPValue = dap,
                            TimeStamp = Convert.ToDateTime(DateTime.Now.AddHours(-(getHour + x - 1)).ToShortDateString() + " " + DateTime.Now.AddHours(-(getHour + x - 1)).ToString("HH:00:00")),
                            DateTIme = DateTime.Now.AddHours(-(getHour + x - 1)).ToShortDateString() + " " + DateTime.Now.AddHours(-(getHour + x - 1)).ToString("HH:00:00")
                        });
                    }
                    catch
                    { }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetHAPExtendedSQL(int SiteID, int UnitID)
        {
            List<TagValue> tagValue = new List<TagValue>();
            Unit unit = new Unit();
            string dateTime;
            Nullable<double> rtd = null;

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, tagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {

                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();

                for (int i = 0; i <= 60; i = i + 5)
                {
                    string min;
                    dateTime = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH");
                    if (i < 10)
                    {
                        min = "0".ToString() + i.ToString();
                    }
                    else if (i == 60)
                    {
                        min = "00";
                        dateTime = DateTime.Now.AddHours(1).ToShortDateString() + " " + DateTime.Now.AddHours(1).ToString("HH");
                    }
                    else
                    {
                        min = i.ToString();
                    }
                    var data = GetSQLData(Convert.ToDateTime(dateTime + ":" + min + ":00"), "HAP", unit.UnitNumber);
                    if (data != null)
                    {
                        rtd = data.Quantity;
                    }
                    else
                    {
                        rtd = null;
                    }

                    tagValue.Add(new TagValue
                    {
                        RTDValue = rtd,
                        TimeStamp = Convert.ToDateTime(dateTime + ":" + min + ":00"),
                        DateTIme = i == 60 ? "60" : min
                    });

                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, tagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetDAPExtendedSQL(int SiteID, int UnitID, Boolean IsCurrent)
        {
            List<TagValue> tagValue = new List<TagValue>();
            Unit unit = new Unit();
            Nullable<Double> rtd, price;

            if (SiteID == 0 && UnitID == 00)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, tagValue);
            }

            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select top 1 * from m_Unit where IsActive = 1 and siteID = " + SiteID + " and UnitID=" + UnitID;
                unit = db.Database.SqlQuery<Unit>(Str).FirstOrDefault<Unit>();
                DateTime currentDate = DateTime.Now;

                if (currentDate.Hour >= 13)
                {
                    for (int i = 13; i <= 23; i++)
                    {
                        DateTime date = DateTime.Now;
                        string hr;
                        if (i.ToString().Length == 1)
                        {
                            hr = "0" + i.ToString();
                        }
                        else
                        {
                            hr = i.ToString();
                        }
                        DateTime timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00");
                        var data = GetSQLData(timestamp, "DAP", unit.UnitNumber);
                        if(data != null)
                        {
                            rtd = data.Quantity;
                            price = data.Price;
                        }
                        else
                        {
                            rtd = null;
                            price = null;
                        }
                        tagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = price,
                            TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                            DateTIme = date.ToShortDateString() + " " + hr + ":00:00",
                            DAPHour = Convert.ToString(i)
                        });

                    }

                    for (int i = 0; i <= 12; i++)
                    {
                        DateTime date = DateTime.Now.AddDays(1);
                        string hr;
                        if (i.ToString().Length == 1)
                        {
                            hr = "0" + i.ToString();
                        }
                        else
                        {
                            hr = i.ToString();
                        }

                        DateTime timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00");
                        var data = GetSQLData(timestamp, "DAP", unit.UnitNumber);
                        if (data != null)
                        {
                            rtd = data.Quantity;
                            price = data.Price;
                        }
                        else
                        {
                            rtd = null;
                            price = null;
                        }

                        tagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = price,
                            TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                            DateTIme = date.ToShortDateString() + " " + hr + ":00:00",
                            DAPHour = Convert.ToString(i) == "0" ? "24" : Convert.ToString(i)
                        });

                    }
                }
                else if (currentDate.Hour >= 1 && currentDate.Hour < 13)
                {
                    DateTime date = DateTime.Now;
                    for (int i = 1; i <= 23; i++)
                    {
                        string hr;
                        if (i.ToString().Length == 1)
                        {
                            hr = "0" + i.ToString();
                        }
                        else
                        {
                            hr = i.ToString();
                        }

                        DateTime timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00");
                        var data = GetSQLData(timestamp, "DAP", unit.UnitNumber);
                        if (data != null)
                        {
                            rtd = data.Quantity;
                            price = data.Price;
                        }
                        else
                        {
                            rtd = null;
                            price = null;
                        }

                        tagValue.Add(new TagValue
                        {
                            RTDValue = rtd,
                            PriceValue = price,
                            TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                            DateTIme = date.ToShortDateString() + " " + hr + ":00:00",
                            DAPHour = Convert.ToString(i)
                        });

                    }

                    DateTime timestamp1 = Convert.ToDateTime(date.AddDays(1).ToShortDateString() + " 00:00:00");
                    var data1 = GetSQLData(timestamp1, "DAP", unit.UnitNumber);
                    if (data1 != null)
                    {
                        rtd = data1.Quantity;
                        price = data1.Price;
                    }
                    else
                    {
                        rtd = null;
                        price = null;
                    }

                    tagValue.Add(new TagValue
                    {
                        RTDValue = rtd,
                        PriceValue = price,
                        TimeStamp = Convert.ToDateTime(date.AddDays(1).ToShortDateString() + " " + "00:00:00"),
                        DateTIme = date.AddDays(1).ToShortDateString() + " " + "00:00:00",
                        DAPHour = "24"
                    });
                }


            }

            return Request.CreateResponse(HttpStatusCode.OK, tagValue);
        }

        public SQLData GetSQLData(DateTime timestamp, string DataType, string Unit)
        {
            SQLData sData = new SQLData();
            using (TradingEntities db = new TradingEntities())
            {
                string query = $@"exec sp_IEMOPSQLData '{timestamp}','{DataType}','{Unit}'";
                sData = db.Database.SqlQuery<SQLData>(query).FirstOrDefault<SQLData>();
            }
            return sData;
        }

        #endregion

        #region AHC
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetCurrentRTDAHC()
        {
            AHCTagValue currentValue = new AHCTagValue();
            TagValue unit1Value = new TagValue();
            TagValue unit2Value = new TagValue();
            List<Unit> units = new List<Unit>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            int minMod = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            piServer.Connect(true);

            using (TradingEntities db = new TradingEntities())
            {
                string Str = $"select * from m_Unit where IsActive = 1 and siteID = {AHCSiteID}";
                units = db.Database.SqlQuery<Unit>(Str).ToList<Unit>();

                foreach(var unit in units)
                {
                    PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                    PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagName);
                    PIPoint ptHAP = PIPoint.FindPIPoint(piServer, unit.HAPTagName);


                    if (ptRTD != null && ptPrice != null)
                    {
                        minMod = 5 - ((DateTime.Now.Minute) % 5);
                        AFValue afValueRTD = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                        AFValue afValuePrice = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                        string dataStatus = "R";

                        if (afValueRTD.Value.ToString() == "No Data" || afValueRTD.Value.ToString() == "Pt Created")
                        {
                            string query = $"exec[sp_OverrideValue] '{unit.UnitNumber}'";
                            var data = db.Database
                                .SqlQuery<Override>(query)
                                .FirstOrDefault<Override>();
                            if (data.IsUse == true)
                            {
                                rtd = data.Value;
                                dataStatus = "O";
                            }
                            else
                            {
                                var ptHAPValue = ptHAP.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                                if (ptHAPValue.Value.ToString() == "No Data" || ptHAPValue.Value.ToString() == "Pt Created")
                                {
                                    rtd = null;
                                    dataStatus = "R";
                                }
                                else
                                {
                                    rtd = Convert.ToDouble(ptHAPValue.Value);
                                    dataStatus = "H";
                                }
                            }
                        }
                        else
                        {
                            //disable override
                            string query = $@"update t_OverrideValue 
                                            set IsUse = 0 
                                        where UnitNumber = '{unit.UnitNumber}'";
                            db.Database.ExecuteSqlCommand(query);

                            rtd = Convert.ToDouble(afValueRTD.Value);
                        }

                        if (afValuePrice.Value.ToString() == "No Data" || afValuePrice.Value.ToString() == "Pt Created")
                        {
                            eap = null;
                        }
                        else
                        {
                            eap = Convert.ToDouble(afValuePrice.Value);
                        }

                        if(unit.UnitNumber == "01ANGAT_M")
                        {
                            unit1Value = new TagValue
                            {
                                RTDValue = rtd,
                                PriceValue = eap,
                                TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                                DateTIme = DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00"),
                                DataStatus = dataStatus
                            };
                        }
                        else
                        {
                            unit2Value = new TagValue
                            {
                                RTDValue = rtd,
                                PriceValue = eap,
                                TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                                DateTIme = DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00"),
                                DataStatus = dataStatus
                            };
                        }
                        
                    }
                }

                currentValue.ActualValue = unit1Value.ActualValue;
                currentValue.ActualValue2 = unit2Value.ActualValue;
                currentValue.RTDValue = unit1Value.RTDValue;
                currentValue.RTDValue2 = unit2Value.RTDValue;
                currentValue.PriceValue = unit1Value.PriceValue;
                currentValue.PriceValue2 = unit2Value.PriceValue;
                currentValue.DateTIme = unit1Value.DateTIme;
                currentValue.DataStatus = unit1Value.DataStatus;
                currentValue.DataStatus2 = unit2Value.DataStatus;
                currentValue.TimeStamp = unit1Value.TimeStamp;

            }

            return Request.CreateResponse(HttpStatusCode.OK, currentValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAheadRTDAHC()
        {
            List<AHCTagValue> TagValue = new List<AHCTagValue>();
            List<TagValue> TagValue2 = new List<TagValue>();
            List<Unit> units = new List<Unit>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            int minMod = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            using (TradingEntities db = new TradingEntities())
            {
                string Str = $"select * from m_Unit where IsActive = 1 and siteID = {AHCSiteID}";
                units = db.Database.SqlQuery<Unit>(Str).ToList<Unit>();

                foreach(var unit in units)
                {
                    //PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.UnitNumber + "_HAP_V");
                    //PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.UnitNumber + "_HAP_P");
                    PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.HAPTagName);
                    PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.HAPPriceTagName);

                    minMod = 5 - ((DateTime.Now.Minute) % 5);
                    try
                    {
                        for (int x = 15; x >= 5; x = x - 5)
                        {

                            var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                            var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);

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
                                eap = null;
                            }
                            else
                            {
                                eap = Convert.ToDouble(ptPriceValue.Value);
                            }

                            if(unit.UnitNumber == "01ANGAT_M")
                            {
                                TagValue.Add(new AHCTagValue
                                {
                                    RTDValue = rtd,
                                    PriceValue = eap,
                                    TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                                    DateTIme = DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")
                                });
                            }
                            else
                            {
                                TagValue2.Add(new TagValue
                                {
                                    RTDValue = rtd,
                                    PriceValue = eap,
                                    TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                                    DateTIme = DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")
                                });
                            }
                            
                        }
                    }
                    catch
                    { }
                }

                foreach(var val2 in TagValue2)
                {
                    var update = TagValue.FirstOrDefault(x => x.DateTIme == val2.DateTIme);
                    update.RTDValue2 = val2.RTDValue;
                    update.PriceValue2 = val2.PriceValue;
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetPastRTDAHC()
        {
            List<AHCTagValue> TagValue = new List<AHCTagValue>();
            List<TagValue> TagValue2 = new List<TagValue>();
            Nullable<Double> rtd = null;
            Nullable<Double> eap = null;
            Nullable<Double> actual = null;
            Nullable<Boolean> isLimit = null;
            List<Unit> units = new List<Unit>();
            int minMod = 0;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            using (TradingEntities db = new TradingEntities())
            {
                string Str = $"select * from m_Unit where IsActive = 1 and siteID = {AHCSiteID}";
                units = db.Database.SqlQuery<Unit>(Str).ToList<Unit>();

                foreach(var unit in units)
                {
                    PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                    PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagName);
                    PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);
                    PIPoint ptCompliance = PIPoint.FindPIPoint(piServer, unit.ComplianceTagName);

                    minMod = ((DateTime.Now.Minute) % 5);

                    for (int x = 0; x <= 25; x = x + 5)
                    {
                        var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                        var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                        var ptActualValue = ptActual.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                        var ptComplianceValue = ptCompliance.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);

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
                            eap = null;
                        }
                        else
                        {
                            eap = Convert.ToDouble(ptPriceValue.Value);
                        }

                        if (ptActualValue.Value.ToString() == "No Data" || ptActualValue.Value.ToString() == "Pt Created")
                        {
                            actual = null;
                        }
                        else
                        {
                            actual = Convert.ToDouble(ptActualValue.Value);
                        }

                        try
                        {
                            if (actual >= (rtd - 1) && actual <= (rtd + 1))
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
                            if(unit.UnitNumber == "01ANGAT_M")
                            {
                                TagValue.Add(new AHCTagValue
                                {
                                    RTDValue = rtd,
                                    PriceValue = eap,
                                    ActualValue = actual,
                                    IsLimit = isLimit,
                                    TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                                    DateTIme = DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")
                                });
                            }
                            else
                            {
                                TagValue2.Add(new TagValue
                                {
                                    RTDValue = rtd,
                                    PriceValue = eap,
                                    ActualValue = actual,
                                    IsLimit = isLimit,
                                    TimeStamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                                    DateTIme = DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")
                                });
                            }

                            
                        }
                        catch
                        { }
                    }
                }

                foreach(var val2 in TagValue2)
                {
                    var update = TagValue.FirstOrDefault(x => x.DateTIme == val2.DateTIme);
                    update.RTDValue2 = val2.RTDValue;
                    update.ActualValue2 = val2.ActualValue;
                    update.PriceValue2 = val2.PriceValue;
                    update.IsLimit2 = val2.IsLimit;
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, TagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetHAPExtendedAHC()
        {
            List<AHCTagValue> tagValue = new List<AHCTagValue>();
            List<TagValue> tagValue2 = new List<TagValue>();
            List<Unit> units = new List<Unit>();
            string dateTime;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            Nullable<Double> rtd, price, actual;

            using (TradingEntities db = new TradingEntities())
            {

                string Str = $"select * from m_Unit where IsActive = 1 and siteID = {AHCSiteID}";
                units = db.Database.SqlQuery<Unit>(Str).ToList<Unit>();

                foreach(var unit in units)
                {
                    PIPoint ptVolume = PIPoint.FindPIPoint(piServer, unit.HAPTagName);
                    PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.HAPPriceTagName);
                    PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);

                    if (ptVolume != null && ptPrice != null)
                    {
                        for (int i = 0; i <= 60; i = i + 5)
                        {
                            string min;
                            dateTime = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH");
                            if (i < 10)
                            {
                                min = "0".ToString() + i.ToString();
                            }
                            else if (i == 60)
                            {
                                min = "00";
                                dateTime = DateTime.Now.AddHours(1).ToShortDateString() + " " + DateTime.Now.AddHours(1).ToString("HH");
                            }
                            else
                            {
                                min = i.ToString();
                            }

                            var ptVolumeValue = ptVolume.RecordedValue(new AFTime(dateTime + ":" + min + ":00"), AFRetrievalMode.Exact);
                            var ptPriceValue = ptPrice.RecordedValue(new AFTime(dateTime + ":" + min + ":00"), AFRetrievalMode.Exact);
                            var ptActualValue = ptActual.RecordedValue(new AFTime(dateTime + ":" + min + ":00"), AFRetrievalMode.Exact);

                            if (ptVolumeValue.Value.ToString() == "No Data" || ptVolumeValue.Value.ToString() == "Pt Created")
                            {
                                rtd = null;
                            }
                            else
                            {
                                rtd = Convert.ToDouble(ptVolumeValue.Value);
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
                            if(unit.UnitNumber == "01ANGAT_M")
                            {
                                tagValue.Add(new AHCTagValue
                                {
                                    RTDValue = rtd,
                                    PriceValue = price,
                                    ActualValue = actual,
                                    TimeStamp = Convert.ToDateTime(dateTime + ":" + min + ":00"),
                                    DateTIme = i == 60 ? "60" : min
                                });
                            }
                            else
                            {
                                tagValue2.Add(new TagValue
                                {
                                    RTDValue = rtd,
                                    PriceValue = price,
                                    ActualValue = actual,
                                    TimeStamp = Convert.ToDateTime(dateTime + ":" + min + ":00"),
                                    DateTIme = i == 60 ? "60" : min
                                });
                            }
                            

                        }
                    }
                }

                foreach (var val2 in tagValue2)
                {
                    var update = tagValue.FirstOrDefault(x => x.DateTIme == val2.DateTIme);
                    update.RTDValue2 = val2.RTDValue;
                    update.ActualValue2 = val2.ActualValue;
                    update.PriceValue2 = val2.PriceValue;
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, tagValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetDAPExtendedAHC()
        {
            List<AHCTagValue> tagValue = new List<AHCTagValue>();
            List<TagValue> tagValue2 = new List<TagValue>();
            List<Unit> units = new List<Unit>();
            string DapHr = "";
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            Nullable<Double> rtd, price, actual;

            using (TradingEntities db = new TradingEntities())
            {
                string Str = $"select * from m_Unit where IsActive = 1 and siteID ={AHCSiteID}";
                units = db.Database.SqlQuery<Unit>(Str).ToList<Unit>();

                foreach(var unit in units)
                {
                    string DAPRTD = unit.DAPTagName;
                    string DAPPrice = unit.DAPPriceTagName;
                    int checkHour = DateTime.Now.Hour;
                    DateTime currentDate = DateTime.Now;

                    PIPoint ptVolume = PIPoint.FindPIPoint(piServer, DAPRTD);
                    PIPoint ptPrice = PIPoint.FindPIPoint(piServer, DAPPrice);

                    if (ptVolume != null && ptPrice != null)
                    {
                        if (currentDate.Hour >= 13)
                        {
                            for (int i = 13; i <= 23; i++)
                            {
                                DateTime date = DateTime.Now;
                                string hr;
                                if (i.ToString().Length == 1)
                                {
                                    hr = "0" + i.ToString();
                                }
                                else
                                {
                                    hr = i.ToString();
                                }

                                var ptVolumeValue = ptVolume.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                                var ptPriceValue = ptPrice.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);

                                if (ptVolumeValue.Value.ToString() == "No Data" || ptVolumeValue.Value.ToString() == "Pt Created")
                                {
                                    rtd = null;
                                }
                                else
                                {
                                    rtd = Convert.ToDouble(ptVolumeValue.Value);
                                }

                                if (ptPriceValue.Value.ToString() == "No Data" || ptPriceValue.Value.ToString() == "Pt Created")
                                {
                                    price = null;
                                }
                                else
                                {
                                    price = Convert.ToDouble(ptPriceValue.Value);
                                }

                                if (unit.UnitNumber == "01ANGAT_M")
                                {
                                    tagValue.Add(new AHCTagValue
                                    {
                                        RTDValue = rtd,
                                        PriceValue = price,
                                        TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                                        DateTIme = date.ToShortDateString() + " " + hr + ":00:00",
                                        DAPHour = Convert.ToString(i)
                                    });
                                }
                                else
                                {
                                    tagValue2.Add(new TagValue
                                    {
                                        RTDValue = rtd,
                                        PriceValue = price,
                                        TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                                        DateTIme = date.ToShortDateString() + " " + hr + ":00:00",
                                        DAPHour = Convert.ToString(i)
                                    });
                                }
                                    

                            }

                            for (int i = 0; i <= 12; i++)
                            {
                                DateTime date = DateTime.Now.AddDays(1);
                                string hr;
                                if (i.ToString().Length == 1)
                                {
                                    hr = "0" + i.ToString();
                                }
                                else
                                {
                                    hr = i.ToString();
                                }

                                var ptVolumeValue = ptVolume.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                                var ptPriceValue = ptPrice.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);

                                if (ptVolumeValue.Value.ToString() == "No Data" || ptVolumeValue.Value.ToString() == "Pt Created")
                                {
                                    rtd = null;
                                }
                                else
                                {
                                    rtd = Convert.ToDouble(ptVolumeValue.Value);
                                }

                                if (ptPriceValue.Value.ToString() == "No Data" || ptPriceValue.Value.ToString() == "Pt Created")
                                {
                                    price = null;
                                }
                                else
                                {
                                    price = Convert.ToDouble(ptPriceValue.Value);
                                }

                                if (unit.UnitNumber == "01ANGAT_M")
                                {
                                    tagValue.Add(new AHCTagValue
                                    {
                                        RTDValue = rtd,
                                        PriceValue = price,
                                        TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                                        DateTIme = date.ToShortDateString() + " " + hr + ":00:00",
                                        DAPHour = Convert.ToString(i) == "0" ? "24" : Convert.ToString(i)
                                    });
                                }
                                else
                                {
                                    tagValue2.Add(new TagValue
                                    {
                                        RTDValue = rtd,
                                        PriceValue = price,
                                        TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                                        DateTIme = date.ToShortDateString() + " " + hr + ":00:00",
                                        DAPHour = Convert.ToString(i) == "0" ? "24" : Convert.ToString(i)
                                    });
                                }
                            }
                        }
                        else if (currentDate.Hour >= 1 && currentDate.Hour < 13)
                        {
                            DateTime date = DateTime.Now;
                            for (int i = 1; i <= 23; i++)
                            {
                                string hr;
                                if (i.ToString().Length == 1)
                                {
                                    hr = "0" + i.ToString();
                                }
                                else
                                {
                                    hr = i.ToString();
                                }

                                var ptVolumeValue = ptVolume.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                                var ptPriceValue = ptPrice.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);

                                if (ptVolumeValue.Value.ToString() == "No Data" || ptVolumeValue.Value.ToString() == "Pt Created")
                                {
                                    rtd = null;
                                }
                                else
                                {
                                    rtd = Convert.ToDouble(ptVolumeValue.Value);
                                }

                                if (ptPriceValue.Value.ToString() == "No Data" || ptPriceValue.Value.ToString() == "Pt Created")
                                {
                                    price = null;
                                }
                                else
                                {
                                    price = Convert.ToDouble(ptPriceValue.Value);
                                }

                                if (unit.UnitNumber == "01ANGAT_M")
                                {
                                    tagValue.Add(new AHCTagValue
                                    {
                                        RTDValue = rtd,
                                        PriceValue = price,
                                        TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                                        DateTIme = date.ToShortDateString() + " " + hr + ":00:00",
                                        DAPHour = Convert.ToString(i)
                                    });
                                }
                                else
                                {
                                    tagValue2.Add(new TagValue
                                    {
                                        RTDValue = rtd,
                                        PriceValue = price,
                                        TimeStamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                                        DateTIme = date.ToShortDateString() + " " + hr + ":00:00",
                                        DAPHour = Convert.ToString(i)
                                    });
                                }
                                    

                            }

                            var ptVolumeVal = ptVolume.RecordedValue(new AFTime(date.AddDays(1).ToShortDateString() + " 00:00:00"), AFRetrievalMode.Exact);
                            var ptPriceVal = ptPrice.RecordedValue(new AFTime(date.AddDays(1).ToShortDateString() + " 00:00:00"), AFRetrievalMode.Exact);

                            if (ptVolumeVal.Value.ToString() == "No Data" || ptVolumeVal.Value.ToString() == "Pt Created")
                            {
                                rtd = null;
                            }
                            else
                            {
                                rtd = Convert.ToDouble(ptVolumeVal.Value);
                            }

                            if (ptPriceVal.Value.ToString() == "No Data" || ptPriceVal.Value.ToString() == "Pt Created")
                            {
                                price = null;
                            }
                            else
                            {
                                price = Convert.ToDouble(ptPriceVal.Value);
                            }

                            if (unit.UnitNumber == "01ANGAT_M")
                            {
                                tagValue.Add(new AHCTagValue
                                {
                                    RTDValue = rtd,
                                    PriceValue = price,
                                    TimeStamp = Convert.ToDateTime(date.AddDays(1).ToShortDateString() + " " + "00:00:00"),
                                    DateTIme = date.AddDays(1).ToShortDateString() + " " + "00:00:00",
                                    DAPHour = "24"
                                });
                            }
                            else
                            {
                                tagValue2.Add(new TagValue
                                {
                                    RTDValue = rtd,
                                    PriceValue = price,
                                    TimeStamp = Convert.ToDateTime(date.AddDays(1).ToShortDateString() + " " + "00:00:00"),
                                    DateTIme = date.AddDays(1).ToShortDateString() + " " + "00:00:00",
                                    DAPHour = "24"
                                });
                            }
                        }
                    }
                }

                //Update tagValueList

                foreach (var val2 in tagValue2)
                {
                    var update = tagValue.FirstOrDefault(x => x.DateTIme == val2.DateTIme);
                    update.RTDValue2 = val2.RTDValue;
                    update.PriceValue2 = val2.PriceValue;
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, tagValue);
        }
        #endregion

        private string GetUsername()
        {
            var identity = (ClaimsIdentity)User.Identity;
            return identity.FindFirst("UserName").Value;
        }
    }

    public class InMemoryMultipartFormDataStreamProvider : MultipartStreamProvider
    {
        private NameValueCollection _formData = new NameValueCollection();
        private List<HttpContent> _fileContents = new List<HttpContent>();

        // Set of indexes of which HttpContents we designate as form data  
        private Collection<bool> _isFormData = new Collection<bool>();

        /// <summary>  
        /// Gets a <see cref="NameValueCollection"/> of form data passed as part of the multipart form data.  
        /// </summary>  
        public NameValueCollection FormData
        {
            get { return _formData; }
        }

        /// <summary>  
        /// Gets list of <see cref="HttpContent"/>s which contain uploaded files as in-memory representation.  
        /// </summary>  
        public List<HttpContent> Files
        {
            get { return _fileContents; }
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            // For form data, Content-Disposition header is a requirement  
            ContentDispositionHeaderValue contentDisposition = headers.ContentDisposition;
            if (contentDisposition != null)
            {
                // We will post process this as form data  
                _isFormData.Add(String.IsNullOrEmpty(contentDisposition.FileName));

                return new MemoryStream();
            }

            // If no Content-Disposition header was present.  
            throw new InvalidOperationException(string.Format("Did not find required '{0}' header field in MIME multipart body part..", "Content-Disposition"));
        }

        /// <summary>  
        /// Read the non-file contents as form data.  
        /// </summary>  
        /// <returns></returns>  
        public override async Task ExecutePostProcessingAsync()
        {
            // Find instances of non-file HttpContents and read them asynchronously  
            // to get the string content and then add that as form data  
            for (int index = 0; index < Contents.Count; index++)
            {
                if (_isFormData[index])
                {
                    HttpContent formContent = Contents[index];
                    // Extract name from Content-Disposition header. We know from earlier that the header is present.  
                    ContentDispositionHeaderValue contentDisposition = formContent.Headers.ContentDisposition;
                    string formFieldName = UnquoteToken(contentDisposition.Name) ?? String.Empty;

                    // Read the contents as string data and add to form data  
                    string formFieldValue = await formContent.ReadAsStringAsync();
                    FormData.Add(formFieldName, formFieldValue);
                }
                else
                {
                    _fileContents.Add(Contents[index]);
                }
            }
        }

        /// <summary>  
        /// Remove bounding quotes on a token if present  
        /// </summary>  
        /// <param name="token">Token to unquote.</param>  
        /// <returns>Unquoted token.</returns>  
        private static string UnquoteToken(string token)
        {
            if (String.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            if (token.StartsWith("\"", StringComparison.Ordinal) && token.EndsWith("\"", StringComparison.Ordinal) && token.Length > 1)
            {
                return token.Substring(1, token.Length - 2);
            }

            return token;
        }

    }
}
