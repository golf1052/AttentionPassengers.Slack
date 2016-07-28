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

namespace AttentionPassengers.Slack.Constants
{
    public static class AppConstants
    {
        public static JObject Routes;
        public static AttentionPassengers AttentionPassengers;
        public static MongoClient Mongo;
        public static IMongoDatabase Database;
        public static IMongoCollection<User> Collection;

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
            Mongo = new MongoClient(Secrets.MongoConnectionString);
            Database = Mongo.GetDatabase("mbta");
            Collection = Database.GetCollection<User>("mbta");
            //BsonClassMap.RegisterClassMap<User>();
        }

        public static void LoadContent()
        {
            Routes = JObject.Parse(File.ReadAllText("Content/routes.json"));
        }
    }
}
