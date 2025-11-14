using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using New_Trading_API.Models;
using System.Security.Claims;
using System.Data.SqlClient;
using System.Data;
using System.Net.Mail;
using System.Text;

namespace New_Trading_API.Controllers
{
    //[Authorize]
    public class UserManagementController : ApiController
    {
        string StrQuery = "";

        private string Query
        {
            get
            {
                return @"SELECT [UserID]
                      ,[UserName]
                      ,[Password]
                      ,[FirstName]
                      ,[LastName]
                      ,[EmailAddress]
                      ,a.[AccountTypeID] UserTypeID
                      ,b.AccountType UserType
	                  ,a.[IsActive]
                      ,a.[CreatedBy]
                      ,a.[CreationDate]
                      ,a.[ModifiedBy]
                      ,a.[ModificationDate]
                  FROM [a_UserAccount] a
                  left join a_UserAccountType b on a.AccountTypeID = b.AccountTypeID
                  where a.isactive = 1";
            }
        }

        private DataTable RetrivedRecord(int id)
        {
            StrQuery = Query + " and UserID = @id";
            var userID = new SqlParameter("@id", id);

          return  GlobalFunctions.DataReader(StrQuery, userID);
        }

        private void sendEmailViaWebApi()
        {

            MailMessage mailMessage = new MailMessage("calibr8systeminc@gmail.com", "vincent.dagdag@calibr8.com.ph");
            mailMessage.Subject = "Change password";
            mailMessage.Body = "";

            SmtpClient smtpClient = new SmtpClient("192.168.10.3", 25);
            smtpClient.Credentials = new System.Net.NetworkCredential()
            {
                UserName = "calibr8systeminc@gmail.com",
                Password = "C8@dm1ns!"
            };

            smtpClient.Send(mailMessage);
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult SendEmail()
        {
            sendEmailViaWebApi();

            return Ok();
        }

        // GET UserManagement/GetUserInfo
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetUserInfo()
        {
            List<User> userList = new List<User>();
            StrQuery = @"SELECT [UserID]
                      ,[UserName]
                      ,[Password]
                      ,[FirstName]
                      ,[LastName]
                      ,[EmailAddress]
                      ,a.[AccountTypeID] UserTypeID
                      ,b.AccountType UserType
	                  ,a.[IsActive]
                      ,a.[CreatedBy]
                      ,a.[CreationDate]
                      ,a.[ModifiedBy]
                      ,a.[ModificationDate]
                  FROM [a_UserAccount] a
                  left join a_UserAccountType b on a.AccountTypeID = b.AccountTypeID
                  order by UserID desc";

            using (TradingEntities db = new TradingEntities())
            {
                userList = db.Database
                    .SqlQuery<User>(StrQuery)
                    .ToList<User>();
            }
            
            return Request.CreateResponse(HttpStatusCode.OK, userList);
            
        }

        // GET UserManagement/GetCurrentUser
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetCurrentUser()
        {
            var identity = (ClaimsIdentity)User.Identity;
            var username = identity.FindFirst("UserName").Value;
            var role = identity.FindFirst("AccountType").Value;
            var userid = identity.FindFirst("UserID").Value;

            return Request.CreateResponse(HttpStatusCode.OK, 
                new { UserName = username,
                      UserType = role,
                      UserId = userid});
        }

        // GET UserManagement/GetCurrentUserInfoWithPermission
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetCurrentUserInfoWithPermission()
        {
            User user = new User();
            StrQuery = @"SELECT top 1 [UserID]
                      ,[UserName]
                      ,[Password]
                      ,[FirstName]
                      ,[LastName]
                      ,[EmailAddress]
					  ,c.IsShowBid
					  ,c.IsShowConfig
					  ,c.IsShowPrice
                      ,c.IsShowUserConfig
                      ,c.IsManualEntry
                      ,isnull(c.IsAlarmDisable,0)IsAlarmDisable
                      ,isnull(c.IsCamSetup,0)IsCamSetup
                      ,isnull(isOperator,0)isOperator
                      ,isnull(IsOverride,0)IsOverride
                      ,isnull(IsPortfolioOnly,0)IsPortfolioOnly
                      ,isnull(isPbReason,0)IsPbReason
                      ,isnull(IsMobileAccess,0)IsMobileAccess
                      ,a.[AccountTypeID] UserTypeID
                      ,b.AccountType UserType
	                  ,a.[IsActive]
                      ,a.[CreatedBy]
                      ,a.[CreationDate]
                      ,a.[ModifiedBy]
                      ,a.[ModificationDate]
                      ,c.PermissionID
                      ,case when c.IsOperator = 1 
                            and isnull((select top 1 UnitID 
                                        from m_UserSiteAccess us 
                                        where us.PermissionID = c.PermissionID 
                                        and isnull(SiteID,0)= 0),0) = 0 
                            then convert(bit,1) 
                            else convert(bit,0) 
                        end IsSPDC
                  FROM [a_UserAccount] a
                  left join a_UserAccountType b on a.AccountTypeID = b.AccountTypeID
				  left join a_Permission c on b.AccountTypeID = c.UserAccountTypeID
                  where a.isactive = 1 and UserID = @userID";

            using (TradingEntities db = new TradingEntities())
            {
                var identity = (ClaimsIdentity)User.Identity;
                var userID = identity.FindFirst("UserID").Value;
                var paramUserID = new SqlParameter("@userID", userID);

                user = db.Database
                    .SqlQuery<User>(StrQuery, paramUserID).FirstOrDefault<User>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, user);

        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetCurrentUserPermission(string Username, string Password)
        {
            User user = new User();
            StrQuery = @"SELECT top 1 [UserID]
                      ,[UserName]
                      ,[Password]
                      ,[FirstName]
                      ,[LastName]
                      ,[EmailAddress]
					  ,c.IsShowBid
					  ,c.IsShowConfig
					  ,c.IsShowPrice
                      ,c.IsShowUserConfig
                      ,c.IsManualEntry
                      ,isnull(c.IsCamSetup,0)IsCamSetup
                      ,isnull(c.IsOperator,0)IsOperator
                      ,a.[AccountTypeID] UserTypeID
                      ,b.AccountType UserType
	                  ,a.[IsActive]
                      ,a.[CreatedBy]
                      ,a.[CreationDate]
                      ,a.[ModifiedBy]
                      ,a.[ModificationDate]
                  FROM [a_UserAccount] a
                  left join a_UserAccountType b on a.AccountTypeID = b.AccountTypeID
				  left join a_Permission c on b.AccountTypeID = c.UserAccountTypeID
                  where a.isactive = 1 and UserName = @username and Password = @password";

            using (TradingEntities db = new TradingEntities())
            {
                var paramUsername = new SqlParameter("@userID", Username);
                var paramPassword = new SqlParameter("@userID", Password);

                user = db.Database
                    .SqlQuery<User>(StrQuery, paramUsername, paramPassword).FirstOrDefault<User>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, user);

        }

        // GET UserManagement/GetAccountTypeList
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAccountTypeList()
        {
            List<AccountTypeAndPermission> accountTypeList = new List<AccountTypeAndPermission>();

            StrQuery = @"
                        SELECT a.[AccountTypeID]
                              ,a.[AccountType]
                              ,a.[IsActive]
	                          ,isnull((select dbo.fn_SiteAccess(b.PermissionID)),'') +
		                        case when b.IsShowPrice = 1 then ',WESM Price' else '' end + 
		                        case when b.IsShowBid = 1 then ',Bid Uploading' else '' end + 
		                        case when b.IsShowConfig = 1 then ',System Config' else '' end UserPermissions
                              ,[CreatedBy]
                              ,[CreationDate]
                              ,[ModifiedBy]
                              ,[ModificationDate]
                              ,b.PermissionID
                          into #tmp
                          FROM [a_UserAccountType] a
                          left join a_Permission b on a.AccountTypeID = b.UserAccountTypeID 
                          where a.IsActive = 1

                          select [AccountTypeID] 
		                        ,[AccountType]
		                        ,[IsActive]
		                        ,case when UserPermissions = '' then '' else right(UserPermissions,len(isnull(UserPermissions,''))-1) end UserPermissions
		                        ,[CreatedBy]
		                        ,[CreationDate]
		                        ,[ModifiedBy]
		                        ,[ModificationDate]
                                ,PermissionID
                          from #tmp

                          drop table #tmp";
            using (TradingEntities db = new TradingEntities())
            {
                accountTypeList = db.Database
                    .SqlQuery<AccountTypeAndPermission>(StrQuery)
                    .ToList<AccountTypeAndPermission>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, accountTypeList);

        }

        // GET UserManagement/GetOperatorAccountTypeList
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetOperatorAccountTypeList(int isOperator)
        {
            List<AccountTypeAndPermission> accountTypeList = new List<AccountTypeAndPermission>();

            StrQuery = $@"
                        SELECT a.[AccountTypeID]
                              ,a.[AccountType]
                              ,a.[IsActive]
	                          ,isnull((select dbo.fn_SiteAccess(b.PermissionID)),'') +
		                        case when b.IsShowPrice = 1 then ',WESM Price' else '' end + 
		                        case when b.IsShowBid = 1 then ',Bid Uploading' else '' end + 
		                        case when b.IsShowConfig = 1 then ',System Config' else '' end UserPermissions
                              ,[CreatedBy]
                              ,[CreationDate]
                              ,[ModifiedBy]
                              ,[ModificationDate]
                              ,b.PermissionID
                          into #tmp
                          FROM [a_UserAccountType] a
                          left join a_Permission b on a.AccountTypeID = b.UserAccountTypeID
                          where a.IsActive = 1 and isnull(b.IsOperator,0) = {isOperator}

                          select [AccountTypeID] 
		                        ,[AccountType]
		                        ,[IsActive]
		                        ,case when UserPermissions = '' then '' else right(UserPermissions,len(isnull(UserPermissions,''))-1) end UserPermissions
		                        ,[CreatedBy]
		                        ,[CreationDate]
		                        ,[ModifiedBy]
		                        ,[ModificationDate]
                                ,PermissionID
                          from #tmp

                          drop table #tmp";
            using (TradingEntities db = new TradingEntities())
            {
                accountTypeList = db.Database
                    .SqlQuery<AccountTypeAndPermission>(StrQuery)
                    .ToList<AccountTypeAndPermission>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, accountTypeList);

        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult Insert([FromBody] a_UserAccount model)
        {
            if (model.UserName == "" || model.UserName == null)
            {
                return Content(HttpStatusCode.BadRequest, "Username is required");
            } else if (model.AccountTypeID == 0)
            {
                return Content(HttpStatusCode.BadRequest, "Account type is required");
            } else if (model.FirstName == "" || model.FirstName == null)
            {
                return Content(HttpStatusCode.BadRequest, "First name is required");
            } else if (model.LastName == "" || model.LastName == null)
            {
                return Content(HttpStatusCode.BadRequest, "Last name is required");
            } else if (model.EmailAddress == "" || model.EmailAddress == null)
            {
                return Content(HttpStatusCode.BadRequest, "Email address type is required");
            } else
            {
                using (var db = new TradingEntities())
                {
                    //var str = $"select top 1 DefaultPassword from a_UserAccountType where AccountTypeID = {model.AccountTypeID}";
                    //var dtAccType = GlobalFunctions.DataReader(str);
                    string defaultPassword = GlobalFunctions.Encrypt("tr@d1ngp@ssw0rd");
                    //if (dtAccType.Rows.Count > 0)
                    //{
                    //    if(dtAccType.Rows[0]["DefaultPassword"].ToString() != "")
                    //    {
                    //        defaultPassword = dtAccType.Rows[0]["DefaultPassword"].ToString();
                    //    }
                    //}

                    var checkrec = db.a_UserAccount.Where(a => a.UserName == model.UserName && a.IsActive == true).FirstOrDefault<a_UserAccount>();
                    if (checkrec != null)
                    {
                        return Content(HttpStatusCode.BadRequest, "Username already exist");
                    }else
                    {
                        var tbl = new a_UserAccount()
                        {
                            UserName = model.UserName,
                            Password = defaultPassword,
                            AccountTypeID = model.AccountTypeID,
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            EmailAddress = model.EmailAddress,
                            IsActive = true,
                            CreatedBy = model.CreatedBy,
                            CreationDate = DateTime.Now
                        };
                        db.a_UserAccount.Add(tbl);
                        db.SaveChanges();

                        return Content(HttpStatusCode.OK, RetrivedRecord(tbl.UserID));
                    }
                }
            }
        }

        [HttpPut]
        [Authorize]
        public IHttpActionResult Update(int id, [FromBody] a_UserAccount model)
        {
            if (model.UserName == "" || model.UserName == null)
            {
                return Content(HttpStatusCode.BadRequest, "Username is required");
            }
            else if (model.AccountTypeID == 0)
            {
                return Content(HttpStatusCode.BadRequest, "Account type is required");
            }
            else if (model.FirstName == "" || model.FirstName == null)
            {
                return Content(HttpStatusCode.BadRequest, "First name is required");
            }
            else if (model.LastName == "" || model.LastName == null)
            {
                return Content(HttpStatusCode.BadRequest, "Last name is required");
            }
            else if (model.EmailAddress == "" || model.EmailAddress == null)
            {
                return Content(HttpStatusCode.BadRequest, "Email address type is required");
            }
            else
            {
                using (var db = new TradingEntities())
                {
                    var checkrec = db.a_UserAccount.Where(a =>
                    a.UserName == model.UserName && a.IsActive == true
                    && a.UserID != id).FirstOrDefault<a_UserAccount>();

                    if (checkrec != null)
                    {
                        return Content(HttpStatusCode.BadRequest, "Record already exist");
                    }
                    else
                    {
                        var tbl = db.a_UserAccount.Where(a => a.UserID == id).FirstOrDefault<a_UserAccount>();
                        tbl.UserName = model.UserName;
                        tbl.AccountTypeID = model.AccountTypeID;
                        tbl.FirstName = model.FirstName;
                        tbl.LastName = model.LastName;
                        tbl.EmailAddress = model.EmailAddress;
                        tbl.ModifiedBy = model.ModifiedBy;
                        tbl.ModificationDate = DateTime.Now;
                        db.SaveChanges();
                        return Content(HttpStatusCode.OK, RetrivedRecord(tbl.UserID));
                    }
                }
            }
        }

        [HttpDelete]
        [Authorize]
        public IHttpActionResult Delete(int id)
        {
            var str = "delete from a_UserAccount where UserID = @id";
            var userID = new SqlParameter("@id", id);

            GlobalFunctions.DataReader(str, userID);

            return Ok();
        }

        [HttpGet]
        [Authorize]
        public IHttpActionResult Disable(int id)
        {
            var str = "UPDATE a_UserAccount SET IsActive = 0 WHERE UserID = @id";
            var userID = new SqlParameter("@id", id);

            GlobalFunctions.DataReader(str, userID);

            return Ok();
        }

        [HttpGet]
        [Authorize]
        public IHttpActionResult Enable(int id)
        {
            var str = "UPDATE a_UserAccount SET IsActive = 1 WHERE UserID = @id";
            var userID = new SqlParameter("@id", id);

            GlobalFunctions.DataReader(str, userID);

            return Ok();
        }

        [HttpPut]
        [Authorize]
        public IHttpActionResult ResetPassword(int id , [FromBody] a_UserAccount model ) 
        {
            //var str = @"update a_UserAccount set Password = @password where UserID = @id";

            //var strAcc = $"select top 1 DefaultPassword from a_UserAccountType where AccountTypeID = {model.AccountTypeID}";
            //var dtAccType = GlobalFunctions.DataReader(strAcc);
            string defaultPassword = GlobalFunctions.Encrypt("tr@d1ngp@ssw0rd");
            //if (dtAccType.Rows.Count > 0)
            //{
            //    if (dtAccType.Rows[0]["DefaultPassword"].ToString() != "")
            //    {
            //        defaultPassword = dtAccType.Rows[0]["DefaultPassword"].ToString();
            //    }
            //}


            var str = $@"update a_UserAccount
	                    set Password = '{defaultPassword}'
                      where UserID = @id";


            var userID = new SqlParameter("@id", id);
            //var password = new SqlParameter("@password", GlobalFunctions.Encrypt("Rtd$mc2022"));
            GlobalFunctions.DataReader(str, userID);

            str = @"select * from a_UserAccount where UserID = @id";
            userID = new SqlParameter("@id", id);
            var dt = GlobalFunctions.DataReader(str, userID);
            
            str = @"select * from [a_CompanySetting]";
            var dtemail = GlobalFunctions.DataReader(str);

            try
            {
                if (dt.Rows.Count > 0 && dt.Rows[0]["EmailAddress"].ToString() != "")
                {
                    MailMessage mailMessage = new MailMessage(dtemail.Rows[0]["EmailAddress"].ToString(), dt.Rows[0]["EmailAddress"].ToString());
                    mailMessage.Subject = "Change password";
                    mailMessage.Body = @"This is to inform you that your password has been reset to default password (p@ssw0rd).";

                    SmtpClient smtpClient = new SmtpClient(dtemail.Rows[0]["HostName"].ToString(), Convert.ToInt32(dtemail.Rows[0]["Port"].ToString()));
                    smtpClient.Credentials = new System.Net.NetworkCredential()
                    {
                        UserName = dtemail.Rows[0]["EmailAddress"].ToString(),
                        Password = GlobalFunctions.Decrypt(dtemail.Rows[0]["Password"].ToString())
                    };

                    smtpClient.Send(mailMessage);
                }
            }
            catch
            {
           
            }
            return Ok();
        }

        [HttpPut]
        [Route("UserManagement/ChangePassword/{userid}")]
        [Authorize]
        public IHttpActionResult ChangePassword(int userid, [FromBody] ChangePassword model)
        {
            var str = @"select * from a_UserAccount where UserID = @id";
            var userId = new SqlParameter("@id", userid);
            var dt = GlobalFunctions.DataReader(str, userId);

           

            if (dt.Rows.Count > 0)
            {
                if (GlobalFunctions.Decrypt(dt.Rows[0]["Password"].ToString()) == model.currentPassword)
                {
                    if (model.newPassword == model.confirmPassword)
                    {
                        str = @"update a_UserAccount set Password = @passWord where UserID = @id";
                        userId = new SqlParameter("@id", userid);
                        var passWord = new SqlParameter("@passWord", GlobalFunctions.Encrypt(model.newPassword));
                        GlobalFunctions.DataReader(str, userId, passWord);

                        return Ok();
                    }
                    else
                    {
                        return Content(HttpStatusCode.BadRequest, "Password did not match");
                    }
                }
                else
                {
                    return Content(HttpStatusCode.BadRequest, "Current password is incorrect");
                }
            }
            else
            {
                return Content(HttpStatusCode.BadRequest, "Current password is incorrect");
            }




            
        }

        [HttpGet]
        [Authorize]
        public IHttpActionResult GetCompanySetting()
        {
            var str = @"SELECT [CompanyID]
                      ,[CompanyName]
                      ,[EmailAddress]
                      ,[Password]
                      ,[HostName]
                      ,[Port]
                      ,[IsActive]
                      ,[CreatedBy]
                      ,[CreationDate]
                      ,[ModifiedBy]
                      ,[ModificationDate]
                  FROM [a_CompanySetting]";

            var dt = GlobalFunctions.DataReader(str);

            return Content(HttpStatusCode.OK, dt);
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult SaveCompanySetting([FromBody] a_CompanySetting model)
        {
            if (model.CompanyName == "" || model.CompanyName == null)
            {
                return Content(HttpStatusCode.BadRequest, "Company name is required");
            } else if (model.EmailAddress == "" || model.EmailAddress == null)
            {
                return Content(HttpStatusCode.BadRequest, "Email address is required");
            } else if (model.Password == "" || model.Password == null)
            {
                return Content(HttpStatusCode.BadRequest, "Password is required");
            } else if (model.HostName == "" || model.HostName == null)
            {
                return Content(HttpStatusCode.BadRequest, "Host name is required");
            } else if (model.Port == "" || model.Port == null)
            {
                return Content(HttpStatusCode.BadRequest, "Port is required");
            }else
            {

                using (var db = new TradingEntities())
                {
                    var tbl = db.a_CompanySetting.Where(a => a.CompanyID != 0).FirstOrDefault<a_CompanySetting>();

                    if (tbl != null)
                    {
                        tbl.CompanyName = model.CompanyName;
                        tbl.EmailAddress = model.EmailAddress;
                        tbl.Password = GlobalFunctions.Encrypt(model.Password);
                        tbl.HostName = model.HostName;
                        tbl.Port = model.Port;
                        tbl.ModifiedBy = tbl.CreatedBy;
                        tbl.ModificationDate = DateTime.Now;
                        db.SaveChanges();
                    }else
                    {
                        var addrec = new a_CompanySetting()
                        {
                            CompanyName = model.CompanyName,
                            EmailAddress =model.EmailAddress,
                            Password = GlobalFunctions.Encrypt(model.Password),
                            HostName = model.HostName,
                            Port = model.Port,
                            IsActive = true,
                            CreatedBy = model.CreatedBy,
                            CreationDate = DateTime.Now
                        };
                        db.a_CompanySetting.Add(addrec);
                        db.SaveChanges();
                    }
                }

            }

            return Ok();
            
        }

        [HttpGet]
        [Route("UserManagement/Decrypt/{str}")]
        
        public IHttpActionResult Decrypt(string str)
        {
            return Content(HttpStatusCode.OK, GlobalFunctions.Decrypt(str));
        }

        [HttpGet]
        [Route("UserManagement/Encrypt/{str}")]
        
        public IHttpActionResult Encrypt(string str)
        {
            return Content(HttpStatusCode.OK, GlobalFunctions.Encrypt(str));
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult UserLogs([FromBody] UserLog model)
        {
            if(model.UserID != null)
            {
                var query = $@"insert into a_UserLogs
                            ([UserID]
                              ,[ModuleID]
                              ,[LogTime]
                              ,[Action])
                        values({model.UserID},
                               {model.ModuleID},
                               '{DateTime.Now}',
                               '{model.Action}')";
                GlobalFunctions.DataReader(query);
            }
            
            return Content(HttpStatusCode.OK, "Logs Successfully");
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult LoginLogs([FromBody] LoginLog model)
        {

            try
            {
                if (model.UserID != null)
                {
                    var query = $@"insert into a_LoginLogs
                            ([UserID]
                              ,[LogTime]
                              ,[Action])
                        values({model.UserID},
                               '{DateTime.Now}',
                               '{model.Action}')";
                    GlobalFunctions.DataReader(query);
                }
            }
            catch(Exception ex)
            {
                return Content(HttpStatusCode.OK, ex.Message);
            }
            return Content(HttpStatusCode.OK, "Logs Successfully");
        }
    }
}