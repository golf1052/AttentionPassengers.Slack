using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AttentionPassengers.Slack
{
    public struct TopicTimeRange
    {
        public LocalTime Start { get; set; }
        public LocalTime End { get; set; }

        public TopicTimeRange(LocalTime start, LocalTime end)
        {
            Start = start;
            End = end;
        }
    }
}
