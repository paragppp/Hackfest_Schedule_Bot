using System;

namespace SampleAADv2Bot.Services
{
    /// <summary>
    /// Room record represents room for displaying purposes
    /// </summary>
    [Serializable]
    public class RoomRecord : Room
    {
        /// <summary>
        /// Counter of a room in collection
        /// </summary>
        public int Counter { get; set;  }

        public override string ToString()
        {
            return $"{Counter}. {Name}";
        }
    }
}