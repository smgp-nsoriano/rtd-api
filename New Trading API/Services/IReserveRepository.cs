using New_Trading_API.Models;
using NPOI.POIFS.Crypt.Dsig;
using NPOI.SS.Formula.Functions;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Linq;

namespace New_Trading_API.Services
{
    interface IReserveRepository
    {
        List<ReserveSchedule> GetReserveSchedulePrice(string unit, string commodity, bool isScedule);
        List<ReserveScheduleView> GetReserveScheduleView(string unitNumber);
        List<ReserveSchedule> GetRegionalReservePrice(string region, string commodity);
        List<ReserveScheduleView> GetRegionalReservePriceView(string region);
    }

    public class ReserveService : IReserveRepository
    {
        private string PIServerName = GlobalFunctions.piServerName;
        const string MIN = "5MINS";
        const string EN = "EN";
        const string FR = "FR";
        const string DR = "DR";
        const string RD = "RD";
        const string RDP = "RD Price";
        const string RU = "RU";
        const string RUP = "RU Price";
        const string ENP = "EN Price";
        const string RMP = "RM Price";
        const int aheadProjMinuteSpan = 20;
        const int currentInterval = 0;
        const int previousMinuteSpan = 45;

        public List<ReserveSchedule> GetReserveSchedulePrice(string unitNumber, string commodity, bool isScedule)
        {
            string rtdTagname = string.Empty;
            string hapTagname = string.Empty;
            string actualTagname = string.Empty;
            
            if (isScedule)
            {
                if (commodity.ToLower() == EN.ToLower())
                {
                    rtdTagname = String.Format(Constant.ReserveDefaults.SchedualeTagNames["RTD"], MIN, unitNumber);
                    hapTagname = String.Format(Constant.ReserveDefaults.SchedualeTagNames["HAP"], MIN, unitNumber);
                }
                else
                {
                    rtdTagname = String.Format(Constant.ReserveDefaults.SchedualeTagNames["RTD"], commodity, unitNumber);
                    hapTagname = String.Format(Constant.ReserveDefaults.SchedualeTagNames["HAP"], commodity, unitNumber);
                }
            }
            else
            {
                if (commodity.ToLower() == EN.ToLower())
                {
                    rtdTagname = String.Format(Constant.ReserveDefaults.PriceTagNames["RTD"], MIN, unitNumber);
                    hapTagname = String.Format(Constant.ReserveDefaults.PriceTagNames["HAP"], MIN, unitNumber);
                }
                else
                {
                    rtdTagname = String.Format(Constant.ReserveDefaults.PriceTagNames["RTD"], commodity, unitNumber);
                    hapTagname = String.Format(Constant.ReserveDefaults.PriceTagNames["HAP"], commodity, unitNumber);
                }
                    
            }
            actualTagname = String.Format(Constant.ReserveDefaults.SchedualeTagNames["ACTUAL"], unitNumber);
            int minMod = 5 - ((DateTime.Now.Minute) % 5);
            DateTime now = DateTime.Now;

            List<ReserveSchedule> reserveSchedules = new List<ReserveSchedule>();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[PIServerName];

            PIPoint ptHap = PIPoint.FindPIPoint(piServer, hapTagname);
            PIPoint ptRtd = PIPoint.FindPIPoint(piServer, rtdTagname);
            PIPoint ptActual = PIPoint.FindPIPoint(piServer, actualTagname);
            PIPoint ptRTDPlus10 = PIPoint.FindPIPoint(piServer, rtdTagname);
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

            // HAP Data
            for (int x = aheadProjMinuteSpan; x >= aheadMin; x = x - 5)
            {
                //if there is no selected unit.
                // default schedule is null
                if (unitNumber == "")
                {
                    reserveSchedules.Add(new ReserveSchedule
                    {
                        Tagname = hapTagname,
                        Schedule = null,
                        Timestamp = Convert.ToDateTime(now.AddMinutes(x + minMod).ToShortDateString() + " " + now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                        Interval = now.AddMinutes(x + minMod).ToString("HH:mm")
                    });
                }
                else
                {
                    var val = GetPIValue(ptHap, minMod, x, false);

                    string shortDateString = now.AddMinutes(x + minMod).ToShortDateString();
                    int interv = now.AddMinutes(x + minMod).Minute == 0 ? now.AddMinutes(x + minMod).Hour : now.AddMinutes(x + minMod).Hour + 1;
                    Nullable<Double> offer_price = GetSpecificBidOfferPrice(shortDateString, interv, unitNumber, commodity, val);

                    reserveSchedules.Add(new ReserveSchedule
                    {
                        Tagname = hapTagname,
                        Schedule = val,
                        OfferPrice = offer_price,
                        Timestamp = Convert.ToDateTime(now.AddMinutes(x + minMod).ToShortDateString() + " " + now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                        Interval = now.AddMinutes(x + minMod).ToString("HH:mm")
                    });
                }
            }

            if (aheadMin == 10)
            {
                var val = Convert.ToDouble(ptRTDValue10.Value);

                string shortDateString = now.AddMinutes(5 + minMod).ToShortDateString();
                int interv = now.AddMinutes(5 + minMod).Minute == 0 ? now.AddMinutes(5 + minMod).Hour : now.AddMinutes(5 + minMod).Hour + 1;
                Nullable<Double> offer_price = GetSpecificBidOfferPrice(shortDateString, interv, unitNumber, commodity, val);

                reserveSchedules.Add(new ReserveSchedule
                {
                    Tagname = rtdTagname,
                    Schedule = val,
                    OfferPrice = offer_price,
                    Timestamp = Convert.ToDateTime(DateTime.Now.AddMinutes(5 + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(5 + minMod).ToString("HH:mm:00")),
                    Interval = DateTime.Now.AddMinutes(5 + minMod).ToString("HH:mm")
                });
            }


            //Current RTD Data
            if (unitNumber == "")
            {
                //if there is no selected unit.
                // default schedule is null
                reserveSchedules.Add(new ReserveSchedule
                {
                    Tagname = rtdTagname,
                    Schedule = null,
                    Timestamp = Convert.ToDateTime(now.AddMinutes(minMod).ToShortDateString() + " " + now.AddMinutes(minMod).ToString("HH:mm:00")),
                    Interval = now.AddMinutes(minMod).ToString("HH:mm")
                });
            }
            else
            {
                string dataStatus = "R";
                var val = GetPIValue(ptRtd, minMod, currentInterval, false);
                if (isScedule)
                {
                    if (val == null)
                    {
                        val = GetOverrideValue(unitNumber, commodity, out dataStatus);
                    }
                    else
                    {
                        DisableOverride(unitNumber, commodity);
                    }
                }

                string shortDateString = now.AddMinutes(minMod).ToShortDateString();
                int interv = now.AddMinutes(minMod).Minute == 0 ? now.AddMinutes(minMod).Hour : now.AddMinutes(minMod).Hour + 1;
                Nullable<Double> offer_price = GetSpecificBidOfferPrice(shortDateString, interv, unitNumber, commodity, val);

                reserveSchedules.Add(new ReserveSchedule
                {
                    Tagname = rtdTagname,
                    Schedule = val,
                    OfferPrice = offer_price,
                    Timestamp = Convert.ToDateTime(now.AddMinutes(minMod).ToShortDateString() + " " + now.AddMinutes(minMod).ToString("HH:mm:00")),
                    Interval = now.AddMinutes(minMod).ToString("HH:mm"),
                    DataStatus = dataStatus
                });
            }

            // Previous RTD Data
            minMod = ((DateTime.Now.Minute) % 5);
            for (int x = 0; x <= previousMinuteSpan; x = x + 5)
            {
                if (unitNumber == "")
                {
                    //if there is no selected unit.
                    // default schedule is null
                    reserveSchedules.Add(new ReserveSchedule
                    {
                        Tagname = rtdTagname,
                        Schedule = null,
                        Timestamp = Convert.ToDateTime(now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                        Interval = now.AddMinutes(-(minMod + x)).ToString("HH:mm"),
                    });
                }
                else
                {
                    var val = GetPIValue(ptRtd, minMod, x, true);
                    var actual = GetPIValue(ptActual, minMod, x, true);

                    string shortDateString = now.AddMinutes(-(minMod + x)).ToShortDateString();
                    int interv = now.AddMinutes(-(minMod + x)).Minute == 0 ? now.AddMinutes(-(minMod + x)).Hour : now.AddMinutes(-(minMod + x)).Hour + 1;
                    Nullable<Double> offer_price = GetSpecificBidOfferPrice(shortDateString, interv, unitNumber, commodity, val);

                    reserveSchedules.Add(new ReserveSchedule
                    {
                        Tagname = rtdTagname,
                        Schedule = val,
                        Actual = actual,
                        OfferPrice = offer_price,
                        Timestamp = Convert.ToDateTime(now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                        Interval = now.AddMinutes(-(minMod + x)).ToString("HH:mm")
                    });
                }
            }

            return reserveSchedules;
        }

        //public Nullable<Double> GetSpecificBidOfferPrice(string shortDateString, int interv, string unitNumber, string commodity, Nullable<Double> val)
        //{
        //    Nullable<Double> offer_price = null;
        //    string query = "";
        //    var data = new OfferDB();

        //    if (commodity.ToLower() == RU.ToLower())
        //    {
        //        using (TradingEntities db = new TradingEntities())
        //        {
        //            query = $"select * from [Trading].[dbo].[t_BidOffer] where DateSchedule = '{shortDateString}' and Interval = {interv} and UnitNumber = '{unitNumber}'";
        //            data = db.Database.SqlQuery<OfferDB>(query).FirstOrDefault<OfferDB>();

        //            if (data != null)
        //            {
        //                if (val <= data.AS_RU_P1)
        //                {
        //                    offer_price = data.AS_RU_Q1;
        //                }
        //                else if (val > data.AS_RU_P1 && val <= data.AS_RU_P2)
        //                {
        //                    offer_price = data.AS_RU_Q2;
        //                }
        //                else if (val > data.AS_RU_P2 && val <= data.AS_RU_P3)
        //                {
        //                    offer_price = data.AS_RU_Q3;
        //                }
        //                else if (val > data.AS_RU_P3 && val <= data.AS_RU_P4)
        //                {
        //                    offer_price = data.AS_RU_Q4;
        //                }
        //                else if (val > data.AS_RU_P4 && val <= data.AS_RU_P5)
        //                {
        //                    offer_price = data.AS_RU_Q5;
        //                }
        //                else
        //                {
        //                    offer_price = null;
        //                }
        //            }
        //        }
        //    }
        //    if (commodity.ToLower() == RD.ToLower())
        //    {
        //        using (TradingEntities db = new TradingEntities())
        //        {
        //            query = $"select * from [Trading].[dbo].[t_BidOffer] where DateSchedule = '{shortDateString}' and Interval = {interv} and UnitNumber = '{unitNumber}'";
        //            data = db.Database.SqlQuery<OfferDB>(query).FirstOrDefault<OfferDB>();

        //            if (data != null)
        //            {
        //                if (val <= data.AS_RD_P1)
        //                {
        //                    offer_price = data.AS_RD_Q1;
        //                }
        //                else if (val > data.AS_RD_P1 && val <= data.AS_RD_P2)
        //                {
        //                    offer_price = data.AS_RD_Q2;
        //                }
        //                else if (val > data.AS_RD_P2 && val <= data.AS_RD_P3)
        //                {
        //                    offer_price = data.AS_RD_Q3;
        //                }
        //                else if (val > data.AS_RD_P3 && val <= data.AS_RD_P4)
        //                {
        //                    offer_price = data.AS_RD_Q4;
        //                }
        //                else if (val > data.AS_RD_P4 && val <= data.AS_RD_P5)
        //                {
        //                    offer_price = data.AS_RD_Q5;
        //                }
        //                else
        //                {
        //                    offer_price = null;
        //                }
        //            }
        //        }
        //    }

        //    return offer_price;
        //}


        public Nullable<double> GetSpecificBidOfferPrice(string shortDateString,int interv,string unitNumber,string commodity,Nullable<double> val)
        {
            Nullable<double> offer_price = null;

            DateTime now = DateTime.Now;

            // ======================================================
            // STEP 0: CURRENT BASE 5-MIN ALIGNMENT
            // ======================================================
            DateTime currentInterval = new DateTime(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                (now.Minute / 5) * 5,
                0
            );

            int offset = interv - 6;

            DateTime intervalTime = currentInterval.AddMinutes(offset * 5);

            // ======================================================
            // STEP 1: DELAYED COLUMN (+15 mins RTD preview)
            // ======================================================
            DateTime delayedTime = now.AddMinutes(15);

            delayedTime = new DateTime(
                delayedTime.Year,
                delayedTime.Month,
                delayedTime.Day,
                delayedTime.Hour,
                (delayedTime.Minute / 5) * 5,
                0
            );

            // ======================================================
            // STEP 2: FINAL TIME SELECTION
            // ======================================================
            DateTime finalTime;

            if (interv == 5)
            {
                finalTime = delayedTime;
            }
            else
            {
                finalTime = intervalTime;
            }

            // ======================================================
            // STEP 3: PI CONNECTION
            // ======================================================
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[PIServerName];

            string prefix = commodity.ToUpper() == "RU" ? "BID_RU" : "BID_RD";

            Func<string, string> tag = (x) => $"{prefix}_{unitNumber}_{x}";

            Func<string, double?> getVal = (suffix) =>
            {
                try
                {
                    var pt = PIPoint.FindPIPoint(piServer, tag(suffix));
                    if (pt == null) return null;

                    return GetPIValue2(pt, finalTime.ToString());
                }
                catch
                {
                    return null;
                }
            };

            // ======================================================
            // STEP 4: FETCH PI CURVE (P/Q)
            // ======================================================
            double? p1 = getVal("P1");
            double? p2 = getVal("P2");
            double? p3 = getVal("P3");
            double? p4 = getVal("P4");
            double? p5 = getVal("P5");

            double? q1 = getVal("Q1");
            double? q2 = getVal("Q2");
            double? q3 = getVal("Q3");
            double? q4 = getVal("Q4");
            double? q5 = getVal("Q5");

            // ======================================================
            // STEP 5: VALIDATION
            // ======================================================
            if (!val.HasValue)
                return null;

            if (!p1.HasValue && !p2.HasValue && !p3.HasValue && !p4.HasValue && !p5.HasValue)
                return null;

            // ======================================================
            // STEP 6: PI TIER EVALUATION (FIXED LOGIC)
            // ======================================================
            if (commodity.ToLower() == RU.ToLower() || commodity.ToLower() == RD.ToLower())
            {
                if (p5.HasValue && val >= p5) offer_price = q5;
                else if (p4.HasValue && val >= p4) offer_price = q4;
                else if (p3.HasValue && val >= p3) offer_price = q3;
                else if (p2.HasValue && val >= p2) offer_price = q2;
                else if (p1.HasValue && val >= p1) offer_price = q1;
                else offer_price = null;
            }

            return offer_price;
        }

        public List<ReserveSchedule> GetReserveOfferPrice(string unitNumber, string commodity)
        {
            List<ReserveSchedule> reserveSchedules = new List<ReserveSchedule>();
            
            var now = DateTime.Now;
            var shortDateString = now.ToShortDateString();
            //var interval = now.Hour + 1;

            int minMod = 5 - ((now.Minute) % 5);
            int aheadMin = 5;

            Nullable<Double> val = null;
            Nullable<Double>[] prices;

            using (TradingEntities db = new TradingEntities())
            {
                string query = "";
                var data = new OfferDB();

                // HAP Data
                for (int x = aheadProjMinuteSpan; x >= 5; x = x - 5)
                {
                    query = $"select * from t_BidOffer where DateSchedule = '{shortDateString}' and Interval = {now.AddMinutes(x + minMod).Hour + 1} and UnitNumber = '{unitNumber}'";
                    data = db.Database.SqlQuery<OfferDB>(query).FirstOrDefault<OfferDB>();

                    if (data == null) {
                        reserveSchedules.Add(new ReserveSchedule
                        {
                            Tagname = "from_SQL",
                            Schedule = null,
                            Timestamp = Convert.ToDateTime(now.AddMinutes(x + minMod).ToShortDateString() + " " + now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                            Interval = now.AddMinutes(x + minMod).ToString("HH:mm")
                        });
                    }
                    else
                    {
                        switch (commodity)
                        {
                            case "RU":
                                prices = new Nullable<Double>[] { data.AS_RU_Q5, data.AS_RU_Q4, data.AS_RU_Q3, data.AS_RU_Q2, data.AS_RU_Q1 };
                                foreach (Nullable<Double> i in prices)
                                {
                                    if (i.HasValue)
                                    {
                                        val = i.Value; break;
                                    }
                                }
                                break;
                            case "RD":
                                prices = new Nullable<Double>[] { data.AS_RD_Q5, data.AS_RD_Q4, data.AS_RD_Q3, data.AS_RD_Q2, data.AS_RD_Q1 };
                                foreach (Nullable<Double> i in prices)
                                {
                                    if (i.HasValue)
                                    {
                                        val = i.Value; break;
                                    }
                                }
                                break;
                        }
                        reserveSchedules.Add(new ReserveSchedule
                        {
                            Tagname = "from_SQL",
                            Schedule = val,
                            Timestamp = Convert.ToDateTime(now.AddMinutes(x + minMod).ToShortDateString() + " " + now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                            Interval = now.AddMinutes(x + minMod).ToString("HH:mm")
                        });
                    }
                }

                // RTD
                query = $"select * from t_BidOffer where DateSchedule = '{shortDateString}' and Interval = {now.AddMinutes(minMod).Hour + 1} and UnitNumber = '{unitNumber}'";
                data = db.Database.SqlQuery<OfferDB>(query).FirstOrDefault<OfferDB>();

                if (data == null)
                {
                    reserveSchedules.Add(new ReserveSchedule
                    {
                        Tagname = "from_SQL",
                        Schedule = null,
                        Timestamp = Convert.ToDateTime(now.AddMinutes(minMod).ToShortDateString() + " " + now.AddMinutes(minMod).ToString("HH:mm:00")),
                        Interval = now.AddMinutes(minMod).ToString("HH:mm")
                        // ,DataStatus = dataStatus
                    });
                }
                else
                {
                    switch (commodity)
                    {
                        case "RU":
                            prices = new Nullable<Double>[] { data.AS_RU_Q5, data.AS_RU_Q4, data.AS_RU_Q3, data.AS_RU_Q2, data.AS_RU_Q1 };
                            foreach (Nullable<Double> i in prices)
                            {
                                if (i.HasValue)
                                {
                                    val = i.Value; break;
                                }
                            }
                            break;
                        case "RD":
                            prices = new Nullable<Double>[] { data.AS_RD_Q5, data.AS_RD_Q4, data.AS_RD_Q3, data.AS_RD_Q2, data.AS_RD_Q1 };
                            foreach (Nullable<Double> i in prices)
                            {
                                if (i.HasValue)
                                {
                                    val = i.Value; break;
                                }
                            }
                            break;
                    }

                    reserveSchedules.Add(new ReserveSchedule
                    {
                        Tagname = "from_SQL",
                        Schedule = val,
                        Timestamp = Convert.ToDateTime(now.AddMinutes(minMod).ToShortDateString() + " " + now.AddMinutes(minMod).ToString("HH:mm:00")),
                        Interval = now.AddMinutes(minMod).ToString("HH:mm")
                        // ,DataStatus = dataStatus
                    });
                }

                // Compliance
                for (int x = 0; x <= previousMinuteSpan; x = x + 5)
                {
                    query = $"select * from t_BidOffer where DateSchedule = '{shortDateString}' and Interval = {now.AddMinutes(-(minMod + x)).Hour + 1} and UnitNumber = '{unitNumber}'";
                    data = db.Database.SqlQuery<OfferDB>(query).FirstOrDefault<OfferDB>();

                    if (data == null)
                    {
                        reserveSchedules.Add(new ReserveSchedule
                        {
                            Tagname = "from_SQL",
                            Schedule = null,
                            // Actual = actual,
                            Timestamp = Convert.ToDateTime(now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            Interval = now.AddMinutes(-(minMod + x)).ToString("HH:mm")
                        });
                    }
                    else
                    {
                        switch (commodity)
                        {
                            case "RU":
                                prices = new Nullable<Double>[] { data.AS_RU_Q5, data.AS_RU_Q4, data.AS_RU_Q3, data.AS_RU_Q2, data.AS_RU_Q1 };
                                foreach (Nullable<Double> i in prices)
                                {
                                    if (i.HasValue)
                                    {
                                        val = i.Value; break;
                                    }
                                }
                                break;
                            case "RD":
                                prices = new Nullable<Double>[] { data.AS_RD_Q5, data.AS_RD_Q4, data.AS_RD_Q3, data.AS_RD_Q2, data.AS_RD_Q1 };
                                foreach (Nullable<Double> i in prices)
                                {
                                    if (i.HasValue)
                                    {
                                        val = i.Value; break;
                                    }
                                }
                                break;
                        }

                        reserveSchedules.Add(new ReserveSchedule
                        {
                            Tagname = "from_SQL",
                            Schedule = val,
                            // Actual = actual,
                            Timestamp = Convert.ToDateTime(now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                            Interval = now.AddMinutes(-(minMod + x)).ToString("HH:mm")
                        });
                    }
                }
            }

            return reserveSchedules;
        }

        public List<ReserveScheduleView> GetReserveScheduleView(string unitNumber)
        {
            List<ReserveScheduleView> reserveScheduleViews = new List<ReserveScheduleView>();

            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = EN,
                ReserveSchedules = GetReserveSchedulePrice(unitNumber, EN, true)
            });
            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = FR,
                ReserveSchedules = GetReserveSchedulePrice(unitNumber, FR, true)
            });
            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = RD,
                ReserveSchedules = GetReserveSchedulePrice(unitNumber, RD, true)
            });
            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = RU,
                ReserveSchedules = GetReserveSchedulePrice(unitNumber, RU, true)
            });
            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = DR,
                ReserveSchedules = GetReserveSchedulePrice(unitNumber, DR, true)
            });
            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = ENP,
                ReserveSchedules = GetReserveSchedulePrice(unitNumber, EN, false)
            });
            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = RMP,
                ReserveSchedules = GetReserveSchedulePrice(unitNumber, EN, false)
            });
            //reserveScheduleViews.Add(new ReserveScheduleView
            //{
            //    Type = RUP,
            //    ReserveSchedules = GetReserveOfferPrice(unitNumber, "RU")
            //});
            //reserveScheduleViews.Add(new ReserveScheduleView
            //{
            //    Type = RDP,
            //    ReserveSchedules = GetReserveOfferPrice(unitNumber, "RD")
            //});

            return reserveScheduleViews;
        }

        public List<ReserveSchedule> GetRegionalReservePrice(string region, string commodity)
        {
            string rtdTagname = string.Empty;
            string hapTagname = string.Empty;

            rtdTagname = String.Format(Constant.ReserveDefaults.OpressTagNames["RTD"], commodity, region);
            hapTagname = String.Format(Constant.ReserveDefaults.OpressTagNames["HAP"], commodity, region);

            int minMod = 5 - ((DateTime.Now.Minute) % 5);
            DateTime now = DateTime.Now;

            List<ReserveSchedule> reserveSchedules = new List<ReserveSchedule>();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[PIServerName];

            PIPoint ptdap = PIPoint.FindPIPoint(piServer, hapTagname);
            PIPoint ptRtd = PIPoint.FindPIPoint(piServer, rtdTagname);

            // HAP Data
            for (int x = aheadProjMinuteSpan; x >= 5; x = x - 5)
            {
                //if there is no selected unit.
                // default schedule is null
                if (region == "")
                {
                    reserveSchedules.Add(new ReserveSchedule
                    {
                        Tagname = hapTagname,
                        Schedule = null,
                        Timestamp = Convert.ToDateTime(now.AddMinutes(x + minMod).ToShortDateString() + " " + now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                        Interval = now.AddMinutes(x + minMod).ToString("HH:mm")
                    });
                }
                else
                {
                    var val = GetPIValue(ptdap, minMod, x, false);
                    reserveSchedules.Add(new ReserveSchedule
                    {
                        Tagname = hapTagname,
                        Schedule = val,
                        Timestamp = Convert.ToDateTime(now.AddMinutes(x + minMod).ToShortDateString() + " " + now.AddMinutes(x + minMod).ToString("HH:mm:00")),
                        Interval = now.AddMinutes(x + minMod).ToString("HH:mm")
                    });
                }
            }

            //Current RTD Data
            if (region == "")
            {
                //if there is no selected unit.
                // default schedule is null
                reserveSchedules.Add(new ReserveSchedule
                {
                    Tagname = hapTagname,
                    Schedule = null,
                    Timestamp = Convert.ToDateTime(now.AddMinutes(minMod).ToShortDateString() + " " + now.AddMinutes(minMod).ToString("HH:mm:00")),
                    Interval = now.AddMinutes(minMod).ToString("HH:mm")
                });
            }
            else
            {
                var val = GetPIValue(ptdap, minMod, currentInterval, false);
                reserveSchedules.Add(new ReserveSchedule
                {
                    Tagname = hapTagname,
                    Schedule = val,
                    Timestamp = Convert.ToDateTime(now.AddMinutes(minMod).ToShortDateString() + " " + now.AddMinutes(minMod).ToString("HH:mm:00")),
                    Interval = now.AddMinutes(minMod).ToString("HH:mm")
                });
            }

            // Previous RTD Data
            minMod = ((DateTime.Now.Minute) % 5);
            for (int x = 0; x <= previousMinuteSpan; x = x + 5)
            {
                if (region == "")
                {
                    //if there is no selected unit.
                    // default schedule is null
                    reserveSchedules.Add(new ReserveSchedule
                    {
                        Tagname = rtdTagname,
                        Schedule = null,
                        Timestamp = Convert.ToDateTime(now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                        Interval = now.AddMinutes(-(minMod + x)).ToString("HH:mm")
                    });
                }
                else
                {
                    var val = GetPIValue(ptRtd, minMod, x, true);
                    reserveSchedules.Add(new ReserveSchedule
                    {
                        Tagname = rtdTagname,
                        Schedule = val,
                        Timestamp = Convert.ToDateTime(now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")),
                        Interval = now.AddMinutes(-(minMod + x)).ToString("HH:mm")
                    });
                }
            }

            return reserveSchedules;
        }

        public List<ReserveRSP> GetRegionalReservePrice24h(string region, string commodity, string date)
        {
            string rtd_reqt_tn = string.Empty;
            string rtd_sched_tn = string.Empty;
            string rtd_price_tn = string.Empty;
            string dap_reqt_tn = string.Empty;
            string dap_sched_tn = string.Empty;
            string dap_price_tn = string.Empty;

            rtd_reqt_tn = String.Format(Constant.ReserveDefaults.MrktReqtTagNames["RTD"], commodity, region);
            rtd_sched_tn = String.Format(Constant.ReserveDefaults.MrktSchedTagNames["RTD"], commodity, region);
            rtd_price_tn = String.Format(Constant.ReserveDefaults.OpressTagNames["RTD"], commodity, region);
            dap_reqt_tn = String.Format(Constant.ReserveDefaults.MrktReqtTagNames["DAP"], commodity, region);
            dap_sched_tn = String.Format(Constant.ReserveDefaults.MrktSchedTagNames["DAP"], commodity, region);
            dap_price_tn = String.Format(Constant.ReserveDefaults.OpressTagNames["DAP"], commodity, region);

            // int minMod = 5 - ((DateTime.Now.Minute) % 5);
            // DateTime now = DateTime.Now;

            List<ReserveRSP> reserveSchedules = new List<ReserveRSP>();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[PIServerName];

            PIPoint rtd_reqt_pt = PIPoint.FindPIPoint(piServer, rtd_reqt_tn);
            PIPoint rtd_sched_pt = PIPoint.FindPIPoint(piServer, rtd_sched_tn);
            PIPoint rtd_price_pt = PIPoint.FindPIPoint(piServer, rtd_price_tn);
            PIPoint dap_reqt_pt = PIPoint.FindPIPoint(piServer, dap_reqt_tn);
            PIPoint dap_sched_pt = PIPoint.FindPIPoint(piServer, dap_sched_tn);
            PIPoint dap_price_pt = PIPoint.FindPIPoint(piServer, dap_price_tn);

            //Current RTD Data
            for (int x = 0; x < 24; x = x + 1) {
                var datetime = DateTime.Parse(date + " 00:00:00").AddHours(x);
                // var datetime = DateTime.Parse(DateTime.Now.ToString("d") + " " + DateTime.Now.Hour + ":00:00");
                var currentHour = DateTime.Now.Hour;

                if (datetime.Hour <= currentHour)
                {
                    // if datetime.Hour is in the past of current use RTD
                    var rtd_reqt_val = GetPIValue2(rtd_reqt_pt, datetime.ToString());
                    var rtd_sched_val = GetPIValue2(rtd_sched_pt, datetime.ToString());
                    var rtd_price_val = GetPIValue2(rtd_price_pt, datetime.ToString());

                    reserveSchedules.Add(new ReserveRSP
                    {
                        MrktReqt_Tagname = rtd_reqt_tn,
                        Schedule_Tagname = rtd_sched_tn,
                        Price_Tagname = rtd_price_tn,
                        MrktReqt = rtd_reqt_val,
                        Schedule = rtd_sched_val,
                        Price = rtd_price_val,
                        Timestamp = Convert.ToDateTime(datetime.ToString()),
                        Interval = DateTime.Parse(datetime.ToString()).ToString("HH:mm")
                    });
                }
                else
                {
                    // else use DAP
                    var dap_reqt_val = GetPIValue2(dap_reqt_pt, datetime.ToString());
                    var dap_sched_val = GetPIValue2(dap_sched_pt, datetime.ToString());
                    var dap_price_val = GetPIValue2(dap_price_pt, datetime.ToString());

                    reserveSchedules.Add(new ReserveRSP
                    {
                        MrktReqt_Tagname = dap_reqt_tn,
                        Schedule_Tagname = dap_sched_tn,
                        Price_Tagname = dap_price_tn,
                        MrktReqt = dap_reqt_val,
                        Schedule = dap_sched_val,
                        Price = dap_price_val,
                        Timestamp = Convert.ToDateTime(datetime.ToString()),
                        Interval = DateTime.Parse(datetime.ToString()).ToString("HH:mm")
                    });
                }
            }

            // Previous RTD Data
            // minMod = ((DateTime.Now.Minute) % 5);
            

            return reserveSchedules;
        }

        public List<ReserveRSP> GetRegionalReserveRSP(string region, string commodity)
        {
            string rtd_reqt_tn = string.Empty;
            string rtd_sched_tn = string.Empty;
            string rtd_price_tn = string.Empty;

            rtd_reqt_tn = String.Format(Constant.ReserveDefaults.MrktReqtTagNames["RTD"], commodity, region);
            rtd_sched_tn = String.Format(Constant.ReserveDefaults.MrktSchedTagNames["RTD"], commodity, region);
            rtd_price_tn = String.Format(Constant.ReserveDefaults.OpressTagNames["RTD"], commodity, region);

            List<ReserveRSP> reserveSchedules = new List<ReserveRSP>();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[PIServerName];

            PIPoint rtd_reqt_pt = PIPoint.FindPIPoint(piServer, rtd_reqt_tn);
            PIPoint rtd_sched_pt = PIPoint.FindPIPoint(piServer, rtd_sched_tn);
            PIPoint rtd_price_pt = PIPoint.FindPIPoint(piServer, rtd_price_tn);

            var now = DateTime.Now;
            var datetime = DateTime.Parse(now.ToString("d") + " " + now.Hour + ":" + now.Minute + ":00").AddMinutes(-(now.Minute % 5));
            var dtDelayed10mins = datetime.AddMinutes(-5);

            // get value from PI using tagname and datetime
            var rtd_reqt_val = GetPIValue2(rtd_reqt_pt, dtDelayed10mins.ToString());
            var rtd_sched_val = GetPIValue2(rtd_sched_pt, dtDelayed10mins.ToString());
            var rtd_price_val = GetPIValue2(rtd_price_pt, dtDelayed10mins.ToString());

            reserveSchedules.Add(new ReserveRSP
            {
                MrktReqt_Tagname = rtd_reqt_tn,
                Schedule_Tagname = rtd_sched_tn,
                Price_Tagname = rtd_price_tn,
                MrktReqt = rtd_reqt_val,
                Schedule = rtd_sched_val,
                Price = rtd_price_val,
                Timestamp = Convert.ToDateTime(dtDelayed10mins),
                Interval = DateTime.Parse(dtDelayed10mins.ToString()).ToString("HH:mm")
            });

            return reserveSchedules;
        }

        public List<ReservePortfolio> GetRegionalPortfolio(string region)
        {
            List<UnitPerSiteName> unitList = new List<UnitPerSiteName>();
            List<UnitPerSiteName> additionalResults = new List<UnitPerSiteName>();
            using (TradingEntities db = new TradingEntities())
            {
                if(region == "UPSI Mindanao")
                {
                    string Str = $@"exec sp_GetUnitsPerSiteName '{region}'";
                    unitList = db.Database.SqlQuery<UnitPerSiteName>(Str).ToList<UnitPerSiteName>();

                    string additional = $@"exec sp_GetUnitsPerSiteName 'SMCPC'";
                    additionalResults = db.Database.SqlQuery<UnitPerSiteName>(additional).ToList<UnitPerSiteName>();

                    unitList.AddRange(additionalResults);
                }
                else
                {
                    string Str = $@"exec sp_GetUnitsPerSiteName '{region}'";
                    unitList = db.Database.SqlQuery<UnitPerSiteName>(Str).ToList<UnitPerSiteName>();
                }
            }

            List<ReservePortfolio> portfolio = new List<ReservePortfolio>();
            PIServers piServers = new PIServers();
            PIServer piServer = piServers[PIServerName];
            var now = DateTime.Now;
            var datetime = DateTime.Parse(now.ToString("d") + " " + now.Hour + ":" + now.Minute + ":00").AddMinutes(-(now.Minute % 5));
            var dtDelayed10mins = datetime.AddMinutes(-5);
            var date = dtDelayed10mins.ToString("yyyy-MM-dd");

            foreach (UnitPerSiteName unit in unitList)
            {
                PIPoint rtd_en_pt = PIPoint.FindPIPoint(piServer, $@"RTDEG_5MINS_{unit.UnitNumber}_MW.MV");
                PIPoint rtd_ru_pt = PIPoint.FindPIPoint(piServer, $@"RTDEG_RU_{unit.UnitNumber}_MW.MV");
                PIPoint rtd_rd_pt = PIPoint.FindPIPoint(piServer, $@"RTDEG_RD_{unit.UnitNumber}_MW.MV");
                PIPoint rtd_cr_pt = PIPoint.FindPIPoint(piServer, $@"RTDEG_FR_{unit.UnitNumber}_MW.MV");
                PIPoint actual_pt = PIPoint.FindPIPoint(piServer, String.Format(Constant.ReserveDefaults.SchedualeTagNames["ACTUAL"], unit.UnitNumber));
                PIPoint price_en_pt = PIPoint.FindPIPoint(piServer, $@"RTDEG_5MINS_{unit.UnitNumber}_P.MV");

                var rtd_en_val = GetPIValue2(rtd_en_pt, dtDelayed10mins.ToString());
                var rtd_ru_val = GetPIValue2(rtd_ru_pt, dtDelayed10mins.ToString());
                var rtd_rd_val = GetPIValue2(rtd_rd_pt, dtDelayed10mins.ToString());
                var rtd_cr_val = GetPIValue2(rtd_cr_pt, dtDelayed10mins.ToString());
                var actual_val = GetPIValue2(actual_pt, dtDelayed10mins.ToString());
                var price_en_val = GetPIValue2(price_en_pt, dtDelayed10mins.ToString());

                List<BidOfferControlMode> bidCmode = new List<BidOfferControlMode>();
                List<GenRemarks> remarks = new List<GenRemarks>();
                Nullable<Double> OfferPrice_RU = null;
                Nullable<Double> OfferPrice_RD = null;
                Nullable<Double>[] prices;
                using (TradingEntities db2 = new TradingEntities())
                {
                    // bid control mode / MOP
                    int mop_hour = dtDelayed10mins.Hour;
                    if (dtDelayed10mins.Minute > 0)
                    {
                        mop_hour = dtDelayed10mins.Hour + 1;
                    }
                    string Str2 = $@"exec sp_GetControlModeOnSpecificBidOffer '{unit.UnitNumber}', '{date}', {mop_hour}";
                    bidCmode = db2.Database.SqlQuery<BidOfferControlMode>(Str2).ToList<BidOfferControlMode>();

                    // general remarks
                    Str2 = $@"select [UnitNumber], [Value] from [t_UnitGeneralComments] where [UnitNumber] = '{unit.UnitNumber}'";
                    remarks = db2.Database.SqlQuery<GenRemarks>(Str2).ToList<GenRemarks>();

                    // offer price
                    string query = $"select * from [Trading].[dbo].[t_BidOffer] where DateSchedule = '{date}' and Interval = {mop_hour} and UnitNumber = '{unit.UnitNumber}'";
                    var data = db2.Database.SqlQuery<OfferDB>(query).FirstOrDefault<OfferDB>();
                    if (data != null)
                    {
                        // RU
                        if (rtd_ru_val <= data.AS_RU_P1)
                        {
                            OfferPrice_RU = data.AS_RU_Q1;
                        }
                        else if (rtd_ru_val > data.AS_RU_P1 && rtd_ru_val <= data.AS_RU_P2)
                        {
                            OfferPrice_RU = data.AS_RU_Q2;
                        }
                        else if (rtd_ru_val > data.AS_RU_P2 && rtd_ru_val <= data.AS_RU_P3)
                        {
                            OfferPrice_RU = data.AS_RU_Q3;
                        }
                        else if (rtd_ru_val > data.AS_RU_P3 && rtd_ru_val <= data.AS_RU_P4)
                        {
                            OfferPrice_RU = data.AS_RU_Q4;
                        }
                        else if (rtd_ru_val > data.AS_RU_P4 && rtd_ru_val <= data.AS_RU_P5)
                        {
                            OfferPrice_RU = data.AS_RU_Q5;
                        }
                        else
                        {
                            OfferPrice_RU = null;
                        }
                        // RD
                        if (rtd_rd_val <= data.AS_RD_P1)
                        {
                            OfferPrice_RD = data.AS_RD_Q1;
                        }
                        else if (rtd_rd_val > data.AS_RD_P1 && rtd_rd_val <= data.AS_RD_P2)
                        {
                            OfferPrice_RD = data.AS_RD_Q2;
                        }
                        else if (rtd_rd_val > data.AS_RD_P2 && rtd_rd_val <= data.AS_RD_P3)
                        {
                            OfferPrice_RD = data.AS_RD_Q3;
                        }
                        else if (rtd_rd_val > data.AS_RD_P3 && rtd_rd_val <= data.AS_RD_P4)
                        {
                            OfferPrice_RD = data.AS_RD_Q4;
                        }
                        else if (rtd_rd_val > data.AS_RD_P4 && rtd_rd_val <= data.AS_RD_P5)
                        {
                            OfferPrice_RD = data.AS_RD_Q5;
                        }
                        else
                        {
                            OfferPrice_RD = null;
                        }
                    }
                }
                string mop = (bidCmode.Count == 0) ? "" : bidCmode.First().ControlMode;
                string remark = (remarks.Count == 0) ? "" : remarks.First().Value;

                portfolio.Add(new ReservePortfolio
                {
                    UnitNumber = unit.UnitNumber,
                    RTD_EN = rtd_en_val,
                    RTD_RU = rtd_ru_val,
                    RTD_RD = rtd_rd_val,
                    RTD_CR = rtd_cr_val,
                    Actual = actual_val,
                    MOP = mop,
                    Price_EN = price_en_val,
                    Price_RU = null,
                    Price_RD = null,
                    Price_CR = null,
                    OfferPrice_RU = OfferPrice_RU,
                    OfferPrice_RD = OfferPrice_RD,
                    Remarks = remark,
                    Timestamp = Convert.ToDateTime(dtDelayed10mins),
                    Interval = DateTime.Parse(dtDelayed10mins.ToString()).ToString("HH:mm"),
                });
            }

            return portfolio;
        }


        public List<ReserveScheduleView> GetRegionalReservePriceView(string region)
        {
            List<ReserveScheduleView> reserveScheduleViews = new List<ReserveScheduleView>();

            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = RU,
                ReserveSchedules = GetRegionalReservePrice(region, RU)
            });

            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = RD,
                ReserveSchedules = GetRegionalReservePrice(region, RD)
            });

            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = FR,
                ReserveSchedules = GetRegionalReservePrice(region, FR)
            });

            return reserveScheduleViews;
        }

        public List<ReserveScheduleView> GetRegionalReservePrice24hView(string region, string date)
        {
            List<ReserveScheduleView> reserveScheduleViews = new List<ReserveScheduleView>();

            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = RU,
                ReserveRegionalSchedules = GetRegionalReservePrice24h(region, RU, date)
            });

            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = RD,
                ReserveRegionalSchedules = GetRegionalReservePrice24h(region, RD, date)
            });

            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = FR,
                ReserveRegionalSchedules = GetRegionalReservePrice24h(region, FR, date)
            });

            return reserveScheduleViews;
        }

        public List<ReserveScheduleView> GetRegionalReserveRSPView(string region)
        {
            List<ReserveScheduleView> reserveScheduleViews = new List<ReserveScheduleView>();

            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = RU,
                ReserveRegionalSchedules = GetRegionalReserveRSP(region, RU)
            });

            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = RD,
                ReserveRegionalSchedules = GetRegionalReserveRSP(region, RD)
            });

            reserveScheduleViews.Add(new ReserveScheduleView
            {
                Type = FR,
                ReserveRegionalSchedules = GetRegionalReserveRSP(region, FR)
            });

            return reserveScheduleViews;
        }

        public List<ReserveRegionalPortfolioView> GetRegionalPortfolioView()
        {
            List<ReserveRegionalPortfolioView> reserveScheduleViews = new List<ReserveRegionalPortfolioView>();

            reserveScheduleViews.Add(new ReserveRegionalPortfolioView
            {
                region = "UPSI",
                ReservePortfolios = GetRegionalPortfolio("UPSI")
            });

            reserveScheduleViews.Add(new ReserveRegionalPortfolioView
            {
                region = "UPSI Visayas",
                ReservePortfolios = GetRegionalPortfolio("UPSI Visayas")
            });

            reserveScheduleViews.Add(new ReserveRegionalPortfolioView
            {
                region = "UPSI Mindanao",
                ReservePortfolios = GetRegionalPortfolio("UPSI Mindanao")
            });

            return reserveScheduleViews;
        }



        private Nullable<Double> GetPIValue(PIPoint pt, int minMod, int x, bool isPrev)
        {
            Nullable<Double> val;
            AFValue ptValue;
            //get resrve pi data
            if (isPrev)
            {
                ptValue = pt.RecordedValue(new AFTime(DateTime.Now.AddMinutes(-(minMod + x)).ToShortDateString() + " " + DateTime.Now.AddMinutes(-(minMod + x)).ToString("HH:mm:00")), AFRetrievalMode.Exact);
            }
            else
            {
                ptValue = pt.RecordedValue(new AFTime(DateTime.Now.AddMinutes(x + minMod).ToShortDateString() + " " + DateTime.Now.AddMinutes(x + minMod).ToString("HH:mm:00")), AFRetrievalMode.Exact);
            }

            if (ptValue.Value.ToString() == "No Data" || ptValue.Value.ToString() == "Pt Created")
            {
                val = null;
            }
            else
            {
                val = Convert.ToDouble(ptValue.Value);
            }

            return val;
        }

        private Nullable<Double> GetPIValue2(PIPoint pt, string datetimeString)
        {
            Nullable<Double> val;
            AFValue ptValue;
            //get resrve pi data
            ptValue = pt.RecordedValue(new AFTime(datetimeString), AFRetrievalMode.Exact);

            if (ptValue.Value.ToString() == "No Data" || ptValue.Value.ToString() == "Pt Created")
            {
                val = null;
            }
            else
            {
                val = Convert.ToDouble(ptValue.Value);
            }

            return val;
        }

        private double? GetOverrideValue(string unitNumber, string commodity, out string dataStatus)
        {
            double? val = null;
            dataStatus = "R";
            using (TradingEntities db = new TradingEntities())
            {
                string query = $"exec[sp_OverrideValue] '{unitNumber}'";
                var data = db.Database
                    .SqlQuery<Override>(query)
                    .FirstOrDefault<Override>();

                switch (commodity)
                {
                    case EN:
                        if (data.IsUse == true)
                        {
                            val = data.Value;
                            dataStatus = "O";
                        }
                        break;
                    case RD:
                        if (data.RdIsUse == true)
                        {
                            val = data.RdValue;
                            dataStatus = "O";
                        }
                        break;
                    case RU:
                        if (data.RuIsUse == true)
                        {
                            val = data.RuValue;
                            dataStatus = "O";
                        }
                        break;
                    case DR:
                        if (data.DrIsUse == true)
                        {
                            val = data.DrValue;
                            dataStatus = "O";
                        }
                        break;
                    case FR:
                        if (data.FrIsUse == true)
                        {
                            val = data.FrValue;
                            dataStatus = "O";
                        }
                        break;
                }
            }

            return val;
        }
    
        private void DisableOverride(string unitNumber, string commodity)
        {
            using (TradingEntities db = new TradingEntities())
            {
                string isUseParam = string.Empty;
                switch (commodity)
                {
                    case EN:
                        isUseParam = "IsUse";
                        break;
                    case RD:
                        isUseParam = "RdIsUse";
                        break;
                    case RU:
                        isUseParam = "RuIsUse";
                        break;
                    case DR:
                        isUseParam = "DrIsUse";
                        break;
                    case FR:
                        isUseParam = "FrIsUse";
                        break;
                    default:
                        isUseParam = "IsUse";
                        break;
                }

                string query = $@"update t_OverrideValue 
                                            set {isUseParam} = 0 
                                        where UnitNumber = '{unitNumber}'";
                db.Database.ExecuteSqlCommand(query);
            }
        }

       
    }
}
