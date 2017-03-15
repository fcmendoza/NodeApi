using System;
using System.Collections.Generic;
using System.Web.Http;

namespace FitbitApi.Controllers
{
    public class FitbitController : ApiController
    {
        [HttpGet]
        public IHttpActionResult Summaries([FromUri] SummaryRequest request)
        {
            var summaries = new List<FoodSummary> {
                new FoodSummary { Date = DateTime.Now.Date.AddDays(-5), CaloriesTotal = 1500, ProteinTotal = 80, CarbsTotal = 180 },
                new FoodSummary { Date = DateTime.Now.Date.AddDays(-4), CaloriesTotal = 1700, ProteinTotal = 100, CarbsTotal = 200 },
            };
            return Ok(summaries);
        }

        [Route("customers/{customerId}/orders")]
        [HttpGet]
        public IEnumerable<string> FindOrdersByCustomer(int customerId)
        {
            var settings = new SettingsRetriever().Retrieve();

            var summaries = new List<string> {
                settings.AccessToken,
                settings.RefreshToken,
            };
            return summaries;
        }

        [HttpGet, Route("fitbit/summaries")]
        public IHttpActionResult GetSummaries([FromUri] SummaryRequest request)
        {
            var summaries = new List<FoodSummary> {
                new FoodSummary { Date = DateTime.Now.Date.AddDays(-5), CaloriesTotal = 1500, ProteinTotal = 80, CarbsTotal = 180 },
                new FoodSummary { Date = DateTime.Now.Date.AddDays(-4), CaloriesTotal = 1700, ProteinTotal = 100, CarbsTotal = 200 },
            };
            return Ok(summaries);
        }
    }

    public class SummaryRequest
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class FoodSummary
    {
        public DateTime Date { get; set; }
        public decimal CaloriesTotal { get; set; }
        public decimal ProteinTotal { get; set; }
        public decimal CarbsTotal { get; set; }
        public decimal ProteingPercentage => ProteinTotal / (ProteinTotal + CarbsTotal) * 100;
        public decimal CarbsPercentage => CarbsTotal / (ProteinTotal + CarbsTotal) * 100;

        public bool ProteinGoalAchived => ProteinTotal >= 90;// && ProteinTotal <= 100;
    }
}
