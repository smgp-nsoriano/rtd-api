using New_Trading_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace New_Trading_API.Controllers
{
    public class WebSettingController : ApiController
    {
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetWebServiceSetting(int UnitID)
        {
            string StrQuery = "";
            WebServiceSet webservice = new WebServiceSet();

            using (TradingEntities db = new TradingEntities())
            {
                StrQuery = "select * from [m_WebServiceSettings] where " +
                        " [UnitID] = " + UnitID;
                webservice = db.Database.SqlQuery<WebServiceSet>(StrQuery).FirstOrDefault<WebServiceSet>();
            }

            return Request.CreateResponse(HttpStatusCode.OK, webservice);

        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetWebURLList()
        {
            string StrQuery = "";
            List<WebServiceUrl> webUrl = new List<WebServiceUrl>();
            using (TradingEntities db = new TradingEntities())
            {
                StrQuery = "select * from [m_WebServiceURL] where isactive = 1";
                webUrl = db.Database.SqlQuery<WebServiceUrl>(StrQuery).ToList<WebServiceUrl>();
            }
            return Request.CreateResponse(HttpStatusCode.OK, webUrl);
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult UpdateWebSetting([FromBody] WebServiceSet model)
        {
            string message = "";
            string StrQuery = "";
            int RecCount = 0;
            using (TradingEntities db = new TradingEntities())
            {
                StrQuery = "select top 1 * from [m_WebServiceSettings] where isactive = 1 and [UnitID] = " + model.UnitID;
                RecCount = db.Database.SqlQuery<WebServiceSet>(StrQuery).Count();

                try
                {
                    if (RecCount > 0)
                    {
                        StrQuery = "";
                        StrQuery = "Update m_WebServiceSettings " +
                            "set [Operation] = '" + model.Operation + "'" +
                            " ,[CertificateURL] = '" + model.CertificateURL + "'" +
                            " ,[Password] = '" + model.Password + "'" +
                            " ,[FriendlyName] = '" + model.FriendlyName + "'" +
                            " ,[TpUser] = '" + model.TpUser + "'" +
                            " ,[BidType] = '" + model.BidType + "'" +
                            " ,[SheetName] = '" + model.SheetName + "'" +
                            " where [UnitID] = " + model.UnitID + " and isactive = 1";
                    }
                    else
                    {
                        StrQuery = "";
                        StrQuery = "insert into m_WebServiceSettings" +
                            "([UnitID] ,[Operation] ,[CertificateURL] ,[Password] " +
                            ",[FriendlyName] ,[TpUser] ,[BidType] ,[FileLocation] ,[SheetName] ,[IsActive])" +
                            " Values(" + model.UnitID + " ,'" + model.Operation + "' ,'" + model.CertificateURL + "'" +
                            " ,'" + model.Password + "' ,'" + model.FriendlyName + "' ,'" + model.TpUser + "'" +
                            ",'" + model.BidType + "' ,'' ,'" + model.SheetName + "' ,1" +
                            ")";
                    }
                    db.Database.ExecuteSqlCommand(StrQuery);
                    message = "saved";
                }
                catch (Exception e)
                {
                    message = e.Message.ToString();
                }

                return Ok();
            }
        }
        
    }
}