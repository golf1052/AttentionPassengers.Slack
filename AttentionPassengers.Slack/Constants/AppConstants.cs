using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using MongoDB.Bson.Serialization;
using AttentionPassengers.Slack.Dto;
using MongoDB.Bson.Serialization.Conventions;
using System.Net.Http;
using System.Text;
using System.Net;

namespace AttentionPassengers.Slack.Constants
{
    public static class AppConstants
    {
        public static JObject Routes;
        public static AttentionPassengers AttentionPassengers;
        public static MongoClient Mongo;
        public static IMongoDatabase Database;
        public static IMongoCollection<User> UserCollection;

        public enum DayTimes
        {
            EarlyAm,
            AmPeak,
            Midday,
            PmPeak,
            Evening,
            Weekend
        }

        public static Dictionary<DayTimes, TopicTimeRange> TopicTimes = new Dictionary<DayTimes, TopicTimeRange>
        {
            { DayTimes.EarlyAm, new TopicTimeRange(new LocalTime(4, 0), new LocalTime(6, 59)) },
            { DayTimes.AmPeak, new TopicTimeRange(new LocalTime(7, 0), new LocalTime(8, 59)) },
            { DayTimes.Midday, new TopicTimeRange(new LocalTime(9, 0), new LocalTime(15, 59)) },
            { DayTimes.PmPeak, new TopicTimeRange(new LocalTime(16, 0), new LocalTime(18, 29)) },
            { DayTimes.Evening, new TopicTimeRange(new LocalTime(18, 30), new LocalTime(1, 59)) }
        };

        static AppConstants()
        {
            AttentionPassengers = new AttentionPassengers(Secrets.ApiKey);
        }

        public static void LoadContent()
        {
            Routes = JObject.Parse(File.ReadAllText("Content/routes.json"));
        }

        public static async Task<string> SendSlackMessage(string message, string channel)
        {
            JObject response = new JObject();
            response["token"] = Secrets.SlackToken;
            response["channel"] = channel;
            response["text"] = message;
            response["username"] = "Attention Passengers";
            response["icon_url"] = "https://s3-us-west-2.amazonaws.com/slack-files2/avatars/2016-07-25/62701244852_2df79c163286988755ea_48.jpg";
            return await PostWebData(new Uri("https://slack.com/api/chat.postMessage"), response.ToString());
        }

        public static async Task<string> SendToResponseUrl(string message, string responseUrl)
        {
            JObject response = new JObject();
            response["text"] = message;
            return await PostWebData(new Uri(responseUrl), response.ToString());
        }

        public static async Task<string> PostWebData(Uri uri, string content)
        {
            HttpClient client = new HttpClient();
            StringContent stringContent = new StringContent(content, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(uri, stringContent);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return string.Empty;
            }
            else
            {
                throw new Exception($"Error sending post: {response.StatusCode.ToString()} - {await response.Content.ReadAsStringAsync()}");
            }
        }

        public static string DayTimeToString(DayTimes dayTime)
        {
            if (dayTime == DayTimes.EarlyAm)
            {
                return "early am";
            }
            else if (dayTime == DayTimes.AmPeak)
            {
                return "am peak";
            }
            else if (dayTime == DayTimes.Midday)
            {
                return "midday";
            }
            else if (dayTime == DayTimes.PmPeak)
            {
                return "pm peak";
            }
            else if (dayTime == DayTimes.Evening)
            {
                return "evening";
            }
            else if (dayTime == DayTimes.Weekend)
            {
                return "weekend";
            }
            else
            {
                return "unknown";
            }
        }

        public static DayTimes StringToDayTime(string str)
        {
            if (str == "early am")
            {
                return DayTimes.EarlyAm;
            }
            else if (str == "am peak")
            {
                return DayTimes.AmPeak;
            }
            else if (str == "peak am")
            {
                return DayTimes.AmPeak;
            }
            else if (str == "midday")
            {
                return DayTimes.Midday;
            }
            else if (str == "mid day")
            {
                return DayTimes.Midday;
            }
            else if (str == "pm peak")
            {
                return DayTimes.PmPeak;
            }
            else if (str == "peak pm")
            {
                return DayTimes.PmPeak;
            }
            else if (str == "weekend")
            {
                return DayTimes.Weekend;
            }
            else if (str == "the weekend")
            {
                return DayTimes.Weekend;
            }
            else
            {
                return DayTimes.Weekend;
            }
        }
    }
}
