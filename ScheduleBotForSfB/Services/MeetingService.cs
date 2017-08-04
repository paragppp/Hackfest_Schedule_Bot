using System;
using Microsoft.Graph;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SampleAADv2Bot.Services
{
    /// <summary>
    /// Service responsible for scheduling meetings 
    /// </summary>
    [Serializable]
    public class MeetingService : IMeetingService
    {
        private readonly string FindsMeetingTimeEndpoint = "https://graph.microsoft.com/v1.0/me/findMeetingTimes";
        private readonly string ScheduleMeetingEndpoint = "https://graph.microsoft.com/v1.0/me/events";
        private readonly IRoomService roomService;
        private readonly IHttpService httpService;

        /// <summary>
        /// Meeting Service Constructor
        /// </summary>
        /// <param name="httpService">HTTP Service instance</param>
        /// <param name="roomService">Room Service instance</param>
        public MeetingService(IHttpService httpService, IRoomService roomService)
        {
            this.roomService = roomService;
            this.httpService = httpService;
        }

        /// <summary>
        /// Provides meeting times suggestions
        /// </summary>
        /// <param name="accessToken">Access Token for API</param>
        /// <param name="userFindMeetingTimesRequestBody">Request object for calling Find Meeting Times API</param>
        /// <returns>Task of <see cref="MeetingTimeSuggestionsResult"/></returns>
        public async Task<MeetingTimeSuggestionsResult> GetMeetingsTimeSuggestions(string accessToken, UserFindMeetingTimesRequestBody userFindMeetingTimesRequestBody)
        {
            try
            {
                var rooms = roomService.GetRooms();
                roomService.AddRooms(userFindMeetingTimesRequestBody, rooms);
                var httpResponseMessage = await httpService.AuthenticatedPost(FindsMeetingTimeEndpoint, accessToken, userFindMeetingTimesRequestBody, string.Empty);
                var meetingTimeSuggestionsResult = JsonConvert.DeserializeObject<MeetingTimeSuggestionsResult>(await httpResponseMessage.Content.ReadAsStringAsync());
                return meetingTimeSuggestionsResult;
            }
            catch (Exception ex)
            {
                // TBD - log exception
                var messgae = ex.Message;
                throw ex;
            }
        }

        /// <summary>
        /// Schedules meeting
        /// </summary>
        /// <param name="accessToken">Access Token for API</param>
        /// <param name="meeting">Meeting object containing all required data for scheduling meeting</param>
        /// <returns>Task of <see cref="Event"/></returns>
        public async Task<Event> ScheduleMeeting(string accessToken, Event meeting)
        {
            try
            {
                var httpResponseMessage = await httpService.AuthenticatedPost(ScheduleMeetingEndpoint, accessToken, meeting, "UTC");
                var scheduledMeeting = JsonConvert.DeserializeObject<Event>(await httpResponseMessage.Content.ReadAsStringAsync());
                return scheduledMeeting;
            }
            catch (Exception ex)
            {
                // TBD - log exception
                var messgae = ex.Message;
                throw ex;
            }
        }
    }
}