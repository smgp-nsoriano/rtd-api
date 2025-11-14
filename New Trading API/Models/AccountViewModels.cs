using System;
using System.Collections.Generic;

namespace New_Trading_API.Models
{
    // Models returned by AccountController actions.

    public class ExternalLoginViewModel
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public string State { get; set; }
    }

    public class ManageInfoViewModel
    {
        public string LocalLoginProvider { get; set; }

        public string Email { get; set; }

        public IEnumerable<UserLoginInfoViewModel> Logins { get; set; }

        public IEnumerable<ExternalLoginViewModel> ExternalLoginProviders { get; set; }
    }

    public class UserInfoViewModel
    {
        public string Email { get; set; }

        public bool HasRegistered { get; set; }

        public string LoginProvider { get; set; }
    }

    public class UserLoginInfoViewModel
    {
        public string LoginProvider { get; set; }

        public string ProviderKey { get; set; }
    }

    public class UserPermission
    {
        public int permissionID { get; set; }
        public int siteId { get; set; }
        public int unitId { get; set; }
        public int accessid { get; set; }
        public string config { get; set; }
        public Boolean tag { get; set; }
        public Boolean isSite { get; set; }


    }

    public class UserAccountType
    {
        public int accounttypeid { get; set; }
        public string accounttype { get; set; }
        public string modifiedby { get; set; }
    }

    public class ChangePassword
    {
        public string currentPassword { get; set; }
        public string newPassword { get; set; }
        public string confirmPassword { get; set; }
    }
}
