using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using New_Trading_API.Models;
using System.Security.Claims;
using System.Data.SqlClient;
using Newtonsoft.Json;

namespace New_Trading_API.Controllers
{
    public class ConfigurationController : ApiController
    {
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetSite(int CompanyID)
        {
            List<Site> siteList = new List<Site>();
            using (TradingEntities db = new TradingEntities())
            {
                string Str = @"select * 
                              from m_Site 
                              where CompanyID = @companyID
                              and isactive = 1";
                var paramCompanyID = new SqlParameter("@companyID", CompanyID);

                siteList = db.Database.SqlQuery<Site>(Str, paramCompanyID).ToList<Site>();
            }

            GlobalFunctions.Log(logType: "GET_SITE",
                            action: "Sites List Retrieved Success",
                            message: "Sites retrieved successfully.",
                            userName: GetUsername());

            return Request.CreateResponse(HttpStatusCode.OK, siteList);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetSiteAccess(int permissionID)
        {
            List<Site> siteList = new List<Site>();
            Site site = new Site();
            using (TradingEntities db = new TradingEntities())
            {
                string Str = $@"select * from m_Site 
			                                where CompanyID = 1
			                                and isactive = 1 
                                            and SiteID in ( select siteID 
											                from m_UserSiteAccess
											                where PermissionID = {permissionID})";

                siteList = db.Database.SqlQuery<Site>(Str).ToList<Site>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, siteList);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetOperatorUnit(int permissionID)
        {
            OperatorUnit operatorUnit = new OperatorUnit();
            using (TradingEntities db = new TradingEntities())
            {
                string Str = $@"                               
                                select top 1  b.UnitID
	                                   ,b.UnitNumber
	                                   ,c.SiteCode
	                                   ,c.SiteID
                                       ,b.UnitName
                                       ,b.IsWithAlarm
                                       ,b.IsWithDecimal
                                from m_UserSiteAccess a
                                left join m_Unit b on a.unitID = b.UnitID and b.IsActive = 1
                                left join m_Site c on b.SiteID = c.SiteID and c.IsActive = 1
                                where PermissionID = {permissionID}
                                ";
                operatorUnit = db.Database.SqlQuery<OperatorUnit>(Str).FirstOrDefault<OperatorUnit>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, operatorUnit);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetUnit(int SiteID)
        {
            List<Unit> unitList = new List<Unit>();
            using (TradingEntities db = new TradingEntities())
            {
                string Str = "select * from m_Unit where siteID = @siteID and isActive = 1";
                var paramSiteID = new SqlParameter("@siteID", SiteID);
                //if(db.Database.SqlQuery<Unit>(Str, paramSiteID).ToList<Unit>().Count > 0)
                //{
                unitList = db.Database.SqlQuery<Unit>(Str, paramSiteID).OrderBy(x => x.UnitNumber).ToList<Unit>();
                //}

            }

            GlobalFunctions.Log(logType: "GET_UNIT",
                            action: "Units List Retrieved Success",
                            message: "Units retrieved successfully.",
                            userName: GetUsername());
            return Request.CreateResponse(HttpStatusCode.OK, unitList);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetAllUnit()
        {
            List<Unit> unitList = new List<Unit>();
            using (TradingEntities db = new TradingEntities())
            {
                string Str = @"select u.* from m_Unit u
                                inner join m_Site s on u.SiteID = s.SiteID and s.IsActive = 1
                                where u.isActive = 1";
                unitList = db.Database.SqlQuery<Unit>(Str).ToList<Unit>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, unitList);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetWebServiceSetting(int UnitID)
        {
            string StrQuery = "";
            WebServiceSet webservice = new WebServiceSet();

            using (TradingEntities db = new TradingEntities())
            {
                StrQuery = @"select * from [m_WebServiceSettings] where [UnitID] = @unitID";
                var paramUntID = new SqlParameter("@unitID", UnitID);
                webservice = db.Database.SqlQuery<WebServiceSet>(StrQuery, paramUntID).FirstOrDefault<WebServiceSet>();
            }

            GlobalFunctions.Log(logType: "GET_WEBSERVICE_SETTINGS",
                            action: "Webservice Settings Retrieved Success",
                            message: $"Webservice Settings for UnitId {UnitID} retrieved successfully.",
                            userName: GetUsername());
            return Request.CreateResponse(HttpStatusCode.OK, webservice);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetWebServiceURLList()
        {
            string StrQuery = "";
            List<WebServiceUrl> webUrl = new List<WebServiceUrl>();
            using (TradingEntities db = new TradingEntities())
            {
                StrQuery = "select * from [m_WebServiceURL] where isactive = 1";
                webUrl = db.Database.SqlQuery<WebServiceUrl>(StrQuery).ToList<WebServiceUrl>();
            }

            GlobalFunctions.Log(logType: "GET_WEBSERVICE_URL",
                            action: "Webservice Urls Retrieved Success",
                            message: $"Webservice urls retrieved successfully.",
                            userName: GetUsername());
            return Request.CreateResponse(HttpStatusCode.OK, webUrl);
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult CreateUpdateSite([FromBody] Site model)
        {
            Site site = new Site();
            string StrQuery = "";
            var identity = (ClaimsIdentity)User.Identity;
            var username = identity.FindFirst("UserName").Value;

            using (TradingEntities db = new TradingEntities())
            {
                if (model.SiteID == 0)
                {
                    StrQuery = "";
                    StrQuery = "Insert into m_Site" +
                            " ( [CompanyID],[SiteName],[SiteCode],[Address],[ParticipantID],[Region],[FtpPath],[IsActive]" +
                            ",[CreatedBy],[CreationDate],[ModifiedBy],[ModificationDate])" +
                            "values(" +
                            " 1, '" + model.SiteName + "'" +
                            " ,'" + model.SiteCode + "'" +
                            " ,'" + model.Address + "'" +
                            " ,'" + model.SiteCode + "'" +
                            " ,'" + model.Region + "'" +
                            " ,'" + model.FtpPath + "',1" +
                            " ,'"+ username + "'" +
                            " ,getdate() ,'' ,''" +
                            ")";

                    db.Database.ExecuteSqlCommand(StrQuery);

                    GlobalFunctions.Log(logType: "CREATE_SITE",
                           action: "Site Create Success",
                           message: $"Site created successfully.",
                           newValues: JsonConvert.SerializeObject(model),
                           userName: GetUsername());
                }
                else
                {
                    {
                        StrQuery = "";
                        StrQuery = "Update m_Site" +
                                    " set [SiteName] = '" + model.SiteName + "'" +
                                    " ,[SiteCode] = '" + model.SiteCode + "'" +
                                    " ,[Address] = '" + model.Address + "'" +
                                    " ,[ParticipantID] = '" + model.SiteCode + "'" +
                                    " ,[Region] = '" + model.Region + "'" +
                                    " ,[FtpPath] = '" + model.FtpPath + "'" +
                                    " ,ModifiedBy = '"+ username + "'" +
                                    " ,ModificationDate = getdate()" +
                                    "where SiteID = " + model.SiteID;

                        db.Database.ExecuteSqlCommand(StrQuery);

                        GlobalFunctions.Log(logType: "UPDATE_SITE",
                          action: "Site Update Success",
                          message: $"Site updated successfully.",
                          newValues: JsonConvert.SerializeObject(model),
                          userName: GetUsername());
                    }
                }
            }

            return Ok();


        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult CreateUpdateUnit([FromBody] Unit model)
        {
            Unit unit = new Unit();
            //string message = "";
            string StrQuery = "";

            var identity = (ClaimsIdentity)User.Identity;
            var username = identity.FindFirst("UserName").Value;

            try
            {
                using (TradingEntities db = new TradingEntities())
                {
                    if (model.UnitID > 0)
                    {
                        StrQuery = "Update m_Unit " +
                           " set [UnitNumber] = '" + model.UnitNumber + "'" +
                           " ,[SiteID] =" + model.SiteID +
                           " ,[TypeID] ='" + model.TypeID + "'" +
                           " ,ModifiedBy = '" + username + "'" +
                           " ,ModificationDate = getdate()" +
                           " ,BColor = '" + model.BColor + "'"+
                           " ,FColor = '" + model.FColor + "'" +
                           " where UnitID = " + model.UnitID;

                        db.Database.ExecuteSqlCommand(StrQuery);
                        GlobalFunctions.Log(logType: "UPDATE_UNIT",
                          action: "Unit Update Success",
                          message: $"Unit updated successfully.",
                          newValues: JsonConvert.SerializeObject(model),
                          userName: GetUsername());
                    }
                    else
                    {
                        StrQuery = "insert into m_Unit " +
                            " ([UnitNumber],[SiteID],[TypeID],[RTDTagName],[EAPTagName]" +
                            ",[DAPTagName],[WAPTagName],[IsActive],[CreatedBy],[CreationDate],[ModifiedBy],[ModificationDate],[BColor],[FColor])" +
                            " values('" + model.UnitNumber + "', " + model.SiteID + " ,'" + model.TypeID + "'" +
                            " ,'" + model.UnitNumber + "_RTD' ,'" + model.UnitNumber + "_EAP' ,'" + model.UnitNumber + "_DAP' ,'" + model.UnitNumber + "_WAP'" +
                            " ,1 ,'" + username + "'" +
                            " ,getdate() ,'','','"+ model.BColor + "','" + model.FColor + "')";

                        db.Database.ExecuteSqlCommand(StrQuery);
                        GlobalFunctions.Log(logType: "CREATE_UNIT",
                          action: "Unit Create Success",
                          message: $"Unit created successfully.",
                          newValues: JsonConvert.SerializeObject(model),
                          userName: GetUsername());
                        
                    }
                }
            }
            catch
            {
            }

            return Ok();
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult UpdateWebUrl([FromBody] WebServiceUrl model)
        {
            
            string StrQuery = "";
            using (TradingEntities db = new TradingEntities())
            {
                try
                {
                    if (model.WebID > 0)
                    {
                        StrQuery = "Update [m_WebServiceURL] " +
                                " set [URL] = '" + model.URL + "'" +
                                " ,Type = '" + model.Type + "'" +
                                " where WebID = " + model.WebID;
                        db.Database.ExecuteSqlCommand(StrQuery);

                       GlobalFunctions.Log(logType: "UPDATE_WEBSERVICE_URL",
                       action: "Webservice Url Update Success",
                       message: $"Webservice url updated successfully.",
                       newValues: JsonConvert.SerializeObject(model),
                       userName: GetUsername());
                    }
                    else
                    {
                        StrQuery = "";
                        StrQuery = "Insert into m_WebServiceURL" +
                            "(ULR, Type ,Isactive)" +
                            " Values('" + model.URL + "','" + model.Type + "' ,1)";
                        db.Database.ExecuteSqlCommand(StrQuery);

                        GlobalFunctions.Log(logType: "CREATE_WEBSERVICE_URL",
                        action: "Webservice Url Create Success",
                        message: $"Webservice url created successfully.",
                        newValues: JsonConvert.SerializeObject(model),
                        userName: GetUsername());
                    }
                }
                catch
                {
                }
            }

            return Ok();
        }

        [HttpDelete]
        [Authorize]
        [Route("Configuration/DeleteSite/{SiteID}")]
        public IHttpActionResult DeleteSite(int SiteID)
        {
            string StrQuery = @"Update m_Site
                                set isactive = 0
                                where siteID = " + SiteID;
            using (TradingEntities db = new TradingEntities())
            {
                db.Database.ExecuteSqlCommand(StrQuery);
            }

            GlobalFunctions.Log(logType: "DELETE_SITE",
                        action: "Site Delete Success",
                        message: $"Site deleted successfully.",
                        newValues: $"SiteID:{SiteID}",
                        userName: GetUsername());
            return Ok();
        }

        [HttpDelete]
        [Authorize]
        [Route("Configuration/DeleteUnit/{UnitID}")]
        public IHttpActionResult DeleteUnit(int UnitID)
        {
            string StrQuery = @"Update m_Unit
                                set isactive = 0
                                where UnitID = " + UnitID;
            using (TradingEntities db = new TradingEntities())
            {
                db.Database.ExecuteSqlCommand(StrQuery);
            }

            GlobalFunctions.Log(logType: "DELETE_UNIT",
                        action: "Unit Delete Success",
                        message: $"Unit deleted successfully.",
                        newValues: $"UnitID:{UnitID}",
                        userName: GetUsername());
            return Ok();
        }

        private string GetUsername()
        {
            var identity = (ClaimsIdentity)User.Identity;
            return identity.FindFirst("UserName").Value;
        }
    }
}