using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.Graph;

namespace SampleAADv2Bot.Services
{
    /// <summary>
    /// Room Service 
    /// </summary>
    [Serializable]
    public class RoomService : IRoomService
    {
        private readonly ILoggingService loggingService;

        /// <summary>
        /// Room service constructor
        /// </summary>
        /// <param name="loggingService">Instance of <see cref="ILoggingService"/></param>
        public RoomService(ILoggingService loggingService)
        {
            this.loggingService = loggingService;
        }

        /// <summary>
        /// Get all rooms 
        /// </summary>
        /// <returns>List of all rooms</returns>
        public List<Room> GetRooms()
        {
            try
            {
                /// Later rooms can be retrieved from Outlook API as specified here - https://blogs.msdn.microsoft.com/exchangedev/2017/06/30/announcing-new-rest-apis-in-beta-for-rooms-time-zones-and-languages/
                var roomNames = ConfigurationManager.AppSettings["RoomNames"];
                var roomEmails = ConfigurationManager.AppSettings["RoomEmails"];
                if(string.IsNullOrEmpty(roomNames) || string.IsNullOrEmpty(roomEmails))
                {
                    throw new ApplicationException("Please provide values for application settings RoomNames and RoomEmails");
                }

                var roomNameValues = roomNames.Split(new string[] { "," }, StringSplitOptions.None);
                var roomEmailValues = roomEmails.Split(new string[] { ","}, StringSplitOptions.None);

                var rooms = new List<Room>();
                for(var i=0; i<roomNameValues.Length; i++)
                {
                    rooms.Add(new Room() {
                            Name = roomNameValues[i],
                            Address = roomEmailValues[i]
                    });
                }
                return rooms;
            }
            catch(Exception ex)
            {
                loggingService.Error(ex);
                throw ex;
            }
        }

        /// <summary>
        /// Add rooms to meeting time suggestion request
        /// </summary>
        /// <param name="request">Meeting time suggestion request</param>
        /// <param name="rooms">List of rooms</param>
        public void AddRooms(UserFindMeetingTimesRequestBody request, List<Room> rooms)
        {
            try
            {
                var attendees = request.Attendees as List<Attendee>;
                foreach (var room in rooms)
                {
                    attendees.Add(new Attendee()
                    {
                        EmailAddress = new EmailAddress()
                        {
                            Address = room.Address,
                            Name = room.Name
                        },
                        Type = AttendeeType.Optional
                    });
                }
            }
            catch (Exception ex)
            {
                loggingService.Error(ex);
                throw;
            }
           
        }
    }


}