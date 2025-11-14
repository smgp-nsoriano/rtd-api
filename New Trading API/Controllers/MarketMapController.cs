using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using New_Trading_API.Models;
using System.Data.SqlClient;
using System.Data;

namespace New_Trading_API.Controllers
{
    public class MarketMapController : ApiController
    {
        string Query = "";

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetFTPInfo()
        {
            List<FtpConfiguration> ftpList = new List<FtpConfiguration>();
            using (CAMSEntities db = new CAMSEntities())
            {
                Query = @"
                        select * from [m_FTPConfiguration]
                        where isactive = 1
                        ";
                ftpList = db.Database
                    .SqlQuery<FtpConfiguration>(Query)
                    .ToList<FtpConfiguration>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, ftpList.ToList());
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult SaveFTPConfiguration([FromBody] m_FTPConfiguration model)
        {

            if (model.IPAddress == "" || model.IPAddress == null)
            {
                return Content(HttpStatusCode.BadRequest, "IP Address is required");
            }
            else if (model.LocalRootPath == "" || model.LocalRootPath == null)
            {
                return Content(HttpStatusCode.BadRequest, "Local Root Path is required");
            }
            else if (model.Username == "" || model.Username == null)
            {
                return Content(HttpStatusCode.BadRequest, "Username is required");
            }
            else if (model.Password == "" || model.Password == null)
            {
                return Content(HttpStatusCode.BadRequest, "Password is required");
            }
            else if (model.UFLRootPath == "" || model.UFLRootPath == null)
            {
                return Content(HttpStatusCode.BadRequest, "UFL Root Path is required");
            }

            int transID = 0;
            FtpConfiguration ftp = new FtpConfiguration();
            if (model.ID == 0)
            {
                using (CAMSEntities db = new CAMSEntities())
                {
                    var ftpConfig = new m_FTPConfiguration()
                    {
                        Username = model.Username
                        ,
                        Password = model.Password
                        ,
                        IPAddress = model.IPAddress
                        ,
                        LocalRootPath = model.LocalRootPath
                        ,
                        UFLRootPath = model.UFLRootPath
                        ,
                        IsActive = true
                    };
                    db.m_FTPConfiguration.Add(ftpConfig);
                    db.SaveChanges();

                    transID = ftpConfig.ID;
                }
            }
            else
            {
                using (CAMSEntities db = new CAMSEntities())
                {
                    var tbl = db.m_FTPConfiguration.Where(x => x.ID == model.ID).FirstOrDefault<m_FTPConfiguration>();
                    tbl.IPAddress = model.IPAddress;
                    tbl.LocalRootPath = model.LocalRootPath;
                    tbl.UFLRootPath = model.UFLRootPath;
                    tbl.Username = model.Username;
                    tbl.Password = model.Password;
                    db.SaveChanges();
                }
                transID = model.ID;
            }

            using (CAMSEntities db = new CAMSEntities())
            {
                Query = @"
                        select * from [m_FTPConfiguration]
                        where isactive = 1 and ID = " + transID;
                ftp = db.Database
                    .SqlQuery<FtpConfiguration>(Query)
                    .FirstOrDefault<FtpConfiguration>();
            }

            return Content(HttpStatusCode.OK, ftp);
        }

        [HttpDelete]
        [Authorize]
        [Route("MarketMap/DeleteFTPConfiguration/{ID}")]
        public IHttpActionResult DeleteFTPConfiguration(int ID)
        {
            using (CAMSEntities db = new CAMSEntities())
            {
                var tbl = db.m_FTPConfiguration.Where(x => x.ID == ID).FirstOrDefault<m_FTPConfiguration>();
                tbl.IsActive = false;
                db.SaveChanges();
            }
            return Ok();
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetFTPFiles()
        {
            var ftpFiles = new List<FTPFiles>();
            using (CAMSEntities db = new CAMSEntities())
            {
                Query = @"
                        SELECT a.[ID]
                          ,[FTPConfigID]
	                      ,b.IPAddress
                          ,[FilePath]
                          ,[UploadFilePath]
                          ,isnull([FilePrefix],'')[FilePrefix]
                          ,isnull([FileFormat],'')[FileFormat]
                          ,isnull([FileSuffix],'')[FileSuffix]
                          ,isnull([ExtensionName],'')[ExtensionName]
                          ,[SheetName]
                          ,[FileperDay]
                          ,[UFLPath]
                          ,[DownloadPath]
                          ,a.IsActive
	                      ,FTPFile
						  ,c.ID ServiceSetupID
						  ,c.ServiceID
						  ,d.ServiceName
                      FROM [m_FTPFiles] a
                      left join m_FTPConfiguration b on a.FTPConfigID = b.ID
					  inner join m_ServiceFTPFileSetup c on c.FileID = a.ID and c.IsActive = 1
					  left join m_Services d on c.ServiceID = d.ID and d.IsActive = 1
                      where a.IsActive = 1
                        ";
                ftpFiles = db.Database
                    .SqlQuery<FTPFiles>(Query)
                    .ToList<FTPFiles>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, ftpFiles.ToList());
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult SaveFile([FromBody] FTPFiles model)
        {
            var ftp = new FTPFiles();
            int transId = 0;

            if (model.FTPConfigID == 0 || model.FTPConfigID == null)
            {
                return Content(HttpStatusCode.BadRequest, new { message = "FTP Configuration is Required!" });
            }
            else if (model.FilePath == "" || model.FilePath == null)
            {
                return Content(HttpStatusCode.BadRequest, new { message = "FTP File Path is Required!" });
            }
            else if (model.ExtensionName == "" || model.ExtensionName == null)
            {
                return Content(HttpStatusCode.BadRequest, new { message = "FTP File Extension is Required!" });
            }
            else if (model.FileperDay == 0 || model.FileperDay == null)
            {
                return Content(HttpStatusCode.BadRequest, new { message = "File Per Day is Required!" });
            }

            using (CAMSEntities db = new CAMSEntities())
            {
                if (model.ID == 0)
                {
                    var tbl = db.m_FTPFiles
                        .Where(x => x.FilePrefix == model.FilePrefix
                                && x.FileFormat == model.FileFormat
                                && x.FileSuffix == model.FileSuffix
                                && x.FTPConfigID == model.FTPConfigID
                                && x.ExtensionName == model.ExtensionName
                                && x.FilePath == model.FilePath)
                        .FirstOrDefault<m_FTPFiles>();

                    if (tbl == null)
                    {
                        var ftpfile = new m_FTPFiles()
                        {
                            IsActive = true,
                            DownloadPath = @"archive\",
                            ExtensionName = model.ExtensionName,
                            FileFormat = model.FileFormat,
                            FilePath = model.FilePath,
                            FileperDay = model.FileperDay,
                            FilePrefix = model.FilePrefix,
                            FileSuffix = model.FileSuffix,
                            UploadFilePath = model.UploadFilePath,
                            FTPConfigID = model.FTPConfigID,
                            UFLPath = model.UFLPath,
                        };
                        db.m_FTPFiles.Add(ftpfile);
                        db.SaveChanges();

                        transId = ftpfile.ID;

                        var ftpSetup = new m_ServiceFTPFileSetup()
                        {
                            FileID = transId,
                            ServiceID = model.ServiceID,
                            IsActive = true
                        };
                        db.m_ServiceFTPFileSetup.Add(ftpSetup);
                        db.SaveChanges();
                    }
                    else
                    {
                        return Content(HttpStatusCode.BadRequest, new { message = "Record Already Exsiting!" });
                    }
                }
                else
                {
                    var tbl = db.m_FTPFiles
                        .Where(x => x.FilePrefix == model.FilePrefix
                                && x.FileFormat == model.FileFormat
                                && x.FileSuffix == model.FileSuffix
                                && x.FTPConfigID == model.FTPConfigID
                                && x.ExtensionName == model.ExtensionName
                                && x.FilePath == model.FilePath
                                && x.ID != model.ID)
                        .FirstOrDefault<m_FTPFiles>();

                    if (tbl == null)
                    {
                        var ftpfile = db.m_FTPFiles
                            .Where(x => x.ID == model.ID)
                            .FirstOrDefault<m_FTPFiles>();

                        ftpfile.ExtensionName = model.ExtensionName;
                        ftpfile.FileFormat = model.FileFormat;
                        ftpfile.FilePath = model.FilePath;
                        ftpfile.FileperDay = model.FileperDay;
                        ftpfile.FilePrefix = model.FilePrefix;
                        ftpfile.FileSuffix = model.FileSuffix;
                        ftpfile.UploadFilePath = model.FilePath;
                        ftpfile.FTPConfigID = model.FTPConfigID;
                        ftpfile.UFLPath = model.UFLPath;
                        db.SaveChanges();
                        transId = model.ID;

                        var ftpSetup = db.m_ServiceFTPFileSetup
                            .Where(x => x.ID == model.ServiceSetupID)
                            .FirstOrDefault<m_ServiceFTPFileSetup>();
                        ftpSetup.ServiceID = model.ServiceID;
                        db.SaveChanges();
                    }
                    else
                    {
                        return Content(HttpStatusCode.BadRequest, new { message = "Record Already Exsiting!" });
                    }
                }

                Query = @"
                        SELECT top 1 a.[ID]
                          ,[FTPConfigID]
	                      ,b.IPAddress
                          ,[FilePath]
                          ,[UploadFilePath]
                          ,isnull([FilePrefix],'')[FilePrefix]
                          ,isnull([FileFormat],'')[FileFormat]
                          ,isnull([FileSuffix],'')[FileSuffix]
                          ,isnull([ExtensionName],'')[ExtensionName]
                          ,[SheetName]
                          ,[FileperDay]
                          ,[UFLPath]
                          ,[DownloadPath]
                          ,a.IsActive
	                      ,FTPFile
						  ,c.ID ServiceSetupID
						  ,c.ServiceID
						  ,d.ServiceName
                      FROM [m_FTPFiles] a
                      left join m_FTPConfiguration b on a.FTPConfigID = b.ID
					  inner join m_ServiceFTPFileSetup c on c.FileID = a.ID and c.IsActive = 1
					  left join m_Services d on c.ServiceID = d.ID and d.IsActive = 1
                      where a.IsActive = 1 and a.ID =" + transId;
                ftp = db.Database
                    .SqlQuery<FTPFiles>(Query)
                    .FirstOrDefault<FTPFiles>();
            }

            return Content(HttpStatusCode.OK, ftp);
        }

        [HttpGet]
        [Authorize]
        [Route("MarketMap/GetFTPFileSheet/{ftpId}")]
        public HttpResponseMessage GetFTPFileSheet(int ftpId)
        {
            var ftpFiles = new List<m_FTPFileSheetName>();
            using (CAMSEntities db = new CAMSEntities())
            {
                Query = @"
                        SELECT [ID]
                          ,[FTPFileID]
                          ,[SheetName]
                          ,[FunctionName]
                          ,[IsActive]
                      FROM [m_FTPFileSheetName]
                      where FTPFileID = 
                        " + ftpId;
                ftpFiles = db.Database
                    .SqlQuery<m_FTPFileSheetName>(Query)
                    .ToList<m_FTPFileSheetName>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, ftpFiles.ToList());
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult SaveFileSheet([FromBody] m_FTPFileSheetName model)
        {

            if (model.SheetName == "" || model.SheetName == null)
            {
                return Content(HttpStatusCode.BadRequest, new { message = "Sheet Name is Required!" });
            }
            else if (model.FunctionName == "" || model.FunctionName == null)
            {
                return Content(HttpStatusCode.BadRequest, new { message = "Function Name is Required!" });
            }

            int transID = 0;
            var ftp = new m_FTPFileSheetName();
            if (model.ID == 0)
            {
                using (CAMSEntities db = new CAMSEntities())
                {
                    var tbl = db.m_FTPFileSheetName
                        .Where(x => x.SheetName == model.SheetName
                                    && x.FTPFileID == model.FTPFileID
                                    && x.IsActive == true)
                        .FirstOrDefault<m_FTPFileSheetName>();

                    if (tbl == null)
                    {
                        var ftpSheet = new m_FTPFileSheetName()
                        {
                            SheetName = model.SheetName
                            ,
                            FunctionName = model.FunctionName
                            ,
                            FTPFileID = model.FTPFileID
                            ,
                            IsActive = true
                        };
                        db.m_FTPFileSheetName.Add(ftpSheet);
                        db.SaveChanges();

                        transID = ftpSheet.ID;
                    }
                    else
                    {
                        return Content(HttpStatusCode.BadRequest, new { message = "Record Already Existing!" });
                    }
                }
            }
            else
            {
                using (CAMSEntities db = new CAMSEntities())
                {
                    var tblCheck = db.m_FTPFileSheetName
                        .Where(x => x.SheetName == model.SheetName
                                    && x.FTPFileID == model.FTPFileID
                                    && x.ID != model.ID
                                    && x.IsActive == true)
                        .FirstOrDefault<m_FTPFileSheetName>();

                    if (tblCheck == null)
                    {
                        var tbl = db.m_FTPFileSheetName.Where(x => x.ID == model.ID).FirstOrDefault<m_FTPFileSheetName>();
                        tbl.SheetName = model.SheetName;
                        tbl.FunctionName = model.FunctionName;
                        db.SaveChanges();
                        transID = model.ID;
                    }
                    else
                    {
                        return Content(HttpStatusCode.BadRequest, new { message = "Record Already Existing!" });
                    }
                }
            }

            using (CAMSEntities db = new CAMSEntities())
            {
                Query = @"
                        SELECT [ID]
                          ,[FTPFileID]
                          ,[SheetName]
                          ,[FunctionName]
                          ,[IsActive]
                      FROM [m_FTPFileSheetName]
                      where ID=" + transID;
                ftp = db.Database
                    .SqlQuery<m_FTPFileSheetName>(Query)
                    .FirstOrDefault<m_FTPFileSheetName>();
            }

            return Content(HttpStatusCode.OK, ftp);
        }

        [HttpDelete]
        [Authorize]
        [Route("MarketMap/DeleteFile/{ID}")]
        public IHttpActionResult DeleteFile(int ID)
        {
            using (CAMSEntities db = new CAMSEntities())
            {
                var tbl = db.m_FTPFiles.Where(x => x.ID == ID).FirstOrDefault<m_FTPFiles>();
                tbl.IsActive = false;
                var tblSetup = db.m_ServiceFTPFileSetup.Where(x => x.FileID == ID).FirstOrDefault<m_ServiceFTPFileSetup>();
                tblSetup.IsActive = false;
                db.SaveChanges();
            }
            return Ok();
        }

        [HttpDelete]
        [Authorize]
        [Route("MarketMap/DeleteFileSheet/{ID}")]
        public IHttpActionResult DeleteFileSheet(int ID)
        {
            using (CAMSEntities db = new CAMSEntities())
            {
                var tbl = db.m_FTPFileSheetName.Where(x => x.ID == ID).FirstOrDefault<m_FTPFileSheetName>();
                tbl.IsActive = false;
                db.SaveChanges();
            }
            return Ok();
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetSingleFTPFileSheet(int Id)
        {
            var ftpFiles = new m_FTPFileSheetName();
            using (CAMSEntities db = new CAMSEntities())
            {
                Query = @"
                        SELECT [ID]
                          ,[FTPFileID]
                          ,[SheetName]
                          ,[FunctionName]
                          ,[IsActive]
                      FROM [m_FTPFileSheetName]
                      where ID = 
                        " + Id;
                ftpFiles = db.Database
                    .SqlQuery<m_FTPFileSheetName>(Query)
                    .FirstOrDefault<m_FTPFileSheetName>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, ftpFiles);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetFtpServices()
        {
            var ftpService = new List<FtpService>();
            using (CAMSEntities db = new CAMSEntities())
            {
                Query = @"
                        SELECT [ID]
                              ,[ServiceName]
                              ,[Interval]
                              ,[IsActive]
                          FROM [m_Services]
                          where IsActive = 1
                        ";
                ftpService = db.Database
                    .SqlQuery<FtpService>(Query)
                    .ToList<FtpService>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, ftpService.ToList());
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult SaveFTPService([FromBody] m_Services model)
        {

            if (model.ServiceName == "" || model.ServiceName == null)
            {
                return Content(HttpStatusCode.BadRequest, "Service Name is Required!");
            }

            int transID = 0;
            FtpService ftp = new FtpService();
            if (model.ID == 0)
            {
                using (CAMSEntities db = new CAMSEntities())
                {
                    var tbl = db.m_Services.Where(x => x.ServiceName == model.ServiceName && x.IsActive == true).FirstOrDefault<m_Services>();

                    if (tbl == null)
                    {
                        var ftpService = new m_Services()
                        {
                            ServiceName = model.ServiceName
                            ,
                            Interval = model.Interval
                            ,
                            IsActive = true
                        };
                        db.m_Services.Add(ftpService);
                        db.SaveChanges();

                        transID = ftpService.ID;
                    }
                    else
                    {
                        return Content(HttpStatusCode.BadRequest, new { message = "Record Already Existing!" });
                    }

                }
            }
            else
            {
                using (CAMSEntities db = new CAMSEntities())
                {
                    var tblCheck = db.m_Services.Where(x => x.ServiceName == model.ServiceName && x.ID != model.ID && x.IsActive == true).FirstOrDefault<m_Services>();
                    if (tblCheck == null)
                    {
                        var tbl = db.m_Services.Where(x => x.ID == model.ID).FirstOrDefault<m_Services>();
                        tbl.ServiceName = model.ServiceName;
                        tbl.Interval = model.Interval;
                        db.SaveChanges();

                        transID = model.ID;
                    }
                    else
                    {
                        return Content(HttpStatusCode.BadRequest, new { message = "Record Already Existing!" });
                    }
                }
            }

            using (CAMSEntities db = new CAMSEntities())
            {
                Query = @"
                        SELECT [ID]
                              ,[ServiceName]
                              ,[Interval]
                              ,[IsActive]
                          FROM [m_Services]
                          where IsActive = 1 and ID=" + transID;
                ftp = db.Database
                    .SqlQuery<FtpService>(Query)
                    .FirstOrDefault<FtpService>();
            }

            return Content(HttpStatusCode.OK, ftp);
        }

        [HttpDelete]
        [Authorize]
        [Route("MarketMap/DeleteFTPService/{ID}")]
        public IHttpActionResult DeleteFTPService(int ID)
        {
            using (CAMSEntities db = new CAMSEntities())
            {
                var tbl = db.m_Services.Where(x => x.ID == ID).FirstOrDefault<m_Services>();
                tbl.IsActive = false;
                db.SaveChanges();
            }
            return Ok();
        }

        [HttpGet]
        [Authorize]
        [Route("MarketMap/GetLogs/{date}")]
        public HttpResponseMessage GetLogs(DateTime date)
        {
            var logList = new List<Logs>();
            using (CAMSEntities db = new CAMSEntities())
            {
                Query = @"
                        select [ID]
                              ,[ServiceID]
                              ,[Service]
                              ,[FileID]
                              ,[FileName]
                              ,[SheetName]
                              ,[Type]
                              ,[Message]
                              ,convert(varchar,LoggedDate,0) LoggedDate 
                        from t_Logs
                        where convert(date,LoggedDate) = convert(date,'" + date + @"')
                        order by ID desc
                        ";

                logList = db.Database
                    .SqlQuery<Logs>(Query)
                    .ToList<Logs>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, logList);
        }
    }
}