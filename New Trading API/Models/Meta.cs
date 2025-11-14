using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace New_Trading_API.Models
{

    public class Site
    {
        public int SiteID { get; set; }
        public string SiteName { get; set; }
        public string SiteCode { get; set; }
        public string Address { get; set; }
        public string ParticipantID { get; set; }
        public string Region { get; set; }
        public string FtpPath { get; set; }
        public Boolean IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreationDate { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<DateTime> ModificationDate { get; set; }
    }

    public class Unit
    {
        public int UnitID { get; set; }
        public string UnitNumber { get; set; }
        public string UnitName { get; set; }
        public int SiteID { get; set; }
        public string TypeID { get; set; }
        public string BColor { get; set; }
        public string FColor { get; set; }
        public string RTDTagName { get; set; }
        public string EAPTagName { get; set; }
        public string DAPTagName { get; set; }
        public string DAPPriceTagName { get; set; }
        public string HAPTagName { get; set; }
        public string HAPPriceTagName { get; set; }
        public string WAPTagName { get; set; }
        public string ActualTagName { get; set; }
        public string ComplianceTagName { get; set; }

        public string RTDTagNameH { get; set; }
        public string EAPTagNameH { get; set; }
        public string DAPTagNameH { get; set; }
        public string DAPPriceTagNameH { get; set; }
        public string HAPTagNameH { get; set; }
        public string HAPPriceTagNameH { get; set; }
        public string WAPTagNameH { get; set; }
        public string ActualTagNameH { get; set; }
        public Boolean IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreationDate { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<DateTime> ModificationDate { get; set; }
        public Nullable<bool> IsWithDecimal { get; set; }
        public Nullable<bool> IsWithAlarm { get; set; }
    }

    public class ManualEntry
    {
        public int UnitID { get; set; }
        public string UnitNumber { get; set; }
        public int SiteID { get; set; }
        public string TypeID { get; set; }
        public string RTDTagName { get; set; }
        public string PriceTagName { get; set; }
        public string HAPTagName { get; set; }
        public Nullable<Double> Value { get; set; }
        public Nullable<Double> PriceValue { get; set; }
        public Nullable<DateTime> TimeStamp { get; set; }
    }

    public class OfferEntry
    {
        public string UnitNumber { get; set; }
        public DateTime Date { get; set; }
        public Nullable<DateTime> PreviousDate { get; set; }
        public Nullable<Boolean> IsWithOffer { get; set; }
        public Nullable<Boolean> IsWithRampRate { get; set; }
    }

    public class User
    {
        public int UserID { get; set; }
        public string UserName { get; set; }
        public string UserType { get; set; }
        public Nullable<int> UserTypeID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public Nullable<Boolean> IsActive { get; set; }
        public Nullable<Boolean> IsShowPrice { get; set; }
        public Nullable<Boolean> IsShowConfig { get; set; }
        public Nullable<Boolean> IsShowUserConfig { get; set; }
        public Nullable<Boolean> IsShowBid { get; set; }
        public Nullable<Boolean> IsManualEntry { get; set; }
        public Nullable<Boolean> IsCamSetup { get; set; }
        public Nullable<Boolean> IsAlarmDisable { get; set; }
        public Nullable<Boolean> IsOverride { get; set; }
        public Nullable<Boolean> IsPortfolioOnly { get; set; }
        public Nullable<Boolean> IsPbReason { get; set; }
        public Nullable<Boolean> IsOperator { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<DateTime> CreationDate { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<DateTime> ModificationDate { get; set; }
        public Nullable<int> PermissionID { get; set; }
        public Nullable<Boolean> IsSPDC { get; set; }
        public Nullable<Boolean> IsMobileAccess { get; set; }
    }

    public class AccountTypeAndPermission
    {
        public Nullable<int> AccountTypeID { get; set; }
        public string AccountType { get; set; }
        public Nullable<Boolean> IsActive { get; set; }
        public string UserPermissions { get; set; }
        public Nullable<int> PermissionID { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<DateTime> CreationDate { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<DateTime> ModificationDate { get; set; }
    }

    public class OperatorUnit
    {
        public Nullable<int> UnitID { get; set; }
        public Nullable<int> SiteID { get; set; }
        public string UnitNumber { get; set; }
        public string UnitName { get; set; }
        public string SiteCode { get; set; }
        public Nullable<bool> IsWithDecimal { get; set; }
        public Nullable<bool> IsWithAlarm { get; set; }
    }

    public class TagValue
    {
        public Nullable<Double> RTDValue { get; set; }
        public Nullable<Double> PriceValue { get; set; }
        public Nullable<Double> ActualValue { get; set; }
        public Nullable<Double> DAPValue { get; set; }
        public Nullable<DateTime> TimeStamp { get; set; }
        public Nullable<Boolean> IsLimit { get; set; }
        public string DAPHour { get; set; }
        public string DateTIme { get; set; }
        public string DataStatus { get; set; }
        public Nullable<Boolean> IsWithDecimal { get; set; }
        public Nullable<Boolean> IsWithAlarm { get; set; }
        public Nullable<Double> RRU { get; set; }
        public Nullable<Double> RRD { get; set; }
        public Nullable<Double> Contingency { get; set; }
        public string RUDataStatus { get; set; }
        public string RDDataStatus { get; set; }
        public string ConDataStatus { get; set; }

    }

    public class AHCTagValue
    {
        public Nullable<Double> RTDValue { get; set; }
        public Nullable<Double> PriceValue { get; set; }
        public Nullable<Double> ActualValue { get; set; }
        public Nullable<Double> DAPValue { get; set; }
        public Nullable<DateTime> TimeStamp { get; set; }
        public Nullable<Boolean> IsLimit { get; set; }
        public string DAPHour { get; set; }
        public string DateTIme { get; set; }
        public string DataStatus { get; set; }

        public Nullable<Double> RTDValue2 { get; set; }
        public Nullable<Double> PriceValue2 { get; set; }
        public Nullable<Double> ActualValue2 { get; set; }
        public Nullable<Double> DAPValue2 { get; set; }
        public string DataStatus2 { get; set; }
        public Nullable<Boolean> IsLimit2 { get; set; }
    }

    public class Bid
    {
        public DateTime Date { get; set; }
        public string DateFormatted { get; set; }
        public int Interval { get; set; }
        public string ResourceID { get; set; }
        public string ProducType { get; set; }
        public Nullable<Double> P1 { get; set; }
        public Nullable<Double> Q1 { get; set; }
        public Nullable<Double> P2 { get; set; }
        public Nullable<Double> Q2 { get; set; }
        public Nullable<Double> P3 { get; set; }
        public Nullable<Double> Q3 { get; set; }
        public Nullable<Double> P4 { get; set; }
        public Nullable<Double> Q4 { get; set; }
        public Nullable<Double> P5 { get; set; }
        public Nullable<Double> Q5 { get; set; }
        public Nullable<Double> P6 { get; set; }
        public Nullable<Double> Q6 { get; set; }
        public Nullable<Double> P7 { get; set; }
        public Nullable<Double> Q7 { get; set; }
        public Nullable<Double> P8 { get; set; }
        public Nullable<Double> Q8 { get; set; }
        public Nullable<Double> P9 { get; set; }
        public Nullable<Double> Q9 { get; set; }
        public Nullable<Double> P10 { get; set; }
        public Nullable<Double> Q10 { get; set; }
        public Nullable<Double> P11 { get; set; }
        public Nullable<Double> Q11 { get; set; }
        public Nullable<Double> AS_RU_Q1 { get; set; }
        public Nullable<Double> AS_RU_P1 { get; set; }
        public Nullable<Double> AS_RU_Q2 { get; set; }
        public Nullable<Double> AS_RU_P2 { get; set; }
        public Nullable<Double> AS_RU_Q3 { get; set; }
        public Nullable<Double> AS_RU_P3 { get; set; }
        public Nullable<Double> AS_RU_Q4 { get; set; }
        public Nullable<Double> AS_RU_P4 { get; set; }
        public Nullable<Double> AS_RU_Q5 { get; set; }
        public Nullable<Double> AS_RU_P5 { get; set; }
        public Nullable<Double> AS_RD_Q1 { get; set; }
        public Nullable<Double> AS_RD_P1 { get; set; }
        public Nullable<Double> AS_RD_Q2 { get; set; }
        public Nullable<Double> AS_RD_P2 { get; set; }
        public Nullable<Double> AS_RD_Q3 { get; set; }
        public Nullable<Double> AS_RD_P3 { get; set; }
        public Nullable<Double> AS_RD_Q4 { get; set; }
        public Nullable<Double> AS_RD_P4 { get; set; }
        public Nullable<Double> AS_RD_Q5 { get; set; }
        public Nullable<Double> AS_RD_P5 { get; set; }
        public Nullable<Double> AS_FR_Q1 { get; set; }
        public Nullable<Double> AS_FR_P1 { get; set; }
        public Nullable<Double> AS_FR_Q2 { get; set; }
        public Nullable<Double> AS_FR_P2 { get; set; }
        public Nullable<Double> AS_FR_Q3 { get; set; }
        public Nullable<Double> AS_FR_P3 { get; set; }
        public Nullable<Double> AS_FR_Q4 { get; set; }
        public Nullable<Double> AS_FR_P4 { get; set; }
        public Nullable<Double> AS_FR_Q5 { get; set; }
        public Nullable<Double> AS_FR_P5 { get; set; }
        public Nullable<Double> AS_DR_Q1 { get; set; }
        public Nullable<Double> AS_DR_P1 { get; set; }
        public Nullable<Double> AS_DR_Q2 { get; set; }
        public Nullable<Double> AS_DR_P2 { get; set; }
        public Nullable<Double> AS_DR_Q3 { get; set; }
        public Nullable<Double> AS_DR_P3 { get; set; }
        public Nullable<Double> AS_DR_Q4 { get; set; }
        public Nullable<Double> AS_DR_P4 { get; set; }
        public Nullable<Double> AS_DR_Q5 { get; set; }
        public Nullable<Double> AS_DR_P5 { get; set; }
        public Nullable<Double> RampQuantity1 { get; set; }
        public Nullable<Double> RRU1 { get; set; }
        public Nullable<Double> RRD1 { get; set; }
        public Nullable<Double> RampQuantity2 { get; set; }
        public Nullable<Double> RRU2 { get; set; }
        public Nullable<Double> RRD2 { get; set; }
        public Nullable<Double> RampQuantity3 { get; set; }
        public Nullable<Double> RRU3 { get; set; }
        public Nullable<Double> RRD3 { get; set; }
        public Nullable<Double> RampQuantity4 { get; set; }
        public Nullable<Double> RRU4 { get; set; }
        public Nullable<Double> RRD4 { get; set; }
        public Nullable<Double> RampQuantity5 { get; set; }
        public Nullable<Double> RRU5 { get; set; }
        public Nullable<Double> RRD5 { get; set; }
        public Nullable<bool> IsCopied { get; set; }
        public Nullable<bool> IsEdit { get; set; }
        public Nullable<bool> IsHovered { get; set; }
        public string TransID { get; set; }
        public string DateTimeSubmitted { get; set; }
        public string UploadedBy { get; set; }
        public string ControlMode { get; set; }
    }

    public class RampRateDB
    {
        //public List<OfferDB> Offers { get; set; }
        public string UnitNumber { get; set; }
        public DateTime DateSchedule { get; set; }
        public Nullable<DateTime> DateTimeSubmitted { get; set; }
        public string TransID { get; set; }
        public Nullable<Double> RRMW1 { get; set; }
        public Nullable<Double> RRMW2 { get; set; }
        public Nullable<Double> RRMW3 { get; set; }
        public Nullable<Double> RRMW4 { get; set; }
        public Nullable<Double> RRMW5 { get; set; }
        public Nullable<Double> RRRU1 { get; set; }
        public Nullable<Double> RRRU2 { get; set; }
        public Nullable<Double> RRRU3 { get; set; }
        public Nullable<Double> RRRU4 { get; set; }
        public Nullable<Double> RRRU5 { get; set; }
        public Nullable<Double> RRRD1 { get; set; }
        public Nullable<Double> RRRD2 { get; set; }
        public Nullable<Double> RRRD3 { get; set; }
        public Nullable<Double> RRRD4 { get; set; }
        public Nullable<Double> RRRD5 { get; set; }
        public string UploadedBy { get; set; }
    }

    public class OfferDB
    {
        public string UnitNumber { get; set; }
        public DateTime DateSchedule { get; set; }
        public int Interval { get; set; }
        public string ResourceID { get; set; }
        public string ProducType { get; set; }
        public Nullable<Double> P1 { get; set; }
        public Nullable<Double> Q1 { get; set; }
        public Nullable<Double> P2 { get; set; }
        public Nullable<Double> Q2 { get; set; }
        public Nullable<Double> P3 { get; set; }
        public Nullable<Double> Q3 { get; set; }
        public Nullable<Double> P4 { get; set; }
        public Nullable<Double> Q4 { get; set; }
        public Nullable<Double> P5 { get; set; }
        public Nullable<Double> Q5 { get; set; }
        public Nullable<Double> P6 { get; set; }
        public Nullable<Double> Q6 { get; set; }
        public Nullable<Double> P7 { get; set; }
        public Nullable<Double> Q7 { get; set; }
        public Nullable<Double> P8 { get; set; }
        public Nullable<Double> Q8 { get; set; }
        public Nullable<Double> P9 { get; set; }
        public Nullable<Double> Q9 { get; set; }
        public Nullable<Double> P10 { get; set; }
        public Nullable<Double> Q10 { get; set; }
        public Nullable<Double> P11 { get; set; }
        public Nullable<Double> Q11 { get; set; }
        public Nullable<Double> AS_RU_Q1 { get; set; }
        public Nullable<Double> AS_RU_P1 { get; set; }
        public Nullable<Double> AS_RU_Q2 { get; set; }
        public Nullable<Double> AS_RU_P2 { get; set; }
        public Nullable<Double> AS_RU_Q3 { get; set; }
        public Nullable<Double> AS_RU_P3 { get; set; }
        public Nullable<Double> AS_RU_Q4 { get; set; }
        public Nullable<Double> AS_RU_P4 { get; set; }
        public Nullable<Double> AS_RU_Q5 { get; set; }
        public Nullable<Double> AS_RU_P5 { get; set; }
        public Nullable<Double> AS_RD_Q1 { get; set; }
        public Nullable<Double> AS_RD_P1 { get; set; }
        public Nullable<Double> AS_RD_Q2 { get; set; }
        public Nullable<Double> AS_RD_P2 { get; set; }
        public Nullable<Double> AS_RD_Q3 { get; set; }
        public Nullable<Double> AS_RD_P3 { get; set; }
        public Nullable<Double> AS_RD_Q4 { get; set; }
        public Nullable<Double> AS_RD_P4 { get; set; }
        public Nullable<Double> AS_RD_Q5 { get; set; }
        public Nullable<Double> AS_RD_P5 { get; set; }
        public Nullable<Double> AS_FR_Q1 { get; set; }
        public Nullable<Double> AS_FR_P1 { get; set; }
        public Nullable<Double> AS_FR_Q2 { get; set; }
        public Nullable<Double> AS_FR_P2 { get; set; }
        public Nullable<Double> AS_FR_Q3 { get; set; }
        public Nullable<Double> AS_FR_P3 { get; set; }
        public Nullable<Double> AS_FR_Q4 { get; set; }
        public Nullable<Double> AS_FR_P4 { get; set; }
        public Nullable<Double> AS_FR_Q5 { get; set; }
        public Nullable<Double> AS_FR_P5 { get; set; }
        public Nullable<Double> AS_DR_Q1 { get; set; }
        public Nullable<Double> AS_DR_P1 { get; set; }
        public Nullable<Double> AS_DR_Q2 { get; set; }
        public Nullable<Double> AS_DR_P2 { get; set; }
        public Nullable<Double> AS_DR_Q3 { get; set; }
        public Nullable<Double> AS_DR_P3 { get; set; }
        public Nullable<Double> AS_DR_Q4 { get; set; }
        public Nullable<Double> AS_DR_P4 { get; set; }
        public Nullable<Double> AS_DR_Q5 { get; set; }
        public Nullable<Double> AS_DR_P5 { get; set; }
        public string ControlMode { get; set; }
    }

    public class BidDataTable
    {
        public DateTime Date { get; set; }
        public int Hour { get; set; }
        public Nullable<Double> P1 { get; set; }
        public Nullable<Double> Q1 { get; set; }
        public Nullable<Double> P2 { get; set; }
        public Nullable<Double> Q2 { get; set; }
        public Nullable<Double> P3 { get; set; }
        public Nullable<Double> Q3 { get; set; }
        public Nullable<Double> P4 { get; set; }
        public Nullable<Double> Q4 { get; set; }
        public Nullable<Double> P5 { get; set; }
        public Nullable<Double> Q5 { get; set; }
        public Nullable<Double> P6 { get; set; }
        public Nullable<Double> Q6 { get; set; }
        public Nullable<Double> P7 { get; set; }
        public Nullable<Double> Q7 { get; set; }
        public Nullable<Double> P8 { get; set; }
        public Nullable<Double> Q8 { get; set; }
        public Nullable<Double> P9 { get; set; }
        public Nullable<Double> Q9 { get; set; }
        public Nullable<Double> P10 { get; set; }
        public Nullable<Double> Q10 { get; set; }
        public Nullable<Double> P11 { get; set; }
        public Nullable<Double> Q11 { get; set; }
        public Nullable<Double> AS_RU_Q1 { get; set; }
        public Nullable<Double> AS_RU_P1 { get; set; }
        public Nullable<Double> AS_RU_Q2 { get; set; }
        public Nullable<Double> AS_RU_P2 { get; set; }
        public Nullable<Double> AS_RU_Q3 { get; set; }
        public Nullable<Double> AS_RU_P3 { get; set; }
        public Nullable<Double> AS_RU_Q4 { get; set; }
        public Nullable<Double> AS_RU_P4 { get; set; }
        public Nullable<Double> AS_RU_Q5 { get; set; }
        public Nullable<Double> AS_RU_P5 { get; set; }
        public Nullable<Double> AS_RD_Q1 { get; set; }
        public Nullable<Double> AS_RD_P1 { get; set; }
        public Nullable<Double> AS_RD_Q2 { get; set; }
        public Nullable<Double> AS_RD_P2 { get; set; }
        public Nullable<Double> AS_RD_Q3 { get; set; }
        public Nullable<Double> AS_RD_P3 { get; set; }
        public Nullable<Double> AS_RD_Q4 { get; set; }
        public Nullable<Double> AS_RD_P4 { get; set; }
        public Nullable<Double> AS_RD_Q5 { get; set; }
        public Nullable<Double> AS_RD_P5 { get; set; }
        public Nullable<Double> AS_FR_Q1 { get; set; }
        public Nullable<Double> AS_FR_P1 { get; set; }
        public Nullable<Double> AS_FR_Q2 { get; set; }
        public Nullable<Double> AS_FR_P2 { get; set; }
        public Nullable<Double> AS_FR_Q3 { get; set; }
        public Nullable<Double> AS_FR_P3 { get; set; }
        public Nullable<Double> AS_FR_Q4 { get; set; }
        public Nullable<Double> AS_FR_P4 { get; set; }
        public Nullable<Double> AS_FR_Q5 { get; set; }
        public Nullable<Double> AS_FR_P5 { get; set; }
        public Nullable<Double> AS_DR_Q1 { get; set; }
        public Nullable<Double> AS_DR_P1 { get; set; }
        public Nullable<Double> AS_DR_Q2 { get; set; }
        public Nullable<Double> AS_DR_P2 { get; set; }
        public Nullable<Double> AS_DR_Q3 { get; set; }
        public Nullable<Double> AS_DR_P3 { get; set; }
        public Nullable<Double> AS_DR_Q4 { get; set; }
        public Nullable<Double> AS_DR_P4 { get; set; }
        public Nullable<Double> AS_DR_Q5 { get; set; }
        public Nullable<Double> AS_DR_P5 { get; set; }
        public string RampRate { get; set; }
        public Nullable<Double> MW { get; set; }
        public Nullable<Double> RU { get; set; }
        public Nullable<Double> RD { get; set; }
        public string ControlMode { get; set; }
    }

    public class WebServiceSet
    {
        public int BidID { get; set; }
        public int UnitID { get; set; }
        public string Operation { get; set; }
        public string CertificateURL { get; set; }
        public string Password { get; set; }
        public string FriendlyName { get; set; }
        public string TpUser { get; set; }
        public string BidType { get; set; }
        public string FileLocation { get; set; }
        public string SheetName { get; set; }
        public Boolean IsActive { get; set; }
    }

    public class WebServiceUrl
    {
        public int WebID { get; set; }
        public string URL { get; set; }
        public string Type { get; set; }
        public Boolean IsActive { get; set; }
    }

    public class SQLData
    {
        public Nullable<DateTime> Timestamp { get; set; }
        public string DataType { get; set; }
        public string Unit { get; set; }
        public Nullable<double> Quantity { get; set; }
        public Nullable<double> Price { get; set; }
    }

    public class BidUpload
    {
        public string BidType { get; set; }
        public string UnitID { get; set; }
        public string UnitNumber { get; set; }
        public List<Bid> Offers { get; set; }
        public string ControlMode { get; set; }
    }

    public class RRStandard
    {
        public string UnitNumber { get; set; }
        public Nullable<double> RRUp { get; set; }
        public Nullable<double> RRDown { get; set; }
        public Nullable<double> RRMax { get; set; }
        public Nullable<double> PMax { get; set; }
    }
    

    public class UserLog
    {
        public Nullable<int> LogID { get; set; }
        public Nullable<int> UserID { get; set; }
        public int ModuleID { get; set; }
        public DateTime LogTime { get; set; }
        public string Action { get; set; }
    }

    public class LoginLog
    {
        public Nullable<int> UserID { get; set; }
        public string Action { get; set; }
    }

    public class UnitPerSiteName
    {
        public int UnitID { get; set; }
        public string UnitNumber { get; set; }
        public string SiteName { get; set; }
    }

    public class BidOfferControlMode
    {
        public string UnitNumber { get; set; }
        public DateTime DateSchedule { get; set; }
        public int Interval { get; set; }
        public string ControlMode { get; set; }
    }
}