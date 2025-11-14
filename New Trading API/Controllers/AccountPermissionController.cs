using New_Trading_API.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace New_Trading_API.Controllers
{
    public class AccountPermissionController : ApiController
    {
        
        [HttpGet]
        [Authorize]
        [Route("AccountPermission/GetSites/{permissionId}/{accountTypeId}")]
        public IHttpActionResult GetSites(Nullable<int> permissionId, Nullable<int> accountTypeId)
        {
            var str = @"declare @PermissionID as integer = isnull(@permId,0);
                        declare @AccountTypeID as integer = isnull(@accountid,0);

                        select top 1
                        a.AccountTypeID 
                        ,a.AccountType
                        ,isnull(b.PermissionID,0) PermissionID
                        ,isnull(b.IsShowPrice,0) IsShowPrice
                        ,isnull(b.IsShowBid,0) IsShowBid
                        ,isnull(b.IsShowConfig,0) IsShowConfig
                        ,isnull(b.IsShowUserConfig,0) IsShowUserConfig
                        ,isnull(b.IsManualEntry,0) IsManualEntry
                        ,isnull(b.IsAlarmDisable,0)IsAlarmDisable
                        ,isnull(b.IsOverride,0)IsOverride
                        ,isnull(b.IsPortfolioOnly,0)IsPortfolioOnly
                        ,isnull(b.IsPbReason,0) IsPbReason
                        ,isnull(b.IsCamSetup,0) IsCamSetup
                        into #tblpermision from a_UserAccountType  a
                        left join a_Permission b on b.UserAccountTypeID = @AccountTypeID and PermissionID = @PermissionID
                        
                        select 
                        case when isnull(b.AccessID,0) = 0 then convert(bit,0) else convert(bit,1) end Tag
                        ,SiteCode + ' Dashboard' SiteName
                        ,a.SiteID
                        ,isnull(b.AccessID,0) AccessID
                        ,Null Config
                        ,convert(bit,1) IsSite
                        from m_Site a
                        left join m_UserSiteAccess b on b.SiteID = a.SiteID and PermissionID = @PermissionID
                        where a.isactive = 1

                        
                        drop table #tblpermision";

                        //union all

                        //select IsShowPrice, 'WESM Price', AccountTypeID, PermissionID, 'IsShowPrice',convert(bit, 0)   from #tblpermision union all
                        //select IsShowUserConfig, 'User Account And Permission', AccountTypeID, PermissionID, 'IsShowUserConfig',convert(bit, 0)  from #tblpermision union all
                        //select IsShowConfig, 'WESM NMMS Config', AccountTypeID, PermissionID, 'IsShowConfig',convert(bit, 0)  from #tblpermision union all
                        //select IsShowBid, 'BID Uploading', AccountTypeID, PermissionID, 'IsShowBid',convert(bit, 0)  from #tblpermision  union all
                        //select IsAlarmDisable, 'Disable Alarm', AccountTypeID, PermissionID, 'IsAlarmDisable',convert(bit, 0)  from #tblpermision union all
                        //select IsOverride, 'Override Value', AccountTypeID, PermissionID, 'IsOverride',convert(bit, 0)  from #tblpermision union all
                        //select IsPortfolioOnly, 'Trading Porfolio', AccountTypeID, PermissionID, 'IsPortfolioOnly',convert(bit, 0)  from #tblpermision union all
                        //select IsPbReason, 'Trading PB Reason', AccountTypeID, PermissionID, 'IsPbReason',convert(bit, 0)  from #tblpermision union all
                        //select IsManualEntry, 'Manual Entry', AccountTypeID, PermissionID, 'IsManualEntry',convert(bit, 0)  from #tblpermision union all
                        //select IsCamSetup, 'CAMS Settings', AccountTypeID, PermissionID, 'IsCamSetup',convert(bit, 0)  from #tblpermision 


            var permId = new SqlParameter("@permId", permissionId);
            var accountid = new SqlParameter("@accountid", accountTypeId);

            return Content(HttpStatusCode.OK, GlobalFunctions.DataReader(str, permId, accountid));
        }

        [HttpGet]
        [Authorize]
        [Route("AccountPermission/GetOperatorAccess/{permissionId}/{accountTypeId}")]
        public IHttpActionResult GetOperatorAccess(Nullable<int> permissionId, Nullable<int> accountTypeId)
        {
            var str = @"declare @PermissionID as integer = isnull(@permId,0);
                        declare @AccountTypeID as integer = isnull(@accountid,0);

                        select top 1
                        a.AccountTypeID 
                        ,a.AccountType
                        ,isnull(b.PermissionID,0) PermissionID
                        ,isnull(b.IsShowPrice,0) IsShowPrice
                        ,isnull(b.IsShowBid,0) IsShowBid
                        ,isnull(b.IsShowConfig,0) IsShowConfig
                        ,isnull(b.IsShowUserConfig,0) IsShowUserConfig
                        ,isnull(b.IsManualEntry,0) IsManualEntry
                        ,isnull(b.IsCamSetup,0) IsCamSetup
                        into #tblpermision from a_UserAccountType  a
                        left join a_Permission b on b.UserAccountTypeID = @AccountTypeID and PermissionID = @PermissionID

                        select 
                        case when isnull(b.AccessID,0) = 0 then convert(bit,0) else convert(bit,1) end Tag
                        ,SiteCode + ' Dashboard' SiteName
                        ,a.SiteID
                        ,isnull(b.AccessID,0) AccessID
                        ,Null Config
                        ,convert(bit,1) IsSite
                        ,convert(bit,0) IsSetting
                        from m_Site a
                        left join m_UserSiteAccess b on b.SiteID = a.SiteID and PermissionID = @PermissionID
                        where a.isactive = 1 and a.[SiteName] = 'SPDC'

                        union all

						select case when isnull(c.AccessID,0) = 0 then convert(bit,0) else convert(bit,1) end Tag
                        ,UnitNumber + ' Dashboard' UnitNumber
                        ,a.UnitID
                        ,isnull(c.AccessID,0) AccessID
                        ,Null Config
                        ,convert(bit,0) IsSite
                        ,convert(bit,0) IsSetting
						from m_Unit a
						inner join m_Site b on a.SiteID = b.SiteID and b.IsActive = 1 and b.SiteName != 'SPDC'
						left join m_UserSiteAccess c on a.UnitID = c.UnitID and PermissionID = @PermissionID
						where a.isactive = 1

                        drop table #tblpermision
                        ";


                        //union all

                        //select IsShowPrice, 'WESM Price', AccountTypeID, PermissionID, 'IsShowPrice',convert(bit, 0),convert(bit, 1) IsSetting from #tblpermision


            var permId = new SqlParameter("@permId", permissionId);
            var accountid = new SqlParameter("@accountid", accountTypeId);

            return Content(HttpStatusCode.OK, GlobalFunctions.DataReader(str, permId, accountid));
        }

        [HttpPost]
        [Authorize]
        [Route("AccountPermission/SavePermission/{accountType}/{createdby}/{isOperator}")]
        public IHttpActionResult SavePermission(string accountType, string createdby, Boolean isOperator)
        {
                using (var db = new TradingEntities())
                {
                    var tblaccounttype = new a_UserAccountType()
                    {
                        AccountType = accountType,
                        IsActive = true,
                        CreatedBy = createdby,
                        CreationDate = DateTime.Now
                    };
                    db.a_UserAccountType.Add(tblaccounttype);
                    db.SaveChanges();
                
                    var tblpermission = new a_Permission()
                    {
                        UserAccountTypeID = tblaccounttype.AccountTypeID,
                        IsShowPrice = false,
                        IsShowBid = false,
                        IsShowConfig = false,
                        IsShowUserConfig = false,
                        IsManualEntry = false,
                        IsActive = true,
                        IsOperator = isOperator,                        
                    };
                    db.a_Permission.Add(tblpermission);
                    db.SaveChanges();

                return Content(HttpStatusCode.OK, tblpermission.PermissionID);
                }   
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult SaveSite([FromBody] UserPermission model)
        {
           if (model.isSite == true)
            {
                if (model.accessid == 0 && model.tag == true)
                {
                    var str = @"INSERT INTO [m_UserSiteAccess]
                            ([PermissionID]
                            ,[SiteID]
                            )
                        VALUES
                            (@PermissionID
                            ,@SiteID
                            )";

                    var permissionID = new SqlParameter("@PermissionID", model.permissionID);
                    var siteID = new SqlParameter("@SiteID", model.siteId);

                    GlobalFunctions.DataReader(str, permissionID, siteID);
                } else if (model.accessid != 0 && model.tag == false)
                {
                    var str = "delete from m_UserSiteAccess where AccessID = @id";
                    var accessId = new SqlParameter("@id", model.accessid);
                    GlobalFunctions.DataReader(str, accessId);
                }
            } else
            {
                var str = @"update a_Permission set [" + model.config + "] = @tag where PermissionID = @id";
                var tag = new SqlParameter("@tag", model.tag);
                var permissionId = new SqlParameter("@id", model.permissionID);
                GlobalFunctions.DataReader(str, tag, permissionId);
            }

            return Ok();
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult SaveSiteOperator([FromBody] UserPermission model)
        {
            if(model.config == null)
            {
                if (model.accessid == 0 && model.tag == true)
                {
                    var str = @"INSERT INTO [m_UserSiteAccess]
                            ([PermissionID]
                            ,[SiteID]
                            ,[UnitID])
                        VALUES
                            (@PermissionID
                            ,@SiteID
                            ,@UnitID)";

                    var permissionID = new SqlParameter("@PermissionID", model.permissionID);
                    var siteID = new SqlParameter("@SiteID", model.isSite == true ? model.siteId : 0);
                    var untID = new SqlParameter("@UnitID", model.isSite == true ? 0 : model.siteId);

                    GlobalFunctions.DataReader(str, permissionID, siteID, untID);
                }
                else if (model.accessid != 0 && model.tag == false)
                {
                    var str = "delete from m_UserSiteAccess where AccessID = @id";
                    var accessId = new SqlParameter("@id", model.accessid);
                    GlobalFunctions.DataReader(str, accessId);
                }
            }
            else
            {
                var str = @"update a_Permission set [" + model.config + "] = @tag where PermissionID = @id";
                var tag = new SqlParameter("@tag", model.tag);
                var permissionId = new SqlParameter("@id", model.permissionID);
                GlobalFunctions.DataReader(str, tag, permissionId);
            }
            

            return Ok();
        }

        [HttpPut]
        [Authorize]
        public IHttpActionResult UpdateAccountType([FromBody] UserAccountType model)
        {
            var str = @"update a_UserAccountType set AccountType = @accountType, ModifiedBy = @modifiedBy, ModificationDate = GetDate() where AccountTypeID = @id";
            var accountType = new SqlParameter("@accountType", model.accounttype);
            var id = new SqlParameter("@id", model.accounttypeid);
            var modifiedBy = new SqlParameter("@modifiedBy", model.modifiedby);
            GlobalFunctions.DataReader(str, accountType, id, modifiedBy);

            return Ok();
        }

        [HttpDelete]
        [Authorize]
        [Route("AccountPermission/DeleteAccountType/{accountTypeId}")]
        public IHttpActionResult DeleteAccountType(int accountTypeId)
        {
            var str = @"delete from m_UserSiteAccess where PermissionID = (select PermissionID from a_Permission where UserAccountTypeID = @id)
                        delete from a_Permission where UserAccountTypeID = @id                        
                        delete from a_UserAccountType where AccountTypeID = @id";
            var accounttypeid = new SqlParameter("@id", accountTypeId);
            GlobalFunctions.DataReader(str, accounttypeid);

            return Ok();
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage EnPassword(string password)
        {
            return Request.CreateResponse(HttpStatusCode.OK, GlobalFunctions.Encrypt(password));
        }
    }
}
