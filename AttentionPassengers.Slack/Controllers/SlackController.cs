using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using AttentionPassengers.Slack.Constants;
using AttentionPassengers.Dto.Alerts;
using AttentionPassengers.Slack.Dto;
using MongoDB.Driver;

namespace AttentionPassengers.Slack.Controllers
{
    [Route("api/[controller]")]
    public class SlackController : Controller
    {
        [HttpPost]
        public async Task Receive()
        {
            RequestBody request = new RequestBody(Request.Form);
            await ProcessText(request);
        }

        private async Task ProcessText(RequestBody request)
        {
            if (request.Text.StartsWith("register me") ||
                request.Text.StartsWith("sign me up") ||
                request.Text.StartsWith("subscribe me"))
            {
                await RegisterUser(request);
            }
            else
            {
                await GetCurrentAlerts(request);
            }
        }

        private async Task RegisterUser(RequestBody request)
        {
            var filter = Builders<User>.Filter.Eq<string>("_id", request.UserId);
            User user = AppConstants.UserCollection.Find(filter).FirstOrDefault();
            if (user == null)
            {
                user = new Dto.User();
                user.Id = request.UserId;
            }
            string line = "";
            HashSet<AppConstants.DayTimes> times = new HashSet<AppConstants.DayTimes>();
            int forIndex = request.Text.IndexOf("for");
            if (forIndex == -1)
            {
                await AppConstants.SendToResponseUrl("Couln't parse register message", request.ResponseUrl);
                return;
            }
            int alertsIndex = request.Text.IndexOf("alerts");
            if (alertsIndex == -1)
            {
                await AppConstants.SendToResponseUrl("Couln't parse register message", request.ResponseUrl);
                return;
            }
            line = request.Text.Substring(forIndex + "for".Length + 1, alertsIndex - forIndex - "for".Length - 2);
            int duringIndex = request.Text.IndexOf("during");
            if (duringIndex == -1)
            {
                await AppConstants.SendToResponseUrl("Couln't parse register message", request.ResponseUrl);
                return;
            }
            string timesString = request.Text.Substring(duringIndex + "during".Length + 1);
            string[] timesSplit = timesString.Split(',');
            foreach (string time in timesSplit)
            {
                times.Add(AppConstants.StringToDayTime(time.Trim()));
            }
            if (!user.SubscribedTimes.ContainsKey(line))
            {
                user.SubscribedTimes.Add(line, new HashSet<AppConstants.DayTimes>());
            }
            foreach (AppConstants.DayTimes time in times)
            {
                user.SubscribedTimes[line].Add(time);
            }
            AppConstants.UserCollection.ReplaceOne(Builders<User>.Filter.Eq<string>("_id", user.Id),
                user,
                new UpdateOptions { IsUpsert = true });
            await AppConstants.SendToResponseUrl("Updated subscriptions!", request.ResponseUrl);
        }

        private async Task GetCurrentAlerts(RequestBody request)
        {
            string route = (string)AppConstants.Routes[request.Text];
            AlertHeadersByRouteObject alertHeaders = null;
            if (route == null)
            {
                try
                {
                    alertHeaders = await AppConstants.AttentionPassengers.AlertsHeadersByRoute(request.Text);
                }
                catch (Exception ex)
                {
                    await AppConstants.SendToResponseUrl("Route unknown", request.ResponseUrl);
                    return;
                }
            }
            else
            {
                alertHeaders = await AppConstants.AttentionPassengers.AlertsHeadersByRoute(route);
            }
            string finalMessage = "";
            if (alertHeaders.AlertHeaders.Count == 0)
            {
                await AppConstants.SendToResponseUrl($"No alerts for {alertHeaders.RouteName} route at this time", request.ResponseUrl);
                return;
            }
            foreach (var alertHeader in alertHeaders.AlertHeaders)
            {
                finalMessage += alertHeader.HeaderText + "\n";
            }
            await AppConstants.SendToResponseUrl(finalMessage, request.ResponseUrl);
        }
    }
}