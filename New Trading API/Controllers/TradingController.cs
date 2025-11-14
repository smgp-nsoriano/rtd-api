using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using OSIsoft.AF.Asset;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using OSIsoft.AF.Data;
using New_Trading_API.Models;
using System.Data;

namespace New_Trading_API.Controllers
{
    public class TradingController : ApiController
    {
        const int AHCSiteID = 1009;
        #region Main

        [HttpGet]
        [Authorize]
        public HttpResponseMessage Interval()
        {
            List<Schedule> sched = new List<Schedule>();
            int minMod = 0;
            minMod = 5 - ((DateTime.Now.Minute) % 5);

            //Ahead and Current
            for (int x = 20; x >= 0; x = x - 5)
            {
                sched.Add(new Schedule {
                    Value = null,
                    Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                    TimestampLabel = DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm")
                });
            }

            //Previous
            minMod = ((DateTime.Now.Minute) % 5);
            for (int x = 0; x <= 45; x = x + 5)
            {
                sched.Add(new Schedule
                {
                    Value = null,
                    Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                    TimestampLabel = DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm")
                });
            }

            return Request.CreateResponse(HttpStatusCode.OK, sched);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage CurrentDateTime()
        {
            string DT = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss");
            return Request.CreateResponse(HttpStatusCode.OK, DT);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage Price(string UnitNumber)
        {
            List<Schedule> sched = new List<Schedule>();
            int minMod = 0;
            Nullable<Double> val = null;
            Unit unit = new Unit();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            using (TradingEntities db = new TradingEntities())
            {
                string Str = $"select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '{UnitNumber}'";
                unit = db.Database
                    .SqlQuery<Unit>(Str)
                    .FirstOrDefault<Unit>();

                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagName);
                PIPoint ptHAPPrice = PIPoint.FindPIPoint(piServer, unit.HAPPriceTagName);
                minMod = 5 - ((DateTime.Now.Minute) % 5);

                //Ahead
                for (int x = 20; x >= 5; x = x - 5)
                {
                    if (UnitNumber == "")
                    {
                        sched.Add(new Schedule
                        {
                            Value = null,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm")
                        });
                    }
                    else
                    {
                        //get pi data
                        var ptPriceValue = ptHAPPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                        if (ptPriceValue.Value.ToString() == "No Data" || ptPriceValue.Value.ToString() == "Pt Created")
                        {
                            val = null;
                        }
                        else
                        {
                            val = Convert.ToDouble(ptPriceValue.Value);
                        }

                        sched.Add(new Schedule
                        {
                            Value = val,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm")
                        });
                    }

                }

                //Current
                if (UnitNumber == "")
                {
                    sched.Add(new Schedule
                    {
                        Value = null,
                        Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                        TimestampLabel = DateTime.Now.AddMinutes(minMod).ToString("HH:mm")
                    });
                }
                else
                {
                    //get pi data
                    var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    if (ptPriceValue.Value.ToString() == "No Data" || ptPriceValue.Value.ToString() == "Pt Created")
                    {
                        val = null;
                    }
                    else
                    {
                        val = Convert.ToDouble(ptPriceValue.Value);
                    }

                    sched.Add(new Schedule
                    {
                        Value = val,
                        Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                        TimestampLabel = DateTime.Now.AddMinutes(minMod).ToString("HH:mm")
                    });
                }


                //Previous
                minMod = ((DateTime.Now.Minute) % 5);
                for (int x = 0; x <= 45; x = x + 5)
                {
                    if (UnitNumber == "")
                    {
                        sched.Add(new Schedule
                        {
                            Value = null,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm")
                        });
                    }
                    else
                    {
                        //get pi data
                        var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                        if (ptPriceValue.Value.ToString() == "No Data" || ptPriceValue.Value.ToString() == "Pt Created")
                        {
                            val = null;
                        }
                        else
                        {
                            val = Convert.ToDouble(ptPriceValue.Value);
                        }

                        sched.Add(new Schedule
                        {
                            Value = val,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm")
                        });
                    }

                }
            }
            return Request.CreateResponse(HttpStatusCode.OK, sched);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage RTD(string UnitNumber)
        {
            List<Schedule> sched = new List<Schedule>();
            int minMod = 0;
            Nullable<Double> val = null;
            Nullable<Double> actual = null;
            Unit unit = new Unit();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            using (TradingEntities db = new TradingEntities())
            {
                string Str = $"select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '{UnitNumber}'";
                unit = db.Database
                    .SqlQuery<Unit>(Str)
                    .FirstOrDefault<Unit>();

                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                PIPoint ptRTDPlus10 = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                PIPoint ptHAPRTD = PIPoint.FindPIPoint(piServer, unit.HAPTagName);
                PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);
                PIPoint ptHAP = PIPoint.FindPIPoint(piServer, unit.HAPTagName);
                minMod = 5 - ((DateTime.Now.Minute) % 5);
                int aheadMin = 5;

                //plus 10 mins
                var ptRTDValue10 = ptRTDPlus10.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod + 5).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod + 5).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                if (ptRTDValue10.Value.ToString() == "No Data" || ptRTDValue10.Value.ToString() == "Pt Created")
                {
                    aheadMin = 5;
                }
                else
                {
                    aheadMin = 10;
                    
                }

                //Ahead
                for (int x = 20; x >= aheadMin; x = x - 5)
                {
                    if (UnitNumber == "")
                    {
                        sched.Add(new Schedule
                        {
                            Value = null,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm")
                        });
                    }
                    else
                    {
                        //get pi data
                        var ptRTDValue = ptHAPRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                        if (ptRTDValue.Value.ToString() == "No Data" || ptRTDValue.Value.ToString() == "Pt Created")
                        {
                            val = null;
                        }
                        else
                        {
                            val = Convert.ToDouble(ptRTDValue.Value);
                        }
                        sched.Add(new Schedule
                        {
                            Value = val,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm")
                        });
                    }

                }

                if (aheadMin == 10)
                {
                    val = Convert.ToDouble(ptRTDValue10.Value);
                    sched.Add(new Schedule
                    {
                        Value = val,
                        Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(5 + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(5 + minMod).ToString("HH:mm:00")),
                        TimestampLabel = DateTime.Now.AddMinutes(5 + minMod).ToString("HH:mm")
                    });
                }


                string dataStatus = "R";
                //Current
                if (UnitNumber == "")
                {
                    sched.Add(new Schedule
                    {
                        Value = null,
                        Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                        TimestampLabel = DateTime.Now.AddMinutes(minMod).ToString("HH:mm")
                    });
                }
                else
                {
                    //get pi data
                    var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    //get MOT
                    string queryMOT = $"exec[sp_MOT] '{UnitNumber}'";
                    var dataMOT = db.Database
                        .SqlQuery<Override>(queryMOT)
                        .FirstOrDefault<Override>();
                    if (dataMOT.IsUse == true)
                    {
                        dataStatus = "M";
                        val = dataMOT.Value;

                        sched.Add(new Schedule
                        {
                            Value = val,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(minMod).ToString("HH:mm"),
                            DataStatus = dataStatus
                        });
                    }
                    else
                    {
                        if (ptRTDValue.Value.ToString() == "No Data" || ptRTDValue.Value.ToString() == "Pt Created")
                        {

                            string query = $"exec[sp_OverrideValue] '{UnitNumber}'";
                            var data = db.Database
                                .SqlQuery<Override>(query)
                                .FirstOrDefault<Override>();
                            if (data.IsUse == true)
                            {
                                val = data.Value;
                                dataStatus = "O";
                            }
                            else
                            {
                                var ptHAPValue = ptHAP.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                                if (ptHAPValue.Value.ToString() == "No Data" || ptHAPValue.Value.ToString() == "Pt Created")
                                {
                                    val = null;
                                    dataStatus = "R";
                                }
                                else
                                {
                                    val = Convert.ToDouble(ptHAPValue.Value);
                                    dataStatus = "H";
                                }
                            }

                        }
                        else
                        {
                            //disable override 
                            string query = $@"update t_OverrideValue 
                                            set IsUse = 0 
                                        where UnitNumber = '{UnitNumber}'";
                            db.Database.ExecuteSqlCommand(query);

                            val = Convert.ToDouble(ptRTDValue.Value);
                            dataStatus = "R";
                        }
                        sched.Add(new Schedule
                        {
                            Value = val,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(minMod).ToString("HH:mm"),
                            DataStatus = dataStatus,
                            IsWithAlarm = unit.IsWithAlarm
                        });
                    }
                }

                //Previous
                minMod = ((DateTime.Now.Minute) % 5);
                for (int x = 0; x <= 45; x = x + 5)
                {
                    if (UnitNumber == "")
                    {
                        sched.Add(new Schedule
                        {
                            Value = null,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm")
                        });
                    }
                    else
                    {
                        //get pi data
                        Nullable<bool> isLimit;
                        var ptRTDValue = ptRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                        if (ptRTDValue.Value.ToString() == "No Data" || ptRTDValue.Value.ToString() == "Pt Created")
                        {
                            val = null;
                        }
                        else
                        {
                            val = Convert.ToDouble(ptRTDValue.Value);
                        }

                        var ptActualValue = ptActual.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
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
                            if (actual >= (val - (val * 0.03)) && actual <= (val + (val * 0.015)))
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


                        sched.Add(new Schedule
                        {
                            Value = val,
                            Actual = actual,
                            IsLimit = isLimit,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm")
                        });
                    }

                }
            }
            return Request.CreateResponse(HttpStatusCode.OK, sched);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetUnitHAP(string UnitNumber)
        {
            List<Schedule> sched = new List<Schedule>();
            Unit unit = new Unit();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            string dateTime;
            Nullable<Double> val = null;

            using (TradingEntities db = new TradingEntities())
            {
                string Str = $"select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '{UnitNumber}'";
                unit = db.Database
                    .SqlQuery<Unit>(Str)
                    .FirstOrDefault<Unit>();

                PIPoint ptHAP = PIPoint.FindPIPoint(piServer, unit.HAPTagName);

                if (ptHAP != null)
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

                        var ptValue = ptHAP.RecordedValue(new AFTime(dateTime + ":" + min + ":00"), AFRetrievalMode.Exact);
                        if (ptValue.Value.ToString() == "No Data" || ptValue.Value.ToString() == "Pt Created")
                        {
                            val = null;
                        }
                        else
                        {
                            val = Convert.ToDouble(ptValue.Value);
                        }

                        sched.Add(new Schedule
                        {
                            Value = val,
                            Timestamp = Convert.ToDateTime(dateTime + ":" + min + ":00"),
                            TimestampLabel = i == 60 ? "60" : min
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

                        var ptValue = ptHAP.RecordedValue(new AFTime(dateTime + ":" + min + ":00"), AFRetrievalMode.Exact);
                        if (ptValue.Value.ToString() == "No Data" || ptValue.Value.ToString() == "Pt Created")
                        {
                            val = null;
                        }
                        else
                        {
                            val = Convert.ToDouble(ptValue.Value);
                        }

                        sched.Add(new Schedule
                        {
                            Value = val,
                            Timestamp = Convert.ToDateTime(dateTime + ":" + min + ":00"),
                            TimestampLabel = i == 60 ? "60" : min
                        });

                    }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, sched);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetUnitDAP(string UnitNumber)
        {
            List<Schedule> sched = new List<Schedule>();
            Unit unit = new Unit();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            Nullable<Double> val;
            DateTime currentDate = DateTime.Now;

            using (TradingEntities db = new TradingEntities())
            {
                string Str = $"select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '{UnitNumber}'";
                unit = db.Database
                    .SqlQuery<Unit>(Str)
                    .FirstOrDefault<Unit>();

                PIPoint ptDAP = PIPoint.FindPIPoint(piServer, unit.DAPTagName);

                if (ptDAP != null)
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

                            var ptValue = ptDAP.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            
                            if (ptValue.Value.ToString() == "No Data" || ptValue.Value.ToString() == "Pt Created")
                            {
                                val = null;
                            }
                            else
                            {
                                val = Convert.ToDouble(ptValue.Value);
                            }

                            sched.Add(new Schedule
                            {
                                Value = val,
                                Timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                                TimestampLabel = hr
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

                            var ptValue = ptDAP.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            
                            if (ptValue.Value.ToString() == "No Data" || ptValue.Value.ToString() == "Pt Created")
                            {
                                val = null;
                            }
                            else
                            {
                                val = Convert.ToDouble(ptValue.Value);
                            }

                            sched.Add(new Schedule
                            {
                                Value = val,
                                Timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                                TimestampLabel = hr == "00" || hr == "0" ? "24" : hr
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

                            var ptValue = ptDAP.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);
                            
                            if (ptValue.Value.ToString() == "No Data" || ptValue.Value.ToString() == "Pt Created")
                            {
                                val = null;
                            }
                            else
                            {
                                val = Convert.ToDouble(ptValue.Value);
                            }

                            sched.Add(new Schedule
                            {
                                Value = val,
                                Timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                                TimestampLabel = hr
                            });
                        }

                        var ptVal = ptDAP.RecordedValue(new AFTime(date.AddDays(1).ToShortDateString() + " "  + "00:00:00"), AFRetrievalMode.Exact);
                        if (ptVal.Value.ToString() == "No Data" || ptVal.Value.ToString() == "Pt Created")
                        {
                            val = null;
                        }
                        else
                        {
                            val = Convert.ToDouble(ptVal.Value);
                        }

                        sched.Add(new Schedule
                        {
                            Value = val,
                            Timestamp = Convert.ToDateTime(date.AddDays(1).ToShortDateString() + " "  + "00:00:00"),
                            TimestampLabel = "24"
                        });
                    }


                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, sched);
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult RTDValue([FromBody] PIValue Model)
        {
            try
            {
                using (TradingEntities db = new TradingEntities())
                {
                    string Str = $"select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '{Model.UnitNumber}'";
                    var unit = db.Database
                        .SqlQuery<Unit>(Str)
                        .FirstOrDefault<Unit>();

                    PIServers piServers = new PIServers();
                    PIServer piServer = piServers[GlobalFunctions.piServerName];
                    IList<AFValue> valuesToWrite = new List<AFValue>();

                    PIPoint pt = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                    AFTime time = new AFTime(Model.Timestamp);
                    AFValue afValue = new AFValue(Model.Value, time);
                    afValue.PIPoint = pt;
                    valuesToWrite.Add(afValue);

                    piServer.UpdateValues(valuesToWrite, AFUpdateOption.Replace, AFBufferOption.BufferIfPossible);
                }
                
            }
            catch (Exception e)
            {
                return Content(HttpStatusCode.BadRequest, new { message = e.Message});
            }

            return Content(HttpStatusCode.OK, "Value successfully saved.");

        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetOverrideValue(string UnitNumber)
        {
            Override overrideValue = new Override();
            using (TradingEntities db = new TradingEntities())
            {
                string query = $"exec[sp_OverrideValue] '{UnitNumber}'";
                overrideValue = db.Database
                    .SqlQuery<Override>(query)
                    .FirstOrDefault<Override>();
            }
            return Request.CreateResponse(HttpStatusCode.OK, overrideValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetMOTValue(string UnitNumber)
        {
            Override overrideValue = new Override();
            using (TradingEntities db = new TradingEntities())
            {
                string query = $"exec[sp_MOT] '{UnitNumber}'";
                overrideValue = db.Database
                    .SqlQuery<Override>(query)
                    .FirstOrDefault<Override>();
            }
            return Request.CreateResponse(HttpStatusCode.OK, overrideValue);
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult SaveOverrideValue([FromBody] Override Model)
        {
            using (TradingEntities db = new TradingEntities())
            {
                string query = string.Empty;

                if (Model.IsReserve == true)
                {
                    query = $"exec[sp_SaveOverrideValue] " +
                    $"'{Model.UnitNumber}'," +
                    $"'{Model.Value ?? 0}'," +
                    $"'{Model.IsUse ?? false}'," +
                    $"{Model.IsReserve ?? false}," +
                    $"{Model.FrValue ?? 0}," +
                    $"{Model.RuValue ?? 0}," +
                    $"{Model.RdValue ?? 0}," +
                    $"{Model.DrValue ?? 0}," +
                    $"{Model.FrIsUse ?? false}," +
                    $"{Model.RuIsUse ?? false}," +
                    $"{Model.RdIsUse ?? false}," +
                    $"{Model.DrIsUse ?? false}";
                }
                else
                {
                    query = $"exec[sp_SaveOverrideValue] " +
                    $"'{Model.UnitNumber}'," +
                    $"'{Model.Value}'," +
                    $"'{Model.IsUse}'," +
                    $"{Model.IsReserve}";
                }
                
                db.Database.ExecuteSqlCommand(query);
            }
            return Content(HttpStatusCode.OK, "Value successfully saved.");
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult SaveMOTValue([FromBody] Override Model)
        {
            using (TradingEntities db = new TradingEntities())
            {
                string query = $"exec [sp_SaveMOTValue] '{Model.UnitNumber}','{Model.Value}','{Model.IsUse}'";
                db.Database.ExecuteSqlCommand(query);
            }
            return Content(HttpStatusCode.OK, "Value successfully saved.");
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetDemand(string UnitNumber = null)
        {
            List<PIValue> piValue = new List<PIValue>();
            List<PIValue> piLuzValue = new List<PIValue>();
            List<PIValue> piVisValue = new List<PIValue>();
            List<PIValue> piMinValue = new List<PIValue>();
            List<PIValue> piPriceValue = new List<PIValue>();
            List<Demands> demands = new List<Demands>();
            DateTime date = DateTime.Now;
            string PriceUnitNumber;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            int minMod = 0;
            Nullable<double> luzVal, visVal, minVal,priceVal;

            if (UnitNumber == null || UnitNumber == "")
            {
                PriceUnitNumber = "";
            }
            else
            {
                PriceUnitNumber = "DAPEG_5MINS_" + UnitNumber + "_P.MV";
            }
            
            PIPoint ptLuz = PIPoint.FindPIPoint(piServer, "DEMAND_DAP_5MINS_CLUZ_MW.MV");
            PIPoint ptVis = PIPoint.FindPIPoint(piServer, "DEMAND_DAP_5MINS_CVIS_MW.MV");
            PIPoint ptMin = PIPoint.FindPIPoint(piServer, "DEMAND_DAP_5MINS_CMIN_MW.MV");

            PIPoint ptLuzRTD = PIPoint.FindPIPoint(piServer, "DEMAND_5MINS_CLUZ_MW.MV");
            PIPoint ptVisRTD = PIPoint.FindPIPoint(piServer, "DEMAND_5MINS_CVIS_MW.MV");
            PIPoint ptMinRTD = PIPoint.FindPIPoint(piServer, "DEMAND_5MINS_CMIN_MW.MV");

            minMod = 5 - ((DateTime.Now.Minute) % 5);

            var ptLuzValue = ptLuz.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.AtOrBefore);
            var ptVisValue = ptVis.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.AtOrBefore);
            var ptMinValue = ptMin.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.AtOrBefore);

            var ptLuzValueRTD = ptLuzRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.AtOrBefore);
            var ptVisValueRTD = ptVisRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.AtOrBefore);
            var ptMinValueRTD = ptMinRTD.RecordedValue(new AFTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")), AFRetrievalMode.AtOrBefore);


            //Current Values
            if (ptLuzValueRTD.Value.ToString() == "No Data" || ptLuzValueRTD.Value.ToString() == "Pt Created")
            {
                luzVal = null;
            }
            else
            {
                luzVal = Convert.ToDouble(ptLuzValueRTD.Value);
            }

            piValue.Add(new PIValue {
                Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                Value = luzVal,
                UnitNumber = "Luz"
            });

            if (ptVisValueRTD.Value.ToString() == "No Data" || ptVisValueRTD.Value.ToString() == "Pt Created")
            {
                visVal = null;
            }
            else
            {
                visVal = Convert.ToDouble(ptVisValueRTD.Value);
            }

            piValue.Add(new PIValue
            {
                Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                Value = visVal,
                UnitNumber = "Vis"
            });

            if (ptMinValueRTD.Value.ToString() == "No Data" || ptMinValueRTD.Value.ToString() == "Pt Created")
            {
                minVal = null;
            }
            else
            {
                minVal = Convert.ToDouble(ptMinValueRTD.Value);
            }

            piValue.Add(new PIValue
            {
                Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                Value = minVal,
                UnitNumber = "Min"
            });

            demands.Add(new Demands {
                Description = "Current",
                PIValue = piValue
            });

            for (int i = 1; i <= 24; i = i + 1)
            {
                string dt = "";
                if (i == 24)
                {
                    dt = date.AddDays(1).ToShortDateString() + " 00:00:00";
                }
                else
                {
                    dt = date.ToShortDateString() + " " + i + ":00:00";
                }
                

                ptLuzValue = ptLuz.RecordedValue(new AFTime(dt), AFRetrievalMode.Exact);
                if (ptLuzValue.Value.ToString() == "No Data" || ptLuzValue.Value.ToString() == "Pt Created")
                {
                    luzVal = null;
                }
                else
                {
                    luzVal = Convert.ToDouble(ptLuzValue.Value);
                }

                ptVisValue = ptVis.RecordedValue(new AFTime(dt), AFRetrievalMode.Exact);
                if (ptVisValue.Value.ToString() == "No Data" || ptVisValue.Value.ToString() == "Pt Created")
                {
                    visVal = null;
                }
                else
                {
                    visVal = Convert.ToDouble(ptVisValue.Value);
                }

                ptMinValue = ptMin.RecordedValue(new AFTime(dt), AFRetrievalMode.Exact);
                if (ptMinValue.Value.ToString() == "No Data" || ptMinValue.Value.ToString() == "Pt Created")
                {
                    minVal = null;
                }
                else
                {
                    minVal = Convert.ToDouble(ptMinValue.Value);
                }

               

                piLuzValue.Add(new PIValue
                {
                    Value = luzVal,
                    Timestamp = Convert.ToDateTime(dt),
                    TimestampLabel = i.ToString()
                });

                piVisValue.Add(new PIValue
                {
                    Value = visVal,
                    Timestamp = Convert.ToDateTime(dt),
                    TimestampLabel = i.ToString()
                });

                piMinValue.Add(new PIValue
                {
                    Value = minVal,
                    Timestamp = Convert.ToDateTime(dt),
                    TimestampLabel = i.ToString()
                });


            }

            demands.Add(new Demands
            {
                Description = "Luz",
                PIValue = piLuzValue
            });

            demands.Add(new Demands
            {
                Description = "Vis",
                PIValue = piVisValue
            });

            demands.Add(new Demands
            {
                Description = "Min",
                PIValue = piMinValue
            });

            //Price
            if (PriceUnitNumber != "")
            {
                PIPoint ptPrice = PIPoint.FindPIPoint(piServer, PriceUnitNumber);
                for (int i = 1; i <= 24; i = i + 1)
                {
                    string dt = "";
                    if (i == 24)
                    {
                        dt = date.AddDays(1).ToShortDateString() + " 00:00:00";
                    }
                    else
                    {
                        dt = date.ToShortDateString() + " " + i + ":00:00";
                    }
                    var ptPriceValue = ptPrice.RecordedValue(new AFTime(dt), AFRetrievalMode.Exact);
                    if (ptPriceValue.Value.ToString() == "No Data" || ptPriceValue.Value.ToString() == "Pt Created")
                    {
                        priceVal = null;
                    }
                    else
                    {
                        priceVal = Convert.ToDouble(ptPriceValue.Value);
                    }

                    piPriceValue.Add(new PIValue
                    {
                        Value = priceVal,
                        Timestamp = Convert.ToDateTime(dt),
                        TimestampLabel = i.ToString()
                    });
                }
            }

            demands.Add(new Demands
            {
                Description = "Price",
                PIValue = piPriceValue
            });

            return Request.CreateResponse(HttpStatusCode.OK, demands);
        }

        public Nullable<double> NullOrValue(PIPoint pt, string dt)
        {
            var ptValue = pt.RecordedValue(new AFTime(dt), AFRetrievalMode.Exact);
            if (ptValue.Value.ToString() == "No Data" || ptValue.Value.ToString() == "Pt Created")
            {
                return null;
            }
            else
            {
                return Convert.ToDouble(ptValue.Value);
            }
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetSystemDemand(string UnitNumber = null)
        {
            List<List<Schedule>> scheds = new List<List<Schedule>>();
            List<Schedule> luzScheds = new List<Schedule>();
            List<Schedule> visScheds = new List<Schedule>();
            List<Schedule> minScheds = new List<Schedule>();
            List<Schedule> priceScheds = new List<Schedule>();

            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            PIPoint ptLuz = PIPoint.FindPIPoint(piServer, "DEMAND_DAP_5MINS_CLUZ_MW.MV");
            PIPoint ptVis = PIPoint.FindPIPoint(piServer, "DEMAND_DAP_5MINS_CVIS_MW.MV");
            PIPoint ptMin = PIPoint.FindPIPoint(piServer, "DEMAND_DAP_5MINS_CMIN_MW.MV");

            PIPoint ptLuzRTD = PIPoint.FindPIPoint(piServer, "DEMAND_5MINS_CLUZ_MW.MV");
            PIPoint ptVisRTD = PIPoint.FindPIPoint(piServer, "DEMAND_5MINS_CVIS_MW.MV");
            PIPoint ptMinRTD = PIPoint.FindPIPoint(piServer, "DEMAND_5MINS_CMIN_MW.MV");

            bool showPrice = false;
            PIPoint ptPrice;
            PIPoint ptPriceRTD;
            if (UnitNumber == null || UnitNumber == "")
            {
                // PriceUnitNumber = "";
                showPrice = false;
                ptPrice = null;
                ptPriceRTD = null;
            }
            else
            {
                showPrice = true;
                ptPrice = PIPoint.FindPIPoint(piServer, "HAPEG_5MINS_" + UnitNumber + "_P.MV");
                ptPriceRTD = PIPoint.FindPIPoint(piServer, "RTDEG_5MINS_" + UnitNumber + "_P.MV");
            }

            var dtNow = DateTime.Now;
            var currInterval = dtNow.AddMinutes(5 - (dtNow.Minute % 5));

            // 4 rows of HAP
            for(var i = 0; i < 4; i++)
            {
                string targetDt = currInterval.AddMinutes(5 * (4 - i)).ToString("MM/d/yyyy HH:mm:00");
                luzScheds.Add(new Schedule
                {
                    Value = NullOrValue(ptLuz, targetDt),
                    Actual = 0,
                    IsLimit = false,
                    Timestamp = Convert.ToDateTime(targetDt),
                    TimestampLabel = Convert.ToDateTime(targetDt).ToString("HH:mm")
                });
                visScheds.Add(new Schedule
                {
                    Value = NullOrValue(ptVis, targetDt),
                    Actual = 0,
                    IsLimit = false,
                    Timestamp = Convert.ToDateTime(targetDt),
                    TimestampLabel = Convert.ToDateTime(targetDt).ToString("HH:mm")
                });
                minScheds.Add(new Schedule
                {
                    Value = NullOrValue(ptMin, targetDt),
                    Actual = 0,
                    IsLimit = false,
                    Timestamp = Convert.ToDateTime(targetDt),
                    TimestampLabel = Convert.ToDateTime(targetDt).ToString("HH:mm")
                });
                priceScheds.Add(new Schedule
                {
                    Value = (showPrice) ? NullOrValue(ptPrice, targetDt) : null,
                    Actual = 0,
                    IsLimit = false,
                    Timestamp = Convert.ToDateTime(targetDt),
                    TimestampLabel = Convert.ToDateTime(targetDt).ToString("HH:mm")
                });
            }
            
            // 1 row of RTD
            string rtdDt = currInterval.ToString("MM/d/yyyy HH:mm:00");
            luzScheds.Add(new Schedule
            {
                Value = NullOrValue(ptLuzRTD, rtdDt),
                Actual = 0,
                IsLimit = false,
                Timestamp = Convert.ToDateTime(rtdDt),
                TimestampLabel = Convert.ToDateTime(rtdDt).ToString("HH:mm")
            });
            visScheds.Add(new Schedule
            {
                Value = NullOrValue(ptVisRTD, rtdDt),
                Actual = 0,
                IsLimit = false,
                Timestamp = Convert.ToDateTime(rtdDt),
                TimestampLabel = Convert.ToDateTime(rtdDt).ToString("HH:mm")
            });
            minScheds.Add(new Schedule
            {
                Value = NullOrValue(ptMinRTD, rtdDt),
                Actual = 0,
                IsLimit = false,
                Timestamp = Convert.ToDateTime(rtdDt),
                TimestampLabel = Convert.ToDateTime(rtdDt).ToString("HH:mm")
            });
            priceScheds.Add(new Schedule
            {
                Value = (showPrice) ? NullOrValue(ptPriceRTD, rtdDt) : null,
                Actual = 0,
                IsLimit = false,
                Timestamp = Convert.ToDateTime(rtdDt),
                TimestampLabel = Convert.ToDateTime(rtdDt).ToString("HH:mm")
            });

            // 10 rows of Compliance
            for (var i = 0; i < 10; i++)
            {
                string targetDt = currInterval.AddMinutes(-(5 * (i + 1))).ToString("MM/d/yyyy HH:mm:00");
                luzScheds.Add(new Schedule
                {
                    Value = NullOrValue(ptLuzRTD, targetDt),
                    Actual = 0,
                    IsLimit = false,
                    Timestamp = Convert.ToDateTime(targetDt),
                    TimestampLabel = Convert.ToDateTime(targetDt).ToString("HH:mm")
                });
                visScheds.Add(new Schedule
                {
                    Value = NullOrValue(ptVisRTD, targetDt),
                    Actual = 0,
                    IsLimit = false,
                    Timestamp = Convert.ToDateTime(targetDt),
                    TimestampLabel = Convert.ToDateTime(targetDt).ToString("HH:mm")
                });
                minScheds.Add(new Schedule
                {
                    Value = NullOrValue(ptMinRTD, targetDt),
                    Actual = 0,
                    IsLimit = false,
                    Timestamp = Convert.ToDateTime(targetDt),
                    TimestampLabel = Convert.ToDateTime(targetDt).ToString("HH:mm")
                });
                priceScheds.Add(new Schedule
                {
                    Value = (showPrice) ? NullOrValue(ptPriceRTD, targetDt) : null,
                    Actual = 0,
                    IsLimit = false,
                    Timestamp = Convert.ToDateTime(targetDt),
                    TimestampLabel = Convert.ToDateTime(targetDt).ToString("HH:mm")
                });
            }

            scheds.Add(luzScheds);
            scheds.Add(visScheds);
            scheds.Add(minScheds);
            scheds.Add(priceScheds);

            return Request.CreateResponse(HttpStatusCode.OK, scheds);
        }


        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetImportExport()
        {

            List<PIValue> piLuz = new List<PIValue>();
            List<PIValue> piVis = new List<PIValue>();
            List<PIValue> piMin = new List<PIValue>();

            List<Demands> summary = new List<Demands>();

            DateTime date = DateTime.Now;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            PIPoint ptLuzImport = PIPoint.FindPIPoint(piServer, "IMPORT_5MINS_CLUZ_MW.MV");
            PIPoint ptVisImport = PIPoint.FindPIPoint(piServer, "IMPORT_5MINS_CVIS_MW.MV");
            PIPoint ptMinImport = PIPoint.FindPIPoint(piServer, "IMPORT_5MINS_CMIN_MW.MV");
            PIPoint ptLuzExport = PIPoint.FindPIPoint(piServer, "EXPORT_5MINS_CLUZ_MW.MV");
            PIPoint ptVisExport = PIPoint.FindPIPoint(piServer, "EXPORT_5MINS_CVIS_MW.MV");
            PIPoint ptMinExport = PIPoint.FindPIPoint(piServer, "EXPORT_5MINS_CMIN_MW.MV");

            PIPoint ptLuzImportDAP = PIPoint.FindPIPoint(piServer, "IMPORT_DAP_CLUZ_MW.MV");
            PIPoint ptVisImportDAP = PIPoint.FindPIPoint(piServer, "IMPORT_DAP_CVIS_MW.MV");
            PIPoint ptMinImportDAP = PIPoint.FindPIPoint(piServer, "IMPORT_DAP_CMIN_MW.MV");
            PIPoint ptLuzExportDAP = PIPoint.FindPIPoint(piServer, "EXPORT_DAP_CLUZ_MW.MV");
            PIPoint ptVisExportDAP = PIPoint.FindPIPoint(piServer, "EXPORT_DAP_CVIS_MW.MV");
            PIPoint ptMinExportDAP = PIPoint.FindPIPoint(piServer, "EXPORT_DAP_CMIN_MW.MV");


            for (int i = 1; i <= 24; i = i + 1)
            {
                string dt = "";
                double luzImport = 0;
                double visImport = 0;
                double minImport = 0;
                double luzExport = 0;
                double visExport = 0;
                double minExport = 0;

                if (i == 24)
                {
                    dt = date.AddDays(1).ToShortDateString() + " 00:00:00";
                }
                else
                {
                    dt = date.ToShortDateString() + " " + i + ":00:00";
                }


                if (i - 1 <= date.Hour)
                {
                    // Import
                    luzImport = GetPIValue(ptLuzImport, dt);
                    visImport = GetPIValue(ptVisImport, dt);
                    minImport = GetPIValue(ptMinImport, dt);

                    // Export
                    luzExport = GetPIValue(ptLuzExport, dt);
                    visExport = GetPIValue(ptVisExport, dt);
                    minExport = GetPIValue(ptMinExport, dt);
                }
                else
                {
                    // Import
                    luzImport = GetPIValue(ptLuzImportDAP, dt);
                    visImport = GetPIValue(ptVisImportDAP, dt);
                    minImport = GetPIValue(ptMinImportDAP, dt);

                    // Export
                    luzExport = GetPIValue(ptLuzExportDAP, dt);
                    visExport = GetPIValue(ptVisExportDAP, dt);
                    minExport = GetPIValue(ptMinExportDAP, dt);
                }

                piLuz.Add(new PIValue
                {
                    Value = luzImport - luzExport,
                    Timestamp = Convert.ToDateTime(dt),
                    TimestampLabel = i.ToString()
                });
                piVis.Add(new PIValue
                {
                    Value = visImport - visExport,
                    Timestamp = Convert.ToDateTime(dt),
                    TimestampLabel = i.ToString()
                });
                piMin.Add(new PIValue
                {
                    Value = minImport - minExport,
                    Timestamp = Convert.ToDateTime(dt),
                    TimestampLabel = i.ToString()
                });
            }

            summary.Add(new Demands
            {
                Description = "Luz",
                PIValue = piLuz
            });

            summary.Add(new Demands
            {
                Description = "Vis",
                PIValue = piVis
            });

            summary.Add(new Demands
            {
                Description = "Min",
                PIValue = piMin
            });

            return Request.CreateResponse(HttpStatusCode.OK, summary);
        }

        private double GetPIValue(PIPoint piPnt, string dt)
        {
            double ImpExpValues = 0;
            double cnt = 0;
            double avg = 0;
            var RegionAFValues = piPnt
                    .RecordedValues(new AFTimeRange(Convert.ToDateTime(dt).AddHours(-1).ToString(),
                    Convert.ToDateTime(dt).AddMinutes(-5).ToString()),
                    AFBoundaryType.Inside, null, false);

            foreach (var val in RegionAFValues)
            {
                if (val.Value.ToString() != "No Data" && val.Value.ToString() != "Pt Created")
                {
                    ImpExpValues += Convert.ToDouble(val.Value);
                }
                cnt++;
            }

            avg = ImpExpValues / cnt;
            return avg;
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetImportExport2()
        {
            List<PIValue> piLuzVis = new List<PIValue>();
            List<PIValue> piVisLuz = new List<PIValue>();
            List<PIValue> pitMinVis = new List<PIValue>();
            List<PIValue> pitVisMin = new List<PIValue>();

            List<Demands> summary = new List<Demands>();

            DateTime date = DateTime.Now;
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            

            PIPoint ptDAPLuzImport = PIPoint.FindPIPoint(piServer, "IMPORT_DAP_CLUZ_MW.MV");
            PIPoint ptDAPMinImport = PIPoint.FindPIPoint(piServer, "IMPORT_DAP_CMIN_MW.MV");
            PIPoint ptLuzImport = PIPoint.FindPIPoint(piServer, "IMPORT_5MINS_CLUZ_MW.MV");
            PIPoint ptMinImport = PIPoint.FindPIPoint(piServer, "IMPORT_5MINS_CMIN_MW.MV");

            PIPoint ptDAPLuzExport = PIPoint.FindPIPoint(piServer, "EXPORT_DAP_CLUZ_MW.MV");
            PIPoint ptDAPMinExport = PIPoint.FindPIPoint(piServer, "EXPORT_DAP_CMIN_MW.MV");
            PIPoint ptLuzExport = PIPoint.FindPIPoint(piServer, "EXPORT_5MINS_CLUZ_MW.MV");
            PIPoint ptMinExport = PIPoint.FindPIPoint(piServer, "EXPORT_5MINS_CMIN_MW.MV");

            for (int i = 1; i <= 24; i = i + 1)
            {
                string dt = "";
                double luzImport = 0;
                double minImport = 0;
                double luzExport = 0;
                double minExport = 0;
                double totalLuz = 0;
                double totalMin = 0;

                if (i == 24)
                {
                    dt = date.AddDays(1).ToShortDateString() + " 00:00:00";
                }
                else
                {
                    dt = date.ToShortDateString() + " " + i + ":00:00";
                }


                if(i-1 <= date.Hour)
                {
                    
                    /// Import
                    // Luzon
                    var luzImportValues = ptLuzImport
                        .RecordedValues(new AFTimeRange(Convert.ToDateTime(dt).AddHours(-1).ToString(),
                        Convert.ToDateTime(dt).AddMinutes(-5).ToString()),
                        AFBoundaryType.Inside, null, false);

                    foreach (var val in luzImportValues)
                    {
                        if (val.Value.ToString() != "No Data" && val.Value.ToString() != "Pt Created")
                        {
                            luzImport += Convert.ToDouble(val.Value);
                        }
                    }

                    //Mindanao
                    var minImportValues = ptMinImport
                        .RecordedValues(new AFTimeRange(Convert.ToDateTime(dt).AddHours(-1).ToString(),
                        Convert.ToDateTime(dt).AddMinutes(-5).ToString()),
                        AFBoundaryType.Inside, null, false);

                    foreach (var val in minImportValues)
                    {
                        if (val.Value.ToString() != "No Data" && val.Value.ToString() != "Pt Created")
                        {
                            minImport += Convert.ToDouble(val.Value);
                        }
                    }


                    /// Export
                    /// Luzon
                    var luzExportValues = ptLuzExport
                    .RecordedValues(new AFTimeRange(Convert.ToDateTime(dt).AddHours(-1).ToString(),
                    Convert.ToDateTime(dt).AddMinutes(-5).ToString()),
                    AFBoundaryType.Inside, null, false);

                    foreach (var val in luzExportValues)
                    {
                        if (val.Value.ToString() != "No Data" && val.Value.ToString() != "Pt Created")
                        {
                            luzExport += Convert.ToDouble(val.Value);
                        }
                    }

                    /// Mindanao
                    var minExportValues = ptMinExport
                    .RecordedValues(new AFTimeRange(Convert.ToDateTime(dt).AddHours(-1).ToString(),
                    Convert.ToDateTime(dt).AddMinutes(-5).ToString()),
                    AFBoundaryType.Inside, null, false);

                    foreach (var val in minExportValues)
                    {
                        if (val.Value.ToString() != "No Data" && val.Value.ToString() != "Pt Created")
                        {
                            minExport += Convert.ToDouble(val.Value);
                        }
                    }

                }
                else
                {
                    /// Import
                    // Luzon
                    var luzImportValues = ptDAPLuzImport
                        .RecordedValues(new AFTimeRange(Convert.ToDateTime(dt).AddHours(-1).ToString(),
                        Convert.ToDateTime(dt).AddMinutes(-5).ToString()),
                        AFBoundaryType.Inside, null, false);

                    foreach (var val in luzImportValues)
                    {
                        if (val.Value.ToString() != "No Data" && val.Value.ToString() != "Pt Created")
                        {
                            luzImport += Convert.ToDouble(val.Value);
                        }
                    }

                    //Mindanao
                    var minImportValues = ptDAPMinImport
                        .RecordedValues(new AFTimeRange(Convert.ToDateTime(dt).AddHours(-1).ToString(),
                        Convert.ToDateTime(dt).AddMinutes(-5).ToString()),
                        AFBoundaryType.Inside, null, false);

                    foreach (var val in minImportValues)
                    {
                        if (val.Value.ToString() != "No Data" && val.Value.ToString() != "Pt Created")
                        {
                            minImport += Convert.ToDouble(val.Value);
                        }
                    }


                    /// Export
                    /// Luzon
                    var luzExportValues = ptDAPLuzExport
                    .RecordedValues(new AFTimeRange(Convert.ToDateTime(dt).AddHours(-1).ToString(),
                    Convert.ToDateTime(dt).AddMinutes(-5).ToString()),
                    AFBoundaryType.Inside, null, false);

                    foreach (var val in luzExportValues)
                    {
                        if (val.Value.ToString() != "No Data" && val.Value.ToString() != "Pt Created")
                        {
                            luzExport += Convert.ToDouble(val.Value);
                        }
                    }

                    /// Mindanao
                    var minExportValues = ptDAPMinExport
                    .RecordedValues(new AFTimeRange(Convert.ToDateTime(dt).AddHours(-1).ToString(),
                    Convert.ToDateTime(dt).AddMinutes(-5).ToString()),
                    AFBoundaryType.Inside, null, false);

                    foreach (var val in minExportValues)
                    {
                        if (val.Value.ToString() != "No Data" && val.Value.ToString() != "Pt Created")
                        {
                            minExport += Convert.ToDouble(val.Value);
                        }
                    }
                }

                piLuzVis.Add(new PIValue
                {
                    Value = luzExport * -1,
                    Timestamp = Convert.ToDateTime(dt),
                    TimestampLabel = i.ToString()
                });

                piVisLuz.Add(new PIValue
                {
                    Value = luzImport,
                    Timestamp = Convert.ToDateTime(dt),
                    TimestampLabel = i.ToString()
                });


                pitMinVis.Add(new PIValue
                {
                    Value = minExport * -1,
                    Timestamp = Convert.ToDateTime(dt),
                    TimestampLabel = i.ToString()
                });

                pitVisMin.Add(new PIValue
                {
                    Value = minImport,
                    Timestamp = Convert.ToDateTime(dt),
                    TimestampLabel = i.ToString()
                });

                /** 

                totalLuz = luzImport - luzExport;
                totalMin = minImport - minExport;

                if (totalLuz < 0)
                {
                    piLuzVis.Add(new PIValue
                    {
                        Value = totalLuz,
                        Timestamp = Convert.ToDateTime(dt),
                        TimestampLabel = i.ToString()
                    });

                    piVisLuz.Add(new PIValue
                    {
                        Value = null,
                        Timestamp = Convert.ToDateTime(dt),
                        TimestampLabel = i.ToString()
                    });

                }
                else
                {
                    piLuzVis.Add(new PIValue
                    {
                        Value = null,
                        Timestamp = Convert.ToDateTime(dt),
                        TimestampLabel = i.ToString()
                    });

                    piVisLuz.Add(new PIValue
                    {
                        Value = totalLuz,
                        Timestamp = Convert.ToDateTime(dt),
                        TimestampLabel = i.ToString()
                    });
                }

               
                if(totalMin < 0)
                {
                    pitMinVis.Add(new PIValue
                    {
                        Value = totalMin,
                        Timestamp = Convert.ToDateTime(dt),
                        TimestampLabel = i.ToString()
                    });

                    pitVisMin.Add(new PIValue
                    {
                        Value = null,
                        Timestamp = Convert.ToDateTime(dt),
                        TimestampLabel = i.ToString()
                    });
                }
                else
                {
                    pitMinVis.Add(new PIValue
                    {
                        Value = null,
                        Timestamp = Convert.ToDateTime(dt),
                        TimestampLabel = i.ToString()
                    });

                    pitVisMin.Add(new PIValue
                    {
                        Value = totalMin,
                        Timestamp = Convert.ToDateTime(dt),
                        TimestampLabel = i.ToString()
                    });
                } **/

            }

            summary.Add(new Demands
            {
                Description = "Luz-Vis",
                PIValue = piLuzVis
            });

            summary.Add(new Demands
            {
                Description = "Vis-Luz",
                PIValue = piVisLuz
            });

            summary.Add(new Demands
            {
                Description = "Min-Vis",
                PIValue = pitMinVis
            });

            summary.Add(new Demands
            {
                Description = "Vis-Min",
                PIValue = pitVisMin
            });

            return Request.CreateResponse(HttpStatusCode.OK, summary);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetUnitPerRegion()
        {
            var Luz = GetRegionPIValue("Luzon");
            var Vis = GetRegionPIValue("Vizayas");
            var Min = GetRegionPIValue("Mindanao");
            return Request.CreateResponse(HttpStatusCode.OK, new { Luz = Luz, Vis = Vis, Min = Min });
        }

        public List<RegionPIValue> GetRegionPIValue(string Region)
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

                    var ptRTDValue = ptRtd.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + 5)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + 5)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    var ptPriceValue = ptPrice.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + 5)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + 5)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
                    var ptActualValue = ptActual.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + 5)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + 5)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
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
                    catch(Exception ex)
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
                        if(rtd == 0)
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
                        Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + 5)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + 5)).ToString("HH:mm:00")),
                        TimestampLabel = DateTime.Now.AddMinutes(-(minMod + 5)).ToString("HH:mm")
                    });
                }
            }

            return regionPIValue;
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetUnitPerAccess(int permissionID)
        {
            List<Unit> unitList = new List<Unit>();
            using (TradingEntities db = new TradingEntities())
            {
                string Str = $@"exec sp_UnitPerAccess {permissionID}";
                unitList = db.Database.SqlQuery<Unit>(Str).OrderBy(x=>x.UnitNumber).ToList<Unit>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, unitList);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetRemarks(string UnitNumber, string Timestamp)
        {
            List<ScheduleString> sched = new List<ScheduleString>();
            string remarks = "";
            Nullable<Double> actual = null;
            Nullable<Double> rtd = null;
            Unit unit = new Unit();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            GenRemarks genRemarks = new GenRemarks();
            
            using (TradingEntities db = new TradingEntities())
            {
                string query = $"select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '{UnitNumber}'";
                unit = db.Database
                    .SqlQuery<Unit>(query)
                    .FirstOrDefault<Unit>();

                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                PIPoint ptRemarks = PIPoint.FindPIPoint(piServer, unit.ComplianceTagName);
                PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);

                //get remarks
                try
                {
                    query = $"exec [sp_GeneralComment] '{UnitNumber}'";
                    genRemarks = db.Database.SqlQuery<GenRemarks>(query).FirstOrDefault<GenRemarks>();
                }
                catch
                {
                    genRemarks.ID = 1;
                    genRemarks.UnitNumber = UnitNumber;
                    genRemarks.Value = "";
                }

                for (int x =5; x<= 60; x+=5)
                {
                    string dt = "";
                    string timelb = "";
                    try
                    {
                        if (x == 60)
                        {
                            dt = Convert.ToDateTime(Timestamp + ":00:00").AddHours(1).ToString("MM/dd/yyyy HH") + ":00";
                        }
                        else
                        {
                            dt = Timestamp + ":" + x;
                        }
                        timelb = Convert.ToDateTime(dt).ToString("HH:mm");
                    }
                    catch(Exception ex)
                    {
                        dt = Timestamp + ":00";
                        timelb = ex.Message + " " + Timestamp;
                    }
                    

                    
                    Nullable<bool> isLimit;
                    var ptRTDValue = ptRTD.RecordedValue(new AFTime(dt), AFRetrievalMode.Exact);
                    if (ptRTDValue.Value.ToString() == "No Data" || ptRTDValue.Value.ToString() == "Pt Created")
                    {
                        rtd = null;
                    }
                    else
                    {
                        rtd = Convert.ToDouble(ptRTDValue.Value);
                    }

                    var ptActualValue = ptActual.RecordedValue(new AFTime(dt), AFRetrievalMode.Exact);
                    if (ptActualValue.Value.ToString() == "No Data" || ptActualValue.Value.ToString() == "Pt Created")
                    {
                        actual = null;
                    }
                    else
                    {
                        actual = Convert.ToDouble(ptActualValue.Value);
                    }

                    var ptRemarksValue = ptRemarks.RecordedValue(new AFTime(dt), AFRetrievalMode.Exact);
                    if (ptRemarksValue.Value.ToString() == "No Data" || ptRemarksValue.Value.ToString() == "Pt Created")
                    {
                        remarks = null;
                    }
                    else
                    {
                        remarks = ptRemarksValue.Value.ToString();
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

                    sched.Add(new ScheduleString
                    {
                        Value = remarks,
                        Actual = actual,
                        IsLimit = isLimit,
                        Timestamp = Convert.ToDateTime(dt),
                        TimestampLabel = timelb
                    });
                }
                
            }
            return Request.CreateResponse(HttpStatusCode.OK, new { sched = sched , genRemarks = genRemarks });
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult SaveRemarks([FromBody] StringValue Model)
        {
            Unit unit = new Unit();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            using (TradingEntities db = new TradingEntities())
            {
                string Str = $"select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '{Model.UnitNumber}'";
                unit = db.Database
                    .SqlQuery<Unit>(Str)
                    .FirstOrDefault<Unit>();

                IList<AFValue> valuesToWrite = new List<AFValue>();

                PIPoint pt = PIPoint.FindPIPoint(piServer, unit.ComplianceTagName);
                AFTime time = new AFTime(Model.Timestamp);
                AFValue afValue = new AFValue(Model.Value, time);
                afValue.PIPoint = pt;
                valuesToWrite.Add(afValue);

                piServer.UpdateValues(valuesToWrite, AFUpdateOption.Replace, AFBufferOption.BufferIfPossible);
            }
            return Content(HttpStatusCode.OK, "Value successfully saved.");
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult SaveGeneralRemarks([FromBody] StringValue Model)
        {
            using (TradingEntities db = new TradingEntities())
            {
                string query = $"exec[sp_SaveGeneralComment] '{Model.UnitNumber}','{Model.Value}'";
                db.Database.ExecuteSqlCommand(query);
            }
            return Content(HttpStatusCode.OK, "Value successfully saved.");
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAllUnitDAPOld()
        {
            List<AllUnitPIValue> allUnitPIValue = new List<AllUnitPIValue>();
            using (TradingEntities db = new TradingEntities())
            {
                string query = $"exec [sp_AllUnit]";
                var data = db.Database
                    .SqlQuery<UnitPerRegionDB>(query)
                    .ToList<UnitPerRegionDB>();

                foreach (var item in data)
                {
                    allUnitPIValue.Add(new AllUnitPIValue { 
                    UnitNumber = item.UnitNumber,
                    BGColor = item.BColor,
                    PIValue = GetAllUnitPIValue(item.DAPTagName,item.UnitNumber)
                    });
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, allUnitPIValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAllUnitDAP()
        {
            List<AllUnitPIValue> allUnitPIValue = new List<AllUnitPIValue>();
            List<AllUnitPIValue> sumUnitPIValue = new List<AllUnitPIValue>();
            List<PIValue> listPIValue = new List<PIValue>();
            //Double val1, val2, val3, val4, val5, val6, val7, val8, val9, val10, val11, val12,
            //    val13, val14, val15, val16, val17, val18, val19;
            using (TradingEntities db = new TradingEntities())
            {
                string query = $"exec [sp_AllUnit]";
                var data = db.Database
                    .SqlQuery<UnitPerRegionDB>(query)
                    .ToList<UnitPerRegionDB>();

                foreach (var item in data)
                {
                    allUnitPIValue.Add(new AllUnitPIValue
                    {
                        UnitNumber = item.UnitNumber,
                        BGColor = item.BColor,
                        PIValue = GetAllUnitPIValue(item.DAPTagName, item.UnitNumber)
                    });
                }

                foreach(var item in allUnitPIValue)
                {
                    foreach(var piItem in item.PIValue)
                    {
                        var check = listPIValue.Where(x => x.TimestampLabel == piItem.TimestampLabel).FirstOrDefault();
                        if (check == null)
                        {
                            listPIValue.Add(new PIValue
                            {
                                Value = piItem.Value,
                                Timestamp = piItem.Timestamp,
                                TimestampLabel = piItem.TimestampLabel,
                                UnitNumber = ""
                            });
                        }
                        else
                        {
                            check.Value = check.Value + piItem.Value??0;
                        }
                    }                    
                }

                sumUnitPIValue.Add(new AllUnitPIValue {
                    UnitNumber = "DAP",
                    BGColor = "orange",
                    PIValue = listPIValue
                });
            }

            return Request.CreateResponse(HttpStatusCode.OK, sumUnitPIValue);
        }

        public HttpResponseMessage GetAllUnitDAPprices()
        {
            List<AllUnitPIValue> allUnitPIValue = new List<AllUnitPIValue>();
            List<AllUnitPIValue> sumUnitPIValue = new List<AllUnitPIValue>();
            List<PIValue> listPIValue = new List<PIValue>();
            //Double val1, val2, val3, val4, val5, val6, val7, val8, val9, val10, val11, val12,
            //    val13, val14, val15, val16, val17, val18, val19;
            using (TradingEntities db = new TradingEntities())
            {
                string query = $"exec [sp_AllUnit]";
                var data = db.Database
                    .SqlQuery<UnitPerRegionDB>(query)
                    .ToList<UnitPerRegionDB>();

                foreach (var item in data)
                {
                    allUnitPIValue.Add(new AllUnitPIValue
                    {
                        UnitNumber = item.UnitNumber,
                        BGColor = item.BColor,
                        PIValue = GetAllUnitPIValue(item.DAPPriceTagName, item.UnitNumber)
                    });
                }

                foreach (var item in allUnitPIValue)
                {
                    foreach (var piItem in item.PIValue)
                    {
                        var check = listPIValue.Where(x => x.TimestampLabel == piItem.TimestampLabel).FirstOrDefault();
                        if (check == null)
                        {
                            listPIValue.Add(new PIValue
                            {
                                Value = piItem.Value,
                                Timestamp = piItem.Timestamp,
                                TimestampLabel = piItem.TimestampLabel,
                                UnitNumber = ""
                            });
                        }
                        else
                        {
                            check.Value = check.Value + piItem.Value ?? 0;
                        }
                    }
                }

                sumUnitPIValue.Add(new AllUnitPIValue
                {
                    UnitNumber = "DAPPrice",
                    BGColor = "orange",
                    PIValue = listPIValue
                });
            }

            return Request.CreateResponse(HttpStatusCode.OK, sumUnitPIValue);
        }

        public List<PIValue> GetAllUnitPIValue(string TagName, string UnitNumber)
        {
            List<PIValue> listPIValue = new List<PIValue>();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];
            Nullable<Double> val;
            DateTime currentDate = DateTime.Now;

            PIPoint ptDAP = PIPoint.FindPIPoint(piServer, TagName);

            if (ptDAP != null)
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

                        var ptValue = ptDAP.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);

                        if (ptValue.Value.ToString() == "No Data" || ptValue.Value.ToString() == "Pt Created")
                        {
                            val = null;
                        }
                        else
                        {
                            val = Convert.ToDouble(ptValue.Value);
                        }

                        listPIValue.Add(new PIValue
                        {
                            Value = val,
                            UnitNumber = UnitNumber,
                            Timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                            TimestampLabel = hr
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

                        var ptValue = ptDAP.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);

                        if (ptValue.Value.ToString() == "No Data" || ptValue.Value.ToString() == "Pt Created")
                        {
                            val = null;
                        }
                        else
                        {
                            val = Convert.ToDouble(ptValue.Value);
                        }

                        listPIValue.Add(new PIValue
                        {
                            Value = val,
                            UnitNumber = UnitNumber,
                            Timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                            TimestampLabel = hr == "00" || hr == "0" ? "24" : hr
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

                        var ptValue = ptDAP.RecordedValue(new AFTime(date.ToShortDateString() + " " + hr + ":00:00"), AFRetrievalMode.Exact);

                        if (ptValue.Value.ToString() == "No Data" || ptValue.Value.ToString() == "Pt Created")
                        {
                            val = null;
                        }
                        else
                        {
                            val = Convert.ToDouble(ptValue.Value);
                        }
                        listPIValue.Add(new PIValue
                        {
                            Value = val,
                            UnitNumber = UnitNumber,
                            Timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                            TimestampLabel = hr
                        });
                    }

                    var ptVal = ptDAP.RecordedValue(new AFTime(date.AddDays(1).ToShortDateString() + " " + "00:00:00"), AFRetrievalMode.Exact);
                    if (ptVal.Value.ToString() == "No Data" || ptVal.Value.ToString() == "Pt Created")
                    {
                        val = null;
                    }
                    else
                    {
                        val = Convert.ToDouble(ptVal.Value);
                    }

                    listPIValue.Add(new PIValue
                    {
                        Value = val,
                        UnitNumber = UnitNumber,
                        Timestamp = Convert.ToDateTime(date.AddDays(1).ToShortDateString() + " " + "00:00:00"),
                        TimestampLabel = "24"
                    });
                }
            }

            return listPIValue;
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetHistoricalData(string UnitNumber, string StartTime, string EndTime, bool IsShowPrice)
        {
            List<HistoricalPIValue> listValue = new List<HistoricalPIValue>();
            Unit unit = new Unit();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            using (TradingEntities db = new TradingEntities())
            {
                string query = $"select top 1 * from m_Unit where IsActive = 1 and UnitNumber = '{UnitNumber}'";
                unit = db.Database
                    .SqlQuery<Unit>(query)
                    .FirstOrDefault<Unit>();

                PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);

                var rtdVals = ptRTD.RecordedValues(new AFTimeRange(StartTime, EndTime), AFBoundaryType.Inside, null, false, 0);
                var actualVals = ptActual.RecordedValues(new AFTimeRange(StartTime, EndTime), AFBoundaryType.Inside, null, false, 0);
                

                //add rtd values
                foreach(var item in rtdVals)
                {
                    Nullable<double> val;
                    if (item.Value.ToString() == "No Data" || item.Value.ToString() == "Pt Created")
                    {
                        val = null;
                    }
                    else
                    {
                        val = Convert.ToDouble(item.Value);
                    }

                    listValue.Add(new HistoricalPIValue {
                    RTD = val,
                    Timestamp = item.Timestamp.ToString()
                    });
                }

                foreach (var item in actualVals)
                {
                    
                    var check = listValue.Where(x => x.Timestamp == item.Timestamp.ToString()).FirstOrDefault();
                    if(check != null)
                    {
                        Nullable<double> val;
                        if (item.Value.ToString() == "No Data" || item.Value.ToString() == "Pt Created")
                        {
                            val = null;
                        }
                        else
                        {
                            val = Convert.ToDouble(item.Value);
                        }

                        check.Actual = val;
                    }
                }


                //if show price
                if (IsShowPrice)
                {
                    PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagName);
                    var priceVals = ptPrice.RecordedValues(new AFTimeRange(StartTime, EndTime), AFBoundaryType.Inside, null, false, 0);

                    foreach (var item in priceVals)
                    {
                        var check = listValue.Where(x => x.Timestamp == item.Timestamp.ToString()).FirstOrDefault();
                        if (check != null)
                        {
                            Nullable<double> val;
                            if (item.Value.ToString() == "No Data" || item.Value.ToString() == "Pt Created")
                            {
                                val = null;
                            }
                            else
                            {
                                val = Convert.ToDouble(item.Value);
                            }

                            check.Price = val;
                        }
                    }
                }
               
            }

            return Request.CreateResponse(HttpStatusCode.OK, listValue);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetHistoricalDataAHC( string StartTime, string EndTime, bool IsShowPrice)
        {
            List<HistoricalPIValueAHC> listValue = new List<HistoricalPIValueAHC>();
            List<HistoricalPIValue> listValue2 = new List<HistoricalPIValue>();
            List<Unit> units = new List<Unit>();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[GlobalFunctions.piServerName];

            using (TradingEntities db = new TradingEntities())
            {
                string query = $"select * from m_Unit where IsActive = 1 and SiteID = '{AHCSiteID}'";
                units = db.Database
                    .SqlQuery<Unit>(query)
                    .ToList<Unit>();

                foreach(var unit in units)
                {
                    PIPoint ptRTD = PIPoint.FindPIPoint(piServer, unit.RTDTagName);
                    PIPoint ptActual = PIPoint.FindPIPoint(piServer, unit.ActualTagName);

                    var rtdVals = ptRTD.RecordedValues(new AFTimeRange(StartTime, EndTime), AFBoundaryType.Inside, null, false, 0);
                    var actualVals = ptActual.RecordedValues(new AFTimeRange(StartTime, EndTime), AFBoundaryType.Inside, null, false, 0);


                    //add rtd values
                    foreach (var item in rtdVals)
                    {
                        Nullable<double> val;
                        if (item.Value.ToString() == "No Data" || item.Value.ToString() == "Pt Created")
                        {
                            val = null;
                        }
                        else
                        {
                            val = Convert.ToDouble(item.Value);
                        }

                        if (unit.UnitNumber == "01ANGAT_M")
                        {
                            listValue.Add(new HistoricalPIValueAHC
                            {
                                RTD_Main = val,
                                Timestamp = item.Timestamp.ToString()
                            });
                        }
                        else
                        {
                            listValue2.Add(new HistoricalPIValue
                            {
                                RTD = val,
                                Timestamp = item.Timestamp.ToString()
                            });
                        }
                            
                    }

                    foreach (var item in actualVals)
                    {
                        if (unit.UnitNumber == "01ANGAT_M")
                        {
                            var check = listValue.Where(x => x.Timestamp == item.Timestamp.ToString()).FirstOrDefault();
                            if (check != null)
                            {
                                Nullable<double> val;
                                if (item.Value.ToString() == "No Data" || item.Value.ToString() == "Pt Created")
                                {
                                    val = null;
                                }
                                else
                                {
                                    val = Convert.ToDouble(item.Value);
                                }

                                check.Actual_Main = val;
                            }
                        }
                        else
                        {
                            var check = listValue2.Where(x => x.Timestamp == item.Timestamp.ToString()).FirstOrDefault();
                            if (check != null)
                            {
                                Nullable<double> val;
                                if (item.Value.ToString() == "No Data" || item.Value.ToString() == "Pt Created")
                                {
                                    val = null;
                                }
                                else
                                {
                                    val = Convert.ToDouble(item.Value);
                                }

                                check.Actual = val;
                            }
                        }
                            
                        
                    }

                    //if show price
                    if (IsShowPrice)
                    {
                        PIPoint ptPrice = PIPoint.FindPIPoint(piServer, unit.EAPTagName);
                        var priceVals = ptPrice.RecordedValues(new AFTimeRange(StartTime, EndTime), AFBoundaryType.Inside, null, false, 0);

                        foreach (var item in priceVals)
                        {
                            if (unit.UnitNumber == "01ANGAT_M")
                            {
                                var check = listValue.Where(x => x.Timestamp == item.Timestamp.ToString()).FirstOrDefault();
                                if (check != null)
                                {
                                    Nullable<double> val;
                                    if (item.Value.ToString() == "No Data" || item.Value.ToString() == "Pt Created")
                                    {
                                        val = null;
                                    }
                                    else
                                    {
                                        val = Convert.ToDouble(item.Value);
                                    }

                                    check.Price_Main = val;
                                }
                            }
                            else
                            {
                                var check = listValue2.Where(x => x.Timestamp == item.Timestamp.ToString()).FirstOrDefault();
                                if (check != null)
                                {
                                    Nullable<double> val;
                                    if (item.Value.ToString() == "No Data" || item.Value.ToString() == "Pt Created")
                                    {
                                        val = null;
                                    }
                                    else
                                    {
                                        val = Convert.ToDouble(item.Value);
                                    }

                                    check.Price = val;
                                }
                            }
                                
                        }
                    }
                }

                foreach(var val2 in listValue2)
                {
                    var update = listValue.FirstOrDefault(x => x.Timestamp == val2.Timestamp);
                    update.Actual_Auxiliary = val2.Actual;
                    update.Price_Auxiliary = val2.Price;
                    update.RTD_Auxiliary = val2.RTD;
                }

            }

            return Request.CreateResponse(HttpStatusCode.OK, listValue);
        }
        #endregion
        //exec[sp_SaveOverrideValue] '01SUAL_G01','155.6',1
        //exec[sp_OverrideValue] '01SUAL_G01'

        //DEMAND_5MINS_CLUZ_MW.MV
        //DEMAND_5MINS_CVIS_MW.MV
        //DEMAND_5MINS_CMIN_MW.MV


        #region SQL Data
        [HttpGet]
        [Authorize]
        public HttpResponseMessage PriceSQL(string UnitNumber)
        {
            List<Schedule> sched = new List<Schedule>();
            int minMod = 0;
            Nullable<Double> val = null;
            
            using (TradingEntities db = new TradingEntities())
            {
                minMod = 5 - ((DateTime.Now.Minute) % 5);

                //Ahead
                for (int x = 20; x >= 5; x = x - 5)
                {
                    if (UnitNumber == "")
                    {
                        sched.Add(new Schedule
                        {
                            Value = null,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm")
                        });
                    }
                    else
                    {
                        //get pi data
                        DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00"));
                        var data = GetSQLData(timestamp, "HAP", UnitNumber);
                        if(data != null)
                        {
                            val = data.Price;
                        }
                        else
                        {
                            val = null;
                        }
                        
                        sched.Add(new Schedule
                        {
                            Value = val,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm")
                        });
                    }

                }

                //Current
                if (UnitNumber == "")
                {
                    sched.Add(new Schedule
                    {
                        Value = null,
                        Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                        TimestampLabel = DateTime.Now.AddMinutes(minMod).ToString("HH:mm")
                    });
                }
                else
                {
                    //get pi data
                    DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00"));
                    var data = GetSQLData(timestamp, "RTD", UnitNumber);
                    if (data != null)
                    {
                        val = data.Price;
                    }
                    else
                    {
                        val = null;
                    }
                    sched.Add(new Schedule
                    {
                        Value = val,
                        Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                        TimestampLabel = DateTime.Now.AddMinutes(minMod).ToString("HH:mm")
                    });
                }

                //Previous
                minMod = ((DateTime.Now.Minute) % 5);
                for (int x = 0; x <= 45; x = x + 5)
                {
                    if (UnitNumber == "")
                    {
                        sched.Add(new Schedule
                        {
                            Value = null,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm")
                        });
                    }
                    else
                    {
                        //get pi data
                        DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00"));
                        var data = GetSQLData(timestamp, "RTD", UnitNumber);
                        if (data != null)
                        {
                            val = data.Price;
                        }
                        else
                        {
                            val = null;
                        }
                        sched.Add(new Schedule
                        {
                            Value = val,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm")
                        });
                    }

                }
            }
            return Request.CreateResponse(HttpStatusCode.OK, sched);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage RTDSQL(string UnitNumber)
        {
            List<Schedule> sched = new List<Schedule>();
            int minMod = 0;
            Nullable<Double> val = null;
            Nullable<Double> actual = null;

            using (TradingEntities db = new TradingEntities())
            {
                minMod = 5 - ((DateTime.Now.Minute) % 5);

                //Ahead
                for (int x = 20; x >= 5; x = x - 5)
                {
                    if (UnitNumber == "")
                    {
                        sched.Add(new Schedule
                        {
                            Value = null,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm")
                        });
                    }
                    else
                    {
                        //get pi data
                        DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00"));
                        var data = GetSQLData(timestamp, "HAP", UnitNumber);
                        if (data != null)
                        {
                            val = data.Quantity;
                        }
                        else
                        {
                            val = null;
                        }
                        sched.Add(new Schedule
                        {
                            Value = val,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm")
                        });
                    }

                }

                string dataStatus = "R";
                //Current
                if (UnitNumber == "")
                {
                    sched.Add(new Schedule
                    {
                        Value = null,
                        Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                        TimestampLabel = DateTime.Now.AddMinutes(minMod).ToString("HH:mm")
                    });
                }
                else
                {
                    //get pi data
                    DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00"));
                    var rData = GetSQLData(timestamp, "RTD", UnitNumber);
                    if (rData != null)
                    {
                        val = rData.Quantity;
                        dataStatus = "R";
                    }
                    else
                    {
                        string query = $"exec[sp_OverrideValue] '{UnitNumber}'";
                        var oData = db.Database
                            .SqlQuery<Override>(query)
                            .FirstOrDefault<Override>();
                        if (oData.IsUse == true)
                        {
                            val = oData.Value;
                            dataStatus = "O";
                        }
                        else
                        {
                            var hData = GetSQLData(timestamp, "HAP", UnitNumber);
                            if (hData != null)
                            {
                                val = hData.Quantity;
                                dataStatus = "H";
                            }
                            else
                            {
                                val = null;
                                dataStatus = "R";
                            }
                        }
                    }

                    sched.Add(new Schedule
                    {
                        Value = val,
                        Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                        TimestampLabel = DateTime.Now.AddMinutes(minMod).ToString("HH:mm"),
                        DataStatus = dataStatus
                    });
                }

                //Previous
                minMod = ((DateTime.Now.Minute) % 5);
                for (int x = 0; x <= 45; x = x + 5)
                {
                    if (UnitNumber == "")
                    {
                        sched.Add(new Schedule
                        {
                            Value = null,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm")
                        });
                    }
                    else
                    {
                        //get pi data
                        Nullable<bool> isLimit;
                        DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00"));
                        var rData = GetSQLData(timestamp, "RTD", UnitNumber);
                        var aData = GetSQLData(timestamp, "DTC", UnitNumber);
                        if (rData != null)
                        {
                            val = rData.Quantity;
                        }
                        else
                        {
                            val = null;
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
                            if (actual >= (val - (val * 0.03)) && actual <= (val + (val * 0.015)))
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


                        sched.Add(new Schedule
                        {
                            Value = val,
                            Actual = actual,
                            IsLimit = isLimit,
                            Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            TimestampLabel = DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm")
                        });
                    }

                }
            }
            return Request.CreateResponse(HttpStatusCode.OK, sched);
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

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetUnitHAPSQL(string UnitNumber)
        {
            List<Schedule> sched = new List<Schedule>();
            string dateTime;
            Nullable<Double> val = null;

            using (TradingEntities db = new TradingEntities())
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
                    DateTime timestamp = Convert.ToDateTime(dateTime + ":" + min + ":00");
                    var data = GetSQLData(timestamp, "HAP", UnitNumber);
                    if(data!=null)
                    {
                        val = data.Quantity;
                    }
                    else
                    {
                        val = null;
                    }
                    sched.Add(new Schedule
                    {
                        Value = val,
                        Timestamp = Convert.ToDateTime(dateTime + ":" + min + ":00"),
                        TimestampLabel = i == 60 ? "60" : min
                    });

                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, sched);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetUnitDAPSQL(string UnitNumber)
        {
            List<Schedule> sched = new List<Schedule>();
            Nullable<Double> val;
            DateTime currentDate = DateTime.Now;

            using (TradingEntities db = new TradingEntities())
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

                        DateTime timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00");
                        var data = GetSQLData(timestamp, "DAP", UnitNumber);
                        if (data != null)
                        {
                            val = data.Quantity;
                        }
                        else
                        {
                            val = null;
                        }

                        sched.Add(new Schedule
                        {
                            Value = val,
                            Timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                            TimestampLabel = hr
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
                        var data = GetSQLData(timestamp, "DAP", UnitNumber);
                        if (data != null)
                        {
                            val = data.Quantity;
                        }
                        else
                        {
                            val = null;
                        }
                        sched.Add(new Schedule
                        {
                            Value = val,
                            Timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                            TimestampLabel = hr == "00" || hr == "0" ? "24" : hr
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
                        var data = GetSQLData(timestamp, "DAP", UnitNumber);
                        if (data != null)
                        {
                            val = data.Quantity;
                        }
                        else
                        {
                            val = null;
                        }

                        sched.Add(new Schedule
                        {
                            Value = val,
                            Timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                            TimestampLabel = hr
                        });
                    }

                    DateTime timestamp1 = Convert.ToDateTime(date.AddDays(1).ToShortDateString() + " " + "00:00:00");
                    var data1 = GetSQLData(timestamp1, "DAP", UnitNumber);
                    if (data1 != null)
                    {
                        val = data1.Quantity;
                    }
                    else
                    {
                        val = null;
                    }

                    sched.Add(new Schedule
                    {
                        Value = val,
                        Timestamp = Convert.ToDateTime(date.AddDays(1).ToShortDateString() + " " + "00:00:00"),
                        TimestampLabel = "24"
                    });
                }

            }

            return Request.CreateResponse(HttpStatusCode.OK, sched);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetDemandSQL(string UnitNumber = null)
        {
            List<PIValue> piValue = new List<PIValue>();
            List<PIValue> piLuzValue = new List<PIValue>();
            List<PIValue> piVisValue = new List<PIValue>();
            List<PIValue> piMinValue = new List<PIValue>();
            List<PIValue> piPriceValue = new List<PIValue>();
            List<Demands> demands = new List<Demands>();
            DateTime date = DateTime.Now;
            
            int minMod = 0;
            Nullable<double> luzVal, visVal, minVal, priceVal;

            //CLUZ CVIS CMIN
            
            minMod = 5 - ((DateTime.Now.Minute) % 5);
            //Current Values
            DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:00:00"));
            var luzData = GetSQLData(timestamp, "DAPDEMAND", "CLUZ");
            if (luzData != null)
            {
                luzVal = luzData.Quantity;
            }
            else
            {
                luzVal = null;
            }
            piValue.Add(new PIValue
            {
                Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:00:00")),
                Value = luzVal,
                UnitNumber = "Luz"
            });

            var visData = GetSQLData(timestamp, "DAPDEMAND", "CVIS");
            if (visData != null)
            {
                visVal = visData.Quantity;
            }
            else
            {
                visVal = null;
            }

            piValue.Add(new PIValue
            {
                Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:00:00")),
                Value = visVal,
                UnitNumber = "Vis"
            });

            var minData = GetSQLData(timestamp, "DAPDEMAND", "CMIN");
            if (minData != null)
            {
                minVal = minData.Quantity;
            }
            else
            {
                minVal = null;
            }

            piValue.Add(new PIValue
            {
                Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(minMod).ToString("HH:mm:00")),
                Value = minVal,
                UnitNumber = "Min"
            });

            demands.Add(new Demands
            {
                Description = "Current",
                PIValue = piValue
            });


            for (int i = 1; i <= 24; i = i + 1)
            {
                string dt = "";
                if (i == 24)
                {
                    dt = date.AddDays(1).ToShortDateString() + " 00:00:00";
                }
                else
                {
                    dt = date.ToShortDateString() + " " + i + ":00:00";
                }

                DateTime timestamp1 = Convert.ToDateTime(dt);
                var luzData1 = GetSQLData(timestamp1, "DAPDEMAND", "CLUZ");
                if (luzData1 != null)
                {
                    luzVal = luzData1.Quantity;
                }
                else
                {
                    luzVal = null;
                }

                var visData1 = GetSQLData(timestamp1, "DAPDEMAND", "CVIS");
                if (visData1 != null)
                {
                    visVal = visData1.Quantity;
                }
                else
                {
                    visVal = null;
                }

                var minData1 = GetSQLData(timestamp1, "DAPDEMAND", "CMIN");
                if (minData1 != null)
                {
                    minVal = minData1.Quantity;
                }
                else
                {
                    minVal = null;
                }



                piLuzValue.Add(new PIValue
                {
                    Value = luzVal,
                    Timestamp = Convert.ToDateTime(dt),
                    TimestampLabel = i.ToString()
                });

                piVisValue.Add(new PIValue
                {
                    Value = visVal,
                    Timestamp = Convert.ToDateTime(dt),
                    TimestampLabel = i.ToString()
                });

                piMinValue.Add(new PIValue
                {
                    Value = minVal,
                    Timestamp = Convert.ToDateTime(dt),
                    TimestampLabel = i.ToString()
                });


            }

            demands.Add(new Demands
            {
                Description = "Luz",
                PIValue = piLuzValue
            });

            demands.Add(new Demands
            {
                Description = "Vis",
                PIValue = piVisValue
            });

            demands.Add(new Demands
            {
                Description = "Min",
                PIValue = piMinValue
            });

            //Price
            if (UnitNumber != "")
            {
                for (int i = 1; i <= 24; i = i + 1)
                {
                    string dt = "";
                    if (i == 24)
                    {
                        dt = date.AddDays(1).ToShortDateString() + " 00:00:00";
                    }
                    else
                    {
                        dt = date.ToShortDateString() + " " + i + ":00:00";
                    }
                    DateTime timestampP = Convert.ToDateTime(dt);
                    var pData = GetSQLData(timestampP, "DAP", UnitNumber);
                    if(pData != null)
                    {
                        priceVal = pData.Price;
                    }
                    else
                    {
                        priceVal = null;
                    }

                    piPriceValue.Add(new PIValue
                    {
                        Value = priceVal,
                        Timestamp = Convert.ToDateTime(dt),
                        TimestampLabel = i.ToString()
                    });
                }
            }

            demands.Add(new Demands
            {
                Description = "Price",
                PIValue = piPriceValue
            });

            return Request.CreateResponse(HttpStatusCode.OK, demands);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetUnitPerRegionSQL()
        {
            var Luz = GetRegionPIValueSQL("Luzon");
            var Vis = GetRegionPIValueSQL("Vizayas");
            var Min = GetRegionPIValueSQL("Mindanao");
            return Request.CreateResponse(HttpStatusCode.OK, new { Luz = Luz, Vis = Vis, Min = Min });
        }

        public List<RegionPIValue> GetRegionPIValueSQL(string Region)
        {
            List<RegionPIValue> regionPIValue = new List<RegionPIValue>();
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
                    //PIPoint ptRemarks = PIPoint.FindPIPoint(piServer, item.ComplianceTagName);
                    DateTime timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + 5)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + 5)).ToString("HH:mm:00"));
                    var rData = GetSQLData(timestamp, "RTD", item.UnitNumber);
                    var aData = GetSQLData(timestamp, "DTC", item.UnitNumber);
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

                    if(rData != null)
                    {
                        rtd = rData.Quantity;
                        price = rData.Price;
                    }
                    else
                    {
                        rtd = null;
                        price = null;
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
                        Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(-(minMod + 5)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + 5)).ToString("HH:mm:00")),
                        TimestampLabel = DateTime.Now.AddMinutes(-(minMod + 5)).ToString("HH:mm")
                    });
                }
            }

            return regionPIValue;
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAllUnitDAPSQL()
        {
            List<AllUnitPIValue> allUnitPIValue = new List<AllUnitPIValue>();
            List<AllUnitPIValue> sumUnitPIValue = new List<AllUnitPIValue>();
            List<PIValue> listPIValue = new List<PIValue>();
            //Double val1, val2, val3, val4, val5, val6, val7, val8, val9, val10, val11, val12,
            //    val13, val14, val15, val16, val17, val18, val19;
            using (TradingEntities db = new TradingEntities())
            {
                string query = $"exec [sp_AllUnit]";
                var data = db.Database
                    .SqlQuery<UnitPerRegionDB>(query)
                    .ToList<UnitPerRegionDB>();

                foreach (var item in data)
                {
                    allUnitPIValue.Add(new AllUnitPIValue
                    {
                        UnitNumber = item.UnitNumber,
                        BGColor = item.BColor,
                        PIValue = GetAllUnitPIValueSQL(item.DAPTagName, item.UnitNumber)
                    });
                }

                foreach (var item in allUnitPIValue)
                {
                    foreach (var piItem in item.PIValue)
                    {
                        var check = listPIValue.Where(x => x.TimestampLabel == piItem.TimestampLabel).FirstOrDefault();
                        if (check == null)
                        {
                            listPIValue.Add(new PIValue
                            {
                                Value = piItem.Value,
                                Timestamp = piItem.Timestamp,
                                TimestampLabel = piItem.TimestampLabel,
                                UnitNumber = ""
                            });
                        }
                        else
                        {
                            check.Value = check.Value + piItem.Value ?? 0;
                        }
                    }
                }

                sumUnitPIValue.Add(new AllUnitPIValue
                {
                    UnitNumber = "DAP",
                    BGColor = "orange",
                    PIValue = listPIValue
                });
            }

            return Request.CreateResponse(HttpStatusCode.OK, sumUnitPIValue);
        }

        public List<PIValue> GetAllUnitPIValueSQL(string TagName, string UnitNumber)
        {
            List<PIValue> listPIValue = new List<PIValue>();
            Nullable<Double> val;
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
                    var data = GetSQLData(timestamp, "DAP", UnitNumber);
                    if (data != null)
                    {
                        val = data.Quantity;
                    }
                    else
                    {
                        val = null;
                    }

                    listPIValue.Add(new PIValue
                    {
                        Value = val,
                        UnitNumber = UnitNumber,
                        Timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                        TimestampLabel = hr
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
                    var data = GetSQLData(timestamp, "DAP", UnitNumber);
                    if (data != null)
                    {
                        val = data.Quantity;
                    }
                    else
                    {
                        val = null;
                    }

                    listPIValue.Add(new PIValue
                    {
                        Value = val,
                        UnitNumber = UnitNumber,
                        Timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                        TimestampLabel = hr == "00" || hr == "0" ? "24" : hr
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
                    var data = GetSQLData(timestamp, "DAP", UnitNumber);
                    if (data != null)
                    {
                        val = data.Quantity;
                    }
                    else
                    {
                        val = null;
                    }

                    listPIValue.Add(new PIValue
                    {
                        Value = val,
                        UnitNumber = UnitNumber,
                        Timestamp = Convert.ToDateTime(date.ToShortDateString() + " " + hr + ":00:00"),
                        TimestampLabel = hr
                    });
                }

                DateTime timestamp1 = Convert.ToDateTime(date.AddDays(1).ToShortDateString() + " " + "00:00:00");
                var data1 = GetSQLData(timestamp1, "DAP", UnitNumber);
                if (data1 != null)
                {
                    val = data1.Quantity;
                }
                else
                {
                    val = null;
                }

                listPIValue.Add(new PIValue
                {
                    Value = val,
                    UnitNumber = UnitNumber,
                    Timestamp = Convert.ToDateTime(date.AddDays(1).ToShortDateString() + " " + "00:00:00"),
                    TimestampLabel = "24"
                });
            }
            return listPIValue;
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetRemarksSQL(string UnitNumber, string Timestamp)
        {
            List<ScheduleString> sched = new List<ScheduleString>();
            string remarks = "";
            Nullable<Double> actual = null;
            Nullable<Double> rtd = null;
            GenRemarks genRemarks = new GenRemarks();

            using (TradingEntities db = new TradingEntities())
            {
                //get remarks
                try
                {
                    string query = $"exec [sp_GeneralComment] '{UnitNumber}'";
                    genRemarks = db.Database.SqlQuery<GenRemarks>(query).FirstOrDefault<GenRemarks>();
                }
                catch
                {
                    genRemarks.ID = 1;
                    genRemarks.UnitNumber = UnitNumber;
                    genRemarks.Value = "";
                }

                for (int x = 5; x <= 60; x += 5)
                {
                    string dt = "";
                    string timelb = "";
                    try
                    {
                        if (x == 60)
                        {
                            dt = Convert.ToDateTime(Timestamp + ":00:00").AddHours(1).ToString("MM/dd/yyyy HH") + ":00";
                        }
                        else
                        {
                            dt = Timestamp + ":" + x;
                        }
                        timelb = Convert.ToDateTime(dt).ToString("HH:mm");
                    }
                    catch (Exception ex)
                    {
                        dt = Timestamp + ":00";
                        timelb = ex.Message + " " + Timestamp;
                    }

                    Nullable<bool> isLimit;
                    DateTime timestamp = Convert.ToDateTime(dt);
                    var rData = GetSQLData(timestamp, "RTD", UnitNumber);
                    var data = GetSQLData(timestamp, "DTC", UnitNumber);
                    if (rData != null)
                    {
                        rtd = rData.Quantity;
                    }
                    else
                    {
                        rtd = null;
                    }
                    if (data != null)
                    {
                        actual = data.Quantity;
                    }
                    else
                    {
                        actual = null;
                    }
                    remarks = null;

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

                    sched.Add(new ScheduleString
                    {
                        Value = remarks,
                        Actual = actual,
                        IsLimit = isLimit,
                        Timestamp = Convert.ToDateTime(dt),
                        TimestampLabel = timelb
                    });
                }

            }
            return Request.CreateResponse(HttpStatusCode.OK, new { sched = sched, genRemarks = genRemarks });
        }
        #endregion
    }
}
