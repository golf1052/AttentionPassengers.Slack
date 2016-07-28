using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using AttentionPassengers.Slack.Constants;
using AttentionPassengers.Dto.Alerts;
using AttentionPassengers.Slack.Dto;

namespace AttentionPassengers.Slack.Controllers
{
    [Route("api/[controller]")]
    public class SlackController : Controller
    {
        [HttpPost]
        public async Task<JObject> Receive()
        {
            RequestBody request = new RequestBody(Request.Form);
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
                    return BasicMessage("Route unknown");
                }
            }
            else
            {
                alertHeaders = await AppConstants.AttentionPassengers.AlertsHeadersByRoute(route);
            }
            string finalMessage = "";
            if (alertHeaders.AlertHeaders.Count == 0)
            {
                return BasicMessage($"No alerts for {alertHeaders.RouteName} route at this time");
            }
            foreach (var alertHeader in alertHeaders.AlertHeaders)
            {
                finalMessage += alertHeader.HeaderText + "\n";
            }
            return BasicMessage(finalMessage);
        }

        [HttpPost]
        [Route("test")]
        public async Task Test()
        {
            User user = new Dto.User();
            HashSet<AppConstants.DayTimes> subscribedTimes = new HashSet<AppConstants.DayTimes>();
            subscribedTimes.Add(AppConstants.DayTimes.AmPeak);
            subscribedTimes.Add(AppConstants.DayTimes.PmPeak);
            user.SubscribedTimes.Add("red", subscribedTimes);
            user.SeenAlerts.Add("111");
            user.SeenAlerts.Add("222");
            await AppConstants.Collection.InsertOneAsync(user);
        }

        private JObject BasicMessage(string text)
        {
            JObject message = new JObject();
            message["text"] = text;
            return message;
        }
    }
}