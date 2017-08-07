using System.Collections.Generic;
using Microsoft.Graph;

namespace SampleAADv2Bot.Services
{
    /// <summary>
    /// Room Service
    /// </summary>
    public interface IRoomService
    {
        /// <summary>
        /// Get rooms
        /// </summary>
        /// <returns>List of all rooms</returns>
        List<Room> GetRooms();
        
        /// <summary>
        /// Add rooms
        /// </summary>
        /// <param name="request">Request object</param>
        /// <param name="rooms">List of rooms</param>
        void AddRooms(UserFindMeetingTimesRequestBody request, List<Room> rooms);

    }
}
