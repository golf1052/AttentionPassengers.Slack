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
using MongoDB.Bson;
using NodaTime;
using NodaTime.Extensions;

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
            else if (request.Text.StartsWith("list registration") ||
                request.Text.StartsWith("list signup") ||
                request.Text.StartsWith("list sign up") ||
                request.Text.StartsWith("list subscription"))
            {
                await ListRegistration(request);
            }
            else if (request.Text.StartsWith("unregister me") ||
                request.Text.StartsWith("unsign me up") ||
                request.Text.StartsWith("unsubscribe me"))
            {
                await UnregisterUser(request);
            }
            else if (request.Text == "unregister all" ||
                    request.Text == "unsubscribe all")
            {
                await UnregisterAll(request);
            }
            else if (request.Text == "times")
            {
                await SendTimes(request);
            }
            else if (request.Text == "help")
            {
                await SendHelp(request);
            }
            else
            {
                await GetCurrentAlerts(request);
            }
        }

        private async Task RegisterUser(RequestBody request)
        {
            Registration registration = await GetRegistrationInfo(request);
            if (registration == null)
            {
                return;
            }
            var filter = Builders<User>.Filter.Eq<string>("_id", request.UserId);
            User user = AppConstants.UserCollection.Find(filter).FirstOrDefault();
            if (user == null)
            {
                user = new Dto.User();
                user.Id = request.UserId;
            }

            foreach (string line in registration.Lines)
            {
                string currentAlerts = string.Empty;
                if (!user.SubscribedTimes.ContainsKey(line))
                {
                    user.SubscribedTimes.Add(line, new HashSet<AppConstants.DayTimes>());
                }
                foreach (AppConstants.DayTimes time in registration.Times)
                {
                    user.SubscribedTimes[line].Add(time);
                }
                AlertHeadersByRouteObject alertHeaders = await AppConstants.AttentionPassengers.AlertsHeadersByRoute(line);
                foreach (AlertHeader alert in alertHeaders.AlertHeaders)
                {
                    if (!user.SeenAlerts.Contains(alert.AlertId))
                    {
                        currentAlerts += alert.HeaderText + "\n";
                        user.SeenAlerts.Add(alert.AlertId);
                    }
                }
                if (currentAlerts != string.Empty)
                {
                    // if the user tries to register to more than 4 lines at a time bad stuff will happen
                    await AppConstants.SendSlackMessage(currentAlerts.Insert(0, "Current alerts\n"), user.Id);
                }
            }
            AppConstants.UserCollection.ReplaceOne(Builders<User>.Filter.Eq<string>("_id", user.Id),
                user,
                new UpdateOptions { IsUpsert = true });
            await AppConstants.SendToResponseUrl("Updated subscriptions!", request.ResponseUrl);
        }

        private async Task ListRegistration(RequestBody request)
        {
            var filter = Builders<User>.Filter.Eq<string>("_id", request.UserId);
            User user = AppConstants.UserCollection.Find(filter).FirstOrDefault();
            if (user == null)
            {
                await AppConstants.SendToResponseUrl("Not registered for any alerts", request.ResponseUrl);
                return;
            }
            string finalMessage = string.Empty;
            foreach (var subscription in user.SubscribedTimes)
            {
                if (subscription.Value.Count > 0)
                {
                    finalMessage += subscription.Key + " line\n";
                    foreach (var time in subscription.Value)
                    {
                        finalMessage += $"\t{AppConstants.DayTimeToString(time)}\n";
                    }
                }
            }
            if (finalMessage == string.Empty)
            {
                finalMessage = "Not registered for any alerts";
            }
            await AppConstants.SendToResponseUrl(finalMessage, request.ResponseUrl);
        }

        private async Task UnregisterUser(RequestBody request)
        {
            Registration registration = await GetRegistrationInfo(request);
            if (registration == null)
            {
                return;
            }
            var filter = Builders<User>.Filter.Eq<string>("_id", request.UserId);
            User user = AppConstants.UserCollection.Find(filter).FirstOrDefault();
            if (user == null)
            {
                await AppConstants.SendToResponseUrl("Not registered for any alerts", request.ResponseUrl);
                return;
            }

            foreach (string line in registration.Lines)
            {
                if (user.SubscribedTimes.ContainsKey(line))
                {
                    foreach (AppConstants.DayTimes time in registration.Times)
                    {
                        if (user.SubscribedTimes[line].Contains(time))
                        {
                            user.SubscribedTimes[line].Remove(time);
                        }
                    }
                    if (user.SubscribedTimes[line].Count == 0)
                    {
                        user.SubscribedTimes.Remove(line);
                    }
                }
            }
            AppConstants.UserCollection.ReplaceOne(Builders<User>.Filter.Eq<string>("_id", user.Id),
                user,
                new UpdateOptions { IsUpsert = true });
            await AppConstants.SendToResponseUrl("Updated subscriptions!", request.ResponseUrl);
        }

        private async Task UnregisterAll(RequestBody request)
        {
            var filter = Builders<User>.Filter.Eq<string>("_id", request.UserId);
            User user = AppConstants.UserCollection.Find(filter).FirstOrDefault();
            if (user == null)
            {
                await AppConstants.SendToResponseUrl("Not registered for any alerts", request.ResponseUrl);
                return;
            }
            List<string> keys = user.SubscribedTimes.Keys.ToList();
            for (int i = 0; i < keys.Count; i++)
            {
                user.SubscribedTimes.Remove(keys[i]);
            }
            AppConstants.UserCollection.ReplaceOne(Builders<User>.Filter.Eq<string>("_id", user.Id),
                user,
                new UpdateOptions { IsUpsert = true });
            await AppConstants.SendToResponseUrl("Updated subscriptions!", request.ResponseUrl);
        }

        private async Task<Registration> GetRegistrationInfo(RequestBody request)
        {
            Registration registration = new Registration();
            string linesString = "";
            int forIndex = request.Text.IndexOf("for");
            if (forIndex == -1)
            {
                await AppConstants.SendToResponseUrl("Couln't parse message", request.ResponseUrl);
                return null;
            }
            int alertsIndex = request.Text.IndexOf("alerts");
            if (alertsIndex == -1)
            {
                await AppConstants.SendToResponseUrl("Couln't parse message", request.ResponseUrl);
                return null;
            }
            linesString = request.Text.Substring(forIndex + "for".Length + 1, alertsIndex - forIndex - "for".Length - 2);
            int duringIndex = request.Text.IndexOf("during");
            if (duringIndex == -1)
            {
                await AppConstants.SendToResponseUrl("Couln't parse message", request.ResponseUrl);
                return null;
            }
            registration.Times = new HashSet<AppConstants.DayTimes>();
            string timesString = request.Text.Substring(duringIndex + "during".Length + 1);
            string[] timesSplit = timesString.Split(',');
            foreach (string time in timesSplit)
            {
                AppConstants.DayTimes dayTime = AppConstants.StringToDayTime(time.Trim());
                if (dayTime == AppConstants.DayTimes.None)
                {
                    await AppConstants.SendToResponseUrl($"One of your times is invalid: {time.Trim()}", request.ResponseUrl);
                    return null;
                }
                registration.Times.Add(dayTime);
            }
            registration.Lines = new HashSet<string>();
            string[] linesSplit = linesString.Split(',');
            foreach (string line in linesSplit)
            {
                string finalLine = line.Trim();
                string route = (string)AppConstants.Routes[finalLine];
                if (route == null)
                {
                    try
                    {
                        AlertHeadersByRouteObject testLine = await AppConstants.AttentionPassengers.AlertsHeadersByRoute(finalLine);
                    }
                    catch (Exception ex)
                    {
                        await AppConstants.SendToResponseUrl($"Unknown MBTA line: {finalLine}", request.ResponseUrl);
                        return null;
                    }
                }
                else
                {
                    finalLine = route;
                }
                finalLine = finalLine.ToLower();
                registration.Lines.Add(finalLine);
            }
            return registration;
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

        private async Task SendTimes(RequestBody request)
        {
            string timesString = string.Empty;
            foreach (var time in AppConstants.TopicTimes)
            {
                timesString += AppConstants.DayTimeToString(time.Key) + ": " + time.Value.Start.ToString("t", null) + " - " + time.Value.End.ToString("t", null) + "\n";
            }
            await AppConstants.SendToResponseUrl(timesString, request.ResponseUrl);
        }

        private async Task SendHelp(RequestBody request)
        {
            string helpText = "/mbta help - this help text\n";
            helpText += "/mbta times - valid times for register command\n";
            helpText += "/mbta register me for _lines (comma delimited)_ alerts during _times (comma delimited)_ - receive live alerts for specified lines during specified times\n";
            helpText += "\tExamples: /mbta register me for red, orange, e line alerts during am peak, midday, pm peak, evening\n";
            helpText += "\t/mbta register me for red line alerts during midday\n";
            helpText += "/mbta list registration - list active registrations\n";
            helpText += "/mbta unregister me for _lines (comma delimited)_ alerts during _times (comma delimited)_ - unregister from alerts\n";
            helpText += "/mbta unregister all - unregister from all alert registrations\n";
            helpText += "/mbta _line_ - get current alerts for the given line\n";
            helpText += "\tExamples: /mbta red line - current red line alerts\n";
            helpText += "\t/mbta 1 - current alerts for the 1 bus\n";
            await AppConstants.SendToResponseUrl(helpText, request.ResponseUrl);
        }

        public static async Task CheckAlerts()
        {
            while (true)
            {
                List<User> users = AppConstants.UserCollection.Find(new BsonDocument()).ToList();
                HashSet<string> lines = new HashSet<string>();
                foreach (User user in users)
                {
                    foreach (var time in user.SubscribedTimes)
                    {
                        lines.Add(time.Key);
                    }
                }
                Dictionary<string, List<AlertHeader>> alertHeaders = new Dictionary<string, List<AlertHeader>>();
                foreach (string line in lines)
                {
                    alertHeaders.Add(line, new List<AlertHeader>());
                    AlertHeadersByRouteObject alertHeadersByRoute = await AppConstants.AttentionPassengers.AlertsHeadersByRoute(line);
                    alertHeaders[line].AddRange(alertHeadersByRoute.AlertHeaders);
                }
                foreach (User user in users)
                {
                    foreach (var subcribedTime in user.SubscribedTimes)
                    {
                        foreach (var time in subcribedTime.Value)
                        {
                            IClock clock = SystemClock.Instance;
                            ZonedClock zonedClock = clock.InZone(DateTimeZoneProviders.Tzdb["America/New_York"]);
                            LocalDateTime now = zonedClock.GetCurrentLocalDateTime();
                            LocalDate today = new LocalDate();
                            if (time == AppConstants.DayTimes.Evening && now.Hour < 3)
                            {
                                today = zonedClock.GetCurrentDate().PlusDays(-1);
                            }
                            else
                            {
                                today = zonedClock.GetCurrentDate();
                            }
                            LocalDateTime periodStart = today + AppConstants.TopicTimes[time].Start;
                            LocalDateTime periodEnd = today + AppConstants.TopicTimes[time].End;
                            if (AppConstants.TopicTimes[time].End < AppConstants.TopicTimes[time].Start)
                            {
                                periodEnd = periodEnd.PlusDays(1);
                            }
                            if (periodStart < now && now < periodEnd)
                            {
                                foreach (AlertHeader alert in alertHeaders[subcribedTime.Key])
                                {
                                    if (!user.SeenAlerts.Contains(alert.AlertId))
                                    {
                                        await AppConstants.SendSlackMessage(alert.HeaderText, user.Id);
                                        user.SeenAlerts.Add(alert.AlertId);
                                    }
                                }
                            }
                        }
                    }
                    AppConstants.UserCollection.ReplaceOne(Builders<User>.Filter.Eq<string>("_id", user.Id),
                        user,
                        new UpdateOptions { IsUpsert = true });
                }
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }
    }
}
