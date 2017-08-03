using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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

        public override string ToString()
        {
            return Name;
        }
    }
}