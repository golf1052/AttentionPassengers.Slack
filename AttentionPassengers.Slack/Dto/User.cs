using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AttentionPassengers.Slack.Constants;

namespace AttentionPassengers.Slack.Dto
{
    public class User
    {
        public string Id { get; set; }
        public Dictionary<string, HashSet<AppConstants.DayTimes>> SubscribedTimes { get; set; }
        public HashSet<string> SeenAlerts { get; set; }

        public User()
        {
            SubscribedTimes = new Dictionary<string, HashSet<AppConstants.DayTimes>>();
            SeenAlerts = new HashSet<string>();
        }
    }
}
