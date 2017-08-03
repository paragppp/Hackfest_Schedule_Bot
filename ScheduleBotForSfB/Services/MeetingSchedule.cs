using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SampleAADv2Bot.Services
{
    [Serializable]
    public class MeetingSchedule
    {
        public string Time { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<Room> Rooms { get; set; }

        public override string ToString()
        {
            return Time;
        }
    }
}