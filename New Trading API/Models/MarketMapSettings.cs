using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace New_Trading_API.Models
{
    public class MarketMapSettings
    {

    }

    public class FtpConfiguration
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string IPAddress { get; set; }
        public string UFLRootPath { get; set; }
        public string LocalRootPath { get; set; }
        public bool IsActive { get; set; }
    }

    public class FTPFiles
    {
        public int ID { get; set; }
        public Nullable<int> FTPConfigID { get; set; }
        public string IPAddress { get; set; }
        public string FilePath { get; set; }
        public string UploadFilePath { get; set; }
        public string FilePrefix { get; set; }
        public string FileFormat { get; set; }
        public string FileSuffix { get; set; }
        public string ExtensionName { get; set; }
        public string SheetName { get; set; }
        public Nullable<int> FileperDay { get; set; }
        public string UFLPath { get; set; }
        public string DownloadPath { get; set; }
        public bool IsActive { get; set; }
        public string FTPFile { get; set; }
        public int ServiceSetupID { get; set; }
        public int ServiceID { get; set; }
        public string ServiceName { get; set; }

    }

    public class FtpService
    {
        public int ID { get; set; }
        public string ServiceName { get; set; }
        public Nullable<int> Interval { get; set; }
        public Nullable<bool> IsActive { get; set; }
    }

    public class FtpServiceMessage
    {
        public string message { get; set; }
        public FtpService FtpService { get; set; }
    }

    public class Logs
    {
        public int ID { get; set; }
        public Nullable<int> ServiceID { get; set; }
        public string Service { get; set; }
        public Nullable<int> FileID { get; set; }
        public string FileName { get; set; }
        public string SheetName { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public string LoggedDate { get; set; }
        public Nullable<bool> IsEmailed { get; set; }
        public Nullable<System.DateTime> EmailedDate { get; set; }
    }
    
 }