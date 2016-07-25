using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using AttentionPassengers;

namespace AttentionPassengers.Slack.Controllers
{
    [Route("api/[controller]")]
    public class SlackController : Controller
    {
        [HttpPost]
        public async Task<JObject> Receive()
        {
            RequestBody request = new RequestBody(Request.Form);
            string route = (string)AppContent.Routes[request.Text];
            Dto.Alerts.AlertHeadersByRouteObject alertHeaders = null;
            if (route == null)
            {
                try
                {
                    alertHeaders = await AppContent.AttentionPassengers.AlertsHeadersByRoute(request.Text);
                }
                catch (Exception ex)
                {
                    return BasicMessage("Route unknown");
                }
            }
            else
            {
                alertHeaders = await AppContent.AttentionPassengers.AlertsHeadersByRoute(route);
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

        private JObject BasicMessage(string text)
        {
            JObject message = new JObject();
            message["text"] = text;
            return message;
        }
    }
}