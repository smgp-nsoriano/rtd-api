using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using New_Trading_API.Models;
using New_Trading_API.Services;
using Newtonsoft.Json;
namespace New_Trading_API.Controllers
{
    public class ReserveMarketController : ApiController
    {
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetSchedule(string UnitNumber)
        {
            List<ReserveScheduleView> sched = new List<ReserveScheduleView>();
            ReserveService reserveService = new ReserveService();
            sched = reserveService.GetReserveScheduleView(UnitNumber);

            return Request.CreateResponse(HttpStatusCode.OK, sched);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetRegionalPrices(string region)
        {
            List<ReserveScheduleView> price = new List<ReserveScheduleView>();
            ReserveService reserveService = new ReserveService();
            price = reserveService.GetRegionalReservePriceView(region);

            return Request.CreateResponse(HttpStatusCode.OK, price);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetRegionalPrices24h(string region, string date)
        {
            List<ReserveScheduleView> price = new List<ReserveScheduleView>();
            ReserveService reserveService = new ReserveService();
            price = reserveService.GetRegionalReservePrice24hView(region, date);

            return Request.CreateResponse(HttpStatusCode.OK, price);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetRegionalReserveRSP(string region)
        {
            List<ReserveScheduleView> price = new List<ReserveScheduleView>();
            ReserveService reserveService = new ReserveService();
            price = reserveService.GetRegionalReserveRSPView(region);

            return Request.CreateResponse(HttpStatusCode.OK, price);
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetRegionalPortfolio()
        {
            List<ReserveRegionalPortfolioView> price = new List<ReserveRegionalPortfolioView>();
            ReserveService reserveService = new ReserveService();
            price = reserveService.GetRegionalPortfolioView();

            return Request.CreateResponse(HttpStatusCode.OK, price);
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult SaveGeneralRemarks([FromBody] StringValue Model)
        {
            using (TradingEntities db = new TradingEntities())
            {
                string query = $"exec[sp_SaveGeneralComment] '{Model.UnitNumber}','{Model.Value}'";
                db.Database.ExecuteSqlCommand(query);
            }

            GlobalFunctions.Log(logType: "SAVE_RESERVED_PORTFOLIO_REMARKS",
                            action: "Reserved Portfolio Remarks Success",
                            message: $"Reserved Portfolio Remarks successfully saved.",
                            newValues: JsonConvert.SerializeObject(Model),
                            userName: GetUsername());
            return Content(HttpStatusCode.OK, "Value successfully saved.");
        }

        private string GetUsername()
        {
            var identity = (ClaimsIdentity)User.Identity;
            return identity.FindFirst("UserName").Value;
        }
    }
}