﻿using System;
using System.Collections.Generic;
using Microsoft.Graph;
using SampleAADv2Bot.Services;
using BotAuth.Models;
using System.Configuration;
using System.Globalization;
using System.Linq;

namespace SampleAADv2Bot.Util
{
    /// <summary>
    /// Data Converter helper class 
    /// </summary>
    public class DataConverter
    {
        /// <summary>
        /// Get meeting rooms
        /// </summary>
        /// <param name="timeSuggestion"></param>
        /// <param name="roomsDictionary"></param>
        /// <returns>List of available rooms</returns>
        public static List<RoomRecord> GetMeetingSuggestionRooms(MeetingTimeSuggestion timeSuggestion, Dictionary<string, string> roomsDictionary)
        {
            var rooms = new List<RoomRecord>();
            var counter = 1;
            foreach(var attendee in timeSuggestion.AttendeeAvailability)		
             {
                 if (!roomsDictionary.ContainsKey(attendee.Attendee.EmailAddress.Address)) continue;
                 rooms.Add(new RoomRecord()
                 {
                     Address = attendee.Attendee.EmailAddress.Address,
                     Name = roomsDictionary[attendee.Attendee.EmailAddress.Address],
                     Counter =  counter
                 });
                 counter++;
             }		
               
            return rooms;
        }

        /// <summary>
        /// Get authentication options 
        /// </summary>
        /// <returns><see cref="AuthenticationOptions" /></returns>
        public static AuthenticationOptions GetAuthenticationOptions()
        {
            var options = new AuthenticationOptions()
            {
                Authority = ConfigurationManager.AppSettings["aad:Authority"],
                ClientId = ConfigurationManager.AppSettings["aad:ClientId"],
                ClientSecret = ConfigurationManager.AppSettings["aad:ClientSecret"],
                Scopes = new [] { "User.Read", "Calendars.ReadWrite", "Calendars.ReadWrite.Shared" },
                RedirectUrl = ConfigurationManager.AppSettings["aad:Callback"]
            };

            return options;
        }

        /// <summary>
        /// Get request object for find meeting times API
        /// </summary>
        /// <param name="date">String representation of date</param>
        /// <param name="normalizedEmails">List of participants emails</param>
        /// <param name="normalizedDuration">Duration of the meeting</param>
        /// <returns><see cref="UserFindMeetingTimesRequestBody" /></returns>
        public static UserFindMeetingTimesRequestBody GetUserFindMeetingTimesRequestBody(DateTime date, string[] normalizedEmails, int normalizedDuration)
        {
            var startDate = $"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}T00:00:00.000Z";
            var endDate = $"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}T10:00:00.000Z";
            var inputAttendee = normalizedEmails.Select(i => new Attendee()
                {
                    EmailAddress = new EmailAddress()
                    {
                        Address = i
                    }
                })
                .ToList();

            var inputDuration = new Duration(new TimeSpan(0, normalizedDuration, 0));

            var userFindMeetingTimesRequestBody = new UserFindMeetingTimesRequestBody()
            {
                Attendees = inputAttendee,
                TimeConstraint = new TimeConstraint()
                {
                    Timeslots = new List<TimeSlot>()
                        {
                            new TimeSlot()
                            {
                                Start = new DateTimeTimeZone()
                                {
                                    DateTime = startDate,
                                    TimeZone = "UTC"
                                },
                                End = new DateTimeTimeZone()
                                {
                                    DateTime = endDate,
                                    TimeZone = "UTC"
                                }
                            }
                        }
                },
                MeetingDuration = inputDuration,
                MaxCandidates = 15,
                IsOrganizerOptional = false,
                ReturnSuggestionReasons = true,
                MinimumAttendeePercentage = 100

            };

            return userFindMeetingTimesRequestBody;

        }

        /// <summary>
        /// Get event request object for scheduling a meeting 
        /// </summary>
        /// <param name="selectedRoom">Selected room</param>
        /// <param name="normalizedEmails">List of participant emails</param>
        /// <param name="subject">Name of the meeting</param>
        /// <param name="startTime">Starting time</param>
        /// <param name="endTime">End time</param>
        /// <returns><see cref="Event" /></returns>
        public static Event GetEvent(Room selectedRoom, string[] normalizedEmails, string subject, DateTime startTime, DateTime endTime)
        {
            var attendees = normalizedEmails.Select(email => new Attendee
                {
                    EmailAddress = new EmailAddress()
                    {
                        Address = email
                    }
                })
                .ToList();
            attendees.Add(new Attendee()
            {
                EmailAddress = new EmailAddress()
                {
                    Name = selectedRoom.Name,
                    Address = selectedRoom.Address
                }
            });

            var meeting = new Event()
            {
                Subject = subject,
                Start = new DateTimeTimeZone()
                {
                    DateTime = startTime.ToString(CultureInfo.InvariantCulture),
                    TimeZone = "UTC"
                },
                End = new DateTimeTimeZone()
                {
                    DateTime = endTime.ToString(CultureInfo.InvariantCulture),
                    TimeZone = "UTC"
                },
                Location = new Location()
                {
                    DisplayName = selectedRoom.Name,
                    LocationEmailAddress = selectedRoom.Address
                },
                Attendees = attendees
            };

            return meeting;
        }

        /// <summary>
        /// Format meeting date-time details in friendlier format
        /// </summary>
        /// <param name="startTime">Start time</param>
        /// <param name="endTime">End time</param>
        /// <param name="timeOffset">Time offset</param>
        /// <param name="counter">Optional counter for better UI in Skype for Business</param>
        /// <returns>Friendly string of date & time of the meeting</returns>
        public static string GetFormatedTime(DateTime startTime, DateTime endTime, int? counter, int timeOffset = 9)
        {
            var counterValue = counter.HasValue & counter > 0 ? $"{counter}. " : string.Empty;
            var formattedTime =  $"{counterValue}{startTime.AddHours(timeOffset):yyyy-MM-dd} -  {startTime.AddHours(timeOffset).ToShortTimeString()}  - {endTime.AddHours(9).ToShortTimeString()}";
            return formattedTime;
        }

        /// <summary>
        /// Get HTML table with meeting information
        /// </summary>
        /// <returns>string of HTML table</returns>
        public static string GetScheduleTicket(string subject, string duration, string number, string emails, string schedule)
        {
            var htmlTicket = "<table><tbody><tr><th>Subject</th><td>";
            htmlTicket += subject ?? "";

            htmlTicket += "</td></tr><tr><th>Duration</th><td>";
            htmlTicket += duration ?? "";

            htmlTicket += "</td></tr><tr><th>Number of Invitations</th><td>";
            htmlTicket += number ?? "";

            htmlTicket += "</td></tr><tr><th>Attendees</th><td>";
            htmlTicket += emails ?? "";

            htmlTicket += "</td></tr><tr><th>Schedule</th><td>";
            htmlTicket += schedule ?? "";

            htmlTicket += "</td></tr></tbody></table>";

            return htmlTicket;
        }

    }
}