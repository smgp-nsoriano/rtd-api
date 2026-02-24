using Microsoft.Owin.Security.OAuth;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;

namespace New_Trading_API.Models
{
    public class MyAuthorizationServerProvider : OAuthAuthorizationServerProvider
    {

        private DataTable GetRecords(string query, params SqlParameter[] parameter)
        {

            using (var db = new TradingEntities())
            {
                var dt = new DataTable();
                var conn = db.Database.Connection;
                var cmd = conn.CreateCommand();
                var str = query;
                cmd.CommandText = str;
                cmd.CommandType = CommandType.Text;

                if (parameter != null)
                {
                    foreach (var q in parameter)
                    {
                        cmd.Parameters.Add(q);
                    }
                }

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                }
                conn.Close();

                return dt;
            }

        }

        public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            context.Validated();
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            var identity = new ClaimsIdentity(context.Options.AuthenticationType);

            var str = @"select UserID
                          ,FirstName + ' ' + LastName FullName
                          ,UserName
                          ,Password
                          ,b.AccountType
                          from a_UserAccount a
                          left join a_UserAccountType b on b.AccountTypeID = a.AccountTypeID
                          where a.IsActive = 1 and UserName = @username";
            var username = new SqlParameter("@username", context.UserName);
            var dt = GetRecords(str, username);

            if (dt.Rows.Count > 0)
            {
                if (GlobalFunctions.Decrypt(dt.Rows[0]["Password"].ToString()) == context.Password)
                {
                    GlobalFunctions.Log(logType: "LOGIN",
                           action: "Login Success",
                           message: "User successfully logged in.",
                           userName: context.UserName);

                    identity.AddClaim(new Claim("UserName", dt.Rows[0]["UserName"].ToString()));
                    identity.AddClaim(new Claim("AccountType", dt.Rows[0]["AccountType"].ToString()));
                    identity.AddClaim(new Claim("UserID", dt.Rows[0]["UserID"].ToString()));
                    context.Validated(identity);
                }
                else
                {
                    GlobalFunctions.Log(logType: "LOGIN",
                         action: "Login Failed",
                         message: "The user name or password is incorrect.",
                         userName: context.UserName);
                }
            }
            else
            {
                GlobalFunctions.Log(logType: "LOGIN",
                         action: "Login Failed",
                         message: "The user name or password is incorrect.",
                         userName: context.UserName);

                context.SetError("Invalid Username", "Invalid Username");
                context.Rejected();
            }
        }
    }

}