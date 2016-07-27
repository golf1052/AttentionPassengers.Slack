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
        public Dictionary<string, HashSet<AppConstants.DayTimes>> SubscribedTimes;

        public User()
        {
            SubscribedTimes = new Dictionary<string, HashSet<Constants.AppConstants.DayTimes>>();
        }
    }
}
