using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace New_Trading_API.Models
{
    public class Schedule
    {
        public Nullable<double> Value { get; set; }
        public Nullable<double> Actual { get; set; }
        public Nullable<bool> IsLimit { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimestampLabel { get; set; }
        public string DataStatus { get; set; }
        public Nullable<Boolean> IsWithAlarm { get; set; }
    }

    public class ScheduleString
    {
        public string Value { get; set; }
        public Nullable<double> Actual { get; set; }
        public Nullable<bool> IsLimit { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimestampLabel { get; set; }
    }

    public class StringValue
    {
        public string UnitNumber { get; set; }
        public string Value { get; set; }
        public string Timestamp { get; set; }
    }
    public class PIValue
    {
        public Nullable<double> Value { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimestampLabel { get; set; }
        public string UnitNumber { get; set; }
    }

    public class AllUnitPIValue
    {
        public string UnitNumber { get; set; }
        public string BGColor { get; set; }
        public List<PIValue> PIValue { get; set; }
    }

    public class Override
    {
        public string UnitNumber { get; set; }
        public Nullable<Double> Value { get; set; }
        public Nullable<Double> RdValue { get; set; }
        public Nullable<Double> FrValue { get; set; }
        public Nullable<Double> RuValue { get; set; }
        public Nullable<Double> DrValue { get; set; }
        public Nullable<Boolean> IsUse { get; set; }
        public Nullable<Boolean> RdIsUse { get; set; }
        public Nullable<Boolean> DrIsUse { get; set; }
        public Nullable<Boolean> FrIsUse { get; set; }
        public Nullable<Boolean> RuIsUse { get; set; }
        public Nullable<Boolean> IsReserve { get; set; }
    }

    public class Demands {
        public string Description { get; set; }
        public List<PIValue> PIValue { get; set; }
    }

    public class UnitPerRegionDB
    {
        public int UnitID { get; set; }
        public string UnitNumber { get; set; }
        public string RTDTagName { get; set; }
        public string EAPTagName { get; set; }
        public string DAPTagName { get; set; }
        public string DAPPriceTagName { get; set; }
        public string HAPTagName { get; set; }
        public string ActualTagName { get; set; }
        public string ComplianceTagName { get; set; }
        public Nullable<bool> IsWithAlarm { get; set; }
        public Nullable<bool> IsWithDecimal { get; set; }
        public string SiteName { get; set; }
        public string Region { get; set; }
        public string BColor { get; set; }
    }


    public class RegionPIValue
    {
        public Nullable<double> RTD { get; set; }
        public Nullable<double> Actual { get; set; }
        public Nullable<double> Price { get; set; }
        public Nullable<double> Percentage { get; set; }
        public Nullable<bool> IsLimit { get; set; }
        public string Remarks { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimestampLabel { get; set; }
        public string UnitNumber { get; set; }
        public string SiteName { get; set; }

    }

    public class GenRemarks
    {
        public int ID { get; set; }
        public string UnitNumber { get; set; }
        public string Value { get; set; }
    }
    public class HistoricalPIValue
    {
        public string Timestamp { get; set; }
        public Nullable<double> RTD { get; set; }
        public Nullable<double> Actual { get; set; }
        public Nullable<double> Price { get; set; }

    }
    public class HistoricalPIValueAHC
    {
        public string Timestamp { get; set; }
        public Nullable<double> RTD_Main { get; set; }
        public Nullable<double> Actual_Main { get; set; }
        public Nullable<double> Price_Main { get; set; }
        public Nullable<double> RTD_Auxiliary { get; set; }
        public Nullable<double> Actual_Auxiliary { get; set; }
        public Nullable<double> Price_Auxiliary { get; set; }

    }
}