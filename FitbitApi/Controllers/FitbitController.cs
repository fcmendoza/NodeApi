﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Http;
using FitbitApi.Models;
using RestSharp;

namespace FitbitApi.Controllers
{
    public class FitbitController : ApiController
    {
        public FitbitController()
        {
            string baseUrl = "https://api.fitbit.com";
            _client = new RestClient(baseUrl);
            _authorizationCode = ConfigurationManager.AppSettings["AuthorizationCode"];
            _base64EncodedCredentials = ConfigurationManager.AppSettings["Base64EncodedCredentials"];
            _defaultDaysRangeCount = int.TryParse(ConfigurationManager.AppSettings["DefaultDaysRangeCount"], out _defaultDaysRangeCount) ? _defaultDaysRangeCount : -14;
        }

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
            InitializeTokens();

            var from = request?.From ?? DateTime.Now.AddDays(_defaultDaysRangeCount);
            var to = request?.To ?? DateTime.Now;

            from = from <= to ? from : to.AddDays(_defaultDaysRangeCount); // just in case

            //Console.WriteLine($"Retrieving daily totals from {from:MMM dd} to {to:MMM dd} ...");

            var summaries = GetFoodSummaries(from, to);

            var averages = new FoodSummary
            {
                Date = DateTime.MinValue,
                CaloriesTotal = summaries.Average(x => x.CaloriesTotal),
                ProteinTotal = summaries.Average(x => x.ProteinTotal),
                CarbsTotal = summaries.Average(x => x.CarbsTotal),
            };

            summaries.Add(averages);

            return Ok(summaries);
        }

        private List<FoodSummary> GetFoodSummaries(DateTime from, DateTime to)
        {
            var summaries = new List<FoodSummary>();

            foreach (var day in EachDay(from, to))
            {
                summaries.Add(GetFoodSummary(day));
            }

            return summaries;
        }

        private FoodSummary GetFoodSummary(DateTime date)
        {
            //Console.WriteLine($"Retrieving totals for {date:MMM dd} ...");

            string url = $"1/user/-/foods/log/date/{date:yyyy-MM-dd}.json";
            var request = new RestRequest(url, Method.GET);
            request.AddHeader("Authorization", $"Bearer {_accessToken}");

            var response = _client.Execute<FoodResponse>(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (response.Content.Contains("expired_token"))
                {
                    RefreshAccessToken();
                    GetFoodSummary(date); // retry;
                }
            }

            var summary = response.Data.summary;

            return new FoodSummary { Date = date, CaloriesTotal = summary.calories, ProteinTotal = summary.protein, CarbsTotal = summary.carbs };
        }

        private void InitializeTokens()
        {
            // Retrieve last access and refresh tokens from storage.
            var settings = new SettingsRetriever().Retrieve();
            _accessToken = settings.AccessToken;
            _refreshToken = settings.RefreshToken;
        }

        private void RefreshAccessToken()
        {
            string oauthUrl = "oauth2/token";

            var request = new RestRequest(oauthUrl, Method.POST);

            request.AddHeader("Authorization", $"Basic {_base64EncodedCredentials}");
            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", _refreshToken);

            var response = _client.Execute<AccessTokenResponse>(request);
            var content = response.Content;

            var accessTokenResponse = response.Data;

            _accessToken = accessTokenResponse.access_token;
            _refreshToken = accessTokenResponse.refresh_token;

            new SettingsRetriever().UpdateSettings(_accessToken, _refreshToken);
        }

        private IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
                yield return day;
        }

        private readonly RestClient _client;
        private string _authorizationCode;
        private string _accessToken;
        private string _refreshToken;
        private readonly string _base64EncodedCredentials;
        private readonly int _defaultDaysRangeCount;
        private string _redirectUrl =
            "https://fitbitfunctionsapp.azurewebsites.net/api/HttpTriggerCSharp1?code=LuvqkWc8OKsA3igutQP3GCha1oRN2Y5ASqgG1PaXwtXYHhyQvLZ6Cg==";
    }

    public class SummaryRequest
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
