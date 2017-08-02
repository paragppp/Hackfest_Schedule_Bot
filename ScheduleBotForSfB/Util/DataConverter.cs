using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Graph;
using System.Text;

namespace SampleAADv2Bot.Util
{
    public class DataConverter
    {
        public static UserFindMeetingTimesRequestBody GetMeetingRequest()
        {
            var request = new UserFindMeetingTimesRequestBody();
            return request;
        }

        public static string GetMeetingSuggestionRooms(MeetingTimeSuggestion timeSuggestion, Dictionary<string, string> roomsDictionary)
        {
            var roomsStirngBuilder = new StringBuilder();
            foreach(var attendee in timeSuggestion.AttendeeAvailability)
            {
                if(roomsDictionary.ContainsKey(attendee.Attendee.EmailAddress.Address))
                {
                    roomsStirngBuilder.Append($"{roomsDictionary[attendee.Attendee.EmailAddress.Address]} ");
                }
            }

            return roomsStirngBuilder.ToString();
        }
    }
}