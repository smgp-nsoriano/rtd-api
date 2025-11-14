using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace New_Trading_API.Constant
{
    public static class ReserveDefaults
    {
        public static readonly Dictionary<string, string> SchedualeTagNames = new Dictionary<string, string>()
        {
            {"DAP","DAPEG_{0}_{1}_MW.MV"},
            {"HAP","HAPEG_{0}_{1}_MW.MV"},
            {"RTD","RTDEG_{0}_{1}_MW.MV"},
            {"ACTUAL","ACTUAL_5MINS_{0}_MW.MV" }
        };
        public static readonly Dictionary<string, string> PriceTagNames = new Dictionary<string, string>()
        {
            {"DAP","DAPEG_{0}_{1}_P.MV"},
            {"HAP","HAPEG_{0}_{1}_P.MV"},
            {"RTD","RTDEG_{0}_{1}_P.MV"}
        };

        public static readonly Dictionary<string, string> OpressTagNames = new Dictionary<string, string>()
        {
            {"DAP","OPRES_DAPEG_{0}_{1}_P.MV"},
            {"HAP","OPRES_HAPEG_{0}_{1}_P.MV"},
            {"RTD","OPRES_RTDEG_{0}_{1}_P.MV"}
        };

        public static readonly Dictionary<string, string> MrktReqtTagNames = new Dictionary<string, string>()
        {
            {"DAP","REG_MKT_REQT_{0}_DAP_{1}_MW.MV"},
            {"RTD","REG_MKT_REQT_{0}_5MINS_{1}_MW.MV"}
        };

        public static readonly Dictionary<string, string> MrktSchedTagNames = new Dictionary<string, string>()
        {
            {"DAP","REG_GEN_{0}_DAP_{1}_MW.MV"},
            {"RTD","REG_GEN_{0}_5MINS_{1}_MW.MV"}
        };
    }
}