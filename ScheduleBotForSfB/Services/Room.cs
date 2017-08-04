using System;

namespace SampleAADv2Bot.Services
{
    /// <summary>
    /// Room 
    /// </summary>
    [Serializable]
    public class Room
    {
        /// <summary>
        /// Room name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Room email
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Custom ToString method that returns room name
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
    }
}