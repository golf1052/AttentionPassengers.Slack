using AttentionPassengers.Slack.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AttentionPassengers.Slack
{
    public class Registration
    {
        public HashSet<string> Lines { get; set; }
        public HashSet<AppConstants.DayTimes> Times { get; set; }
    }
}
