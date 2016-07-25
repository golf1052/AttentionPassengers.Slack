using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AttentionPassengers.Slack
{
    public static class AppContent
    {
        public static JObject Routes;

        public static AttentionPassengers AttentionPassengers;

        static AppContent()
        {
            AttentionPassengers = new AttentionPassengers(Secrets.ApiKey);
        }

        public static void LoadContent()
        {
            Routes = JObject.Parse(File.ReadAllText(@"Content\routes.json"));
        }
    }
}
