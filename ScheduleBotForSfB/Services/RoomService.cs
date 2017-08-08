using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.Graph;

namespace SampleAADv2Bot.Services
{
    /// <summary>
    /// Room Service 
    /// </summary>
    [Serializable]
    public class RoomService : IRoomService
    {
        private readonly ILoggingService _loggingService;

        /// <summary>
        /// Room service constructor
        /// </summary>
        /// <param name="loggingService">Instance of <see cref="ILoggingService"/></param>
        public RoomService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// Get all rooms 
        /// </summary>
        /// <returns>List of all rooms</returns>
        public List<Room> GetRooms()
        {
            try
            {
                var roomNames = ConfigurationManager.AppSettings["RoomNames"];
                var roomEmails = ConfigurationManager.AppSettings["RoomEmails"];
                if(string.IsNullOrEmpty(roomNames) || string.IsNullOrEmpty(roomEmails))
                {
                    throw new ApplicationException("Please provide values for application settings RoomNames and RoomEmails");
                }
                // Removing the space 
                var roomNameValues = roomNames.Replace(" ","").Split(new string[] { "," }, StringSplitOptions.None);
                var roomEmailValues = roomEmails.Replace(" ", "").Split(new string[] { ","}, StringSplitOptions.None);

                return roomNameValues.Select((t, i) => new Room()
                    {
                        Name = t,
                        Address = roomEmailValues[i]
                    })
                    .ToList();
            }
            catch(Exception ex)
            {
                _loggingService.Error(ex);
                throw;
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
                attendees.AddRange(rooms.Select(room => new Attendee()
                {
                    EmailAddress = new EmailAddress()
                    {
                        Address = room.Address,
                        Name = room.Name
                    },
                    Type = AttendeeType.Optional
                }));
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
                throw;
            }
           
        }
    }


}