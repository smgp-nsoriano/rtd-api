using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace New_Trading_API.Models
{
    public class ReserveSchedule
    {
        public string Tagname { get; set; }
        public Nullable<double> Schedule { get; set; }
        public Nullable<double> Price { get; set; }
        public Nullable<double> MrktReqt { get; set; }
        public Nullable<double> Actual { get; set; }
        public DateTime Timestamp { get; set; }
        public string Interval { get; set; }
        public string DataStatus { get; set; }
    }

    public class ReservePortfolio
    {
        public string UnitNumber { get; set; }
        public Nullable<double> RTD_EN { get; set; }
        public Nullable<double> RTD_RU { get; set; }
        public Nullable<double> RTD_RD { get; set; }
        public Nullable<double> RTD_CR { get; set; }
        public Nullable<double> Actual { get; set; }
        public string MOP { get; set; }
        public Nullable<double> Price_EN { get; set; }
        public Nullable<double> Price_RU { get; set; }
        public Nullable<double> Price_RD { get; set; }
        public Nullable<double> Price_CR { get; set; }
        public string Remarks { get; set; }
        public DateTime Timestamp { get; set; }
        public string Interval { get; set; }
    }

    public class ReserveRegionalPortfolioView
    {
        public string region { get; set; }
        public List<ReservePortfolio> ReservePortfolios { get; set; }
    }

    public class ReserveRSP
    {
        public string MrktReqt_Tagname { get; set; }
        public string Schedule_Tagname { get; set; }
        public string Price_Tagname { get; set; }
        public Nullable<double> Schedule { get; set; }
        public Nullable<double> Price { get; set; }
        public Nullable<double> MrktReqt { get; set; }
        public DateTime Timestamp { get; set; }
        public string Interval { get; set; }
    }

    public class ReserveScheduleView
    {
        public string Type { get; set; }
        public List<ReserveSchedule> ReserveSchedules { get; set; }
        public List<ReserveRSP> ReserveRegionalSchedules { get; set; }
    }
}