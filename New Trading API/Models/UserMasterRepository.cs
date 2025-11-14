using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace New_Trading_API.Models
{
    public class UserMasterRepository : IDisposable
    {
        TradingEntities context = new TradingEntities();
        //This method is used to check and validate the user credentials
        public a_UserAccount ValidateUser(string username, string password)
        {
            return context.a_UserAccount
                .FirstOrDefault(user => user
                                        .UserName
                                        .Equals(username, StringComparison.OrdinalIgnoreCase)
                                        && user.Password == password);
        }
        public void Dispose()
        {
            context.Dispose();
        }

    }
}