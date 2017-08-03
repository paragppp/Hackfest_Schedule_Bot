using System.Collections.Generic;
using Microsoft.Graph;
using SampleAADv2Bot.Services;
using BotAuth.Models;
using System.Configuration;

namespace SampleAADv2Bot.Util
{
    public class DataConverter
    {
        public static UserFindMeetingTimesRequestBody GetMeetingRequest()
        {
            var request = new UserFindMeetingTimesRequestBody();
            return request;
        }

        public static List<Room> GetMeetingSuggestionRooms(MeetingTimeSuggestion timeSuggestion, Dictionary<string, string> roomsDictionary)
        {
            var rooms = new List<Room>();
            foreach(var attendee in timeSuggestion.AttendeeAvailability)
            {
                if(roomsDictionary.ContainsKey(attendee.Attendee.EmailAddress.Address))
                {
                    rooms.Add(new Room() {  Address = attendee.Attendee.EmailAddress.Address, Name = roomsDictionary[attendee.Attendee.EmailAddress.Address]});
                }
            }

            return rooms;
        }

        public static AuthenticationOptions GetAuthenticationOptions()
        {
            var options = new AuthenticationOptions()
            {
                Authority = ConfigurationManager.AppSettings["aad:Authority"],
                ClientId = ConfigurationManager.AppSettings["aad:ClientId"],
                ClientSecret = ConfigurationManager.AppSettings["aad:ClientSecret"],
                Scopes = new string[] { "User.Read", "Calendars.ReadWrite", "Calendars.ReadWrite.Shared" },
                RedirectUrl = ConfigurationManager.AppSettings["aad:Callback"]
            };

            return options;
        }
    }
}