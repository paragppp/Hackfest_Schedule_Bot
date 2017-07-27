using BotAuth.AADv2;
using BotAuth.Dialogs;
using BotAuth.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Newtonsoft.Json;
using System.Text;
using SampleAADv2Bot.Services;

namespace SampleAADv2Bot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<string>
    {
        MeetingService meetingService = new MeetingService();

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;
            // Get meeting suggestions
            await GetMeetingSuggestions(context, message);
            // Schedule a meeting
            //await ScheduleMeeitng(context, message);

        }

        private AuthenticationOptions GetAuthenticationOptions()
        {
            return new AuthenticationOptions()
            {
                Authority = ConfigurationManager.AppSettings["aad:Authority"],
                ClientId = ConfigurationManager.AppSettings["aad:ClientId"],
                ClientSecret = ConfigurationManager.AppSettings["aad:ClientSecret"],
                // TBD - Read the scopres from Web.Config 
                Scopes = new string[] { "User.Read", "Calendars.ReadWrite", "Calendars.ReadWrite.Shared" },
                RedirectUrl = ConfigurationManager.AppSettings["aad:Callback"]
            };
        }

        // TBD - inject function logic for the interaction with Graph API 
        private async Task GetMeetingSuggestions(IDialogContext context, IMessageActivity message)
        {
            try
            {
                await context.Forward(new AuthDialog(new MSALAuthProvider(), GetAuthenticationOptions()), async (IDialogContext authContext, IAwaitable<AuthResult> authResult) =>
                {
                    #region TBD Replace with real input
                    var userFindMeetingTimesRequestBody = new UserFindMeetingTimesRequestBody()
                    {
                        Attendees = new List<Attendee>()
                    {
                        new Attendee()
                        {
                            EmailAddress = new EmailAddress(){
                                Address ="gled4er@microsoft312.onmicrosoft.com",
                                Name ="Alex Darrow" }
                        }
                    },
                        LocationConstraint = new LocationConstraint()
                        {
                            IsRequired = false,
                            SuggestLocation = true,
                            Locations = new List<LocationConstraintItem>()
                        {
                            new LocationConstraintItem()
                            {
                                DisplayName = "Conf Room 32/1368",
                                LocationEmailAddress = "conf32room1368@imgeek.onmicrosoft.com"
                            }
                        }
                        },
                        TimeConstraint = new TimeConstraint()
                        {
                            Timeslots = new List<TimeSlot>()
                        {
                            new TimeSlot()
                            {
                                Start = new DateTimeTimeZone()
                                {
                                    DateTime = "2017-07-27T04:19:50.442Z",
                                    TimeZone = "Pacific Standard Time"
                                },
                                End = new DateTimeTimeZone()
                                {
                                    DateTime = "2017-08-03T04:19:50.442Z",
                                    TimeZone = "Pacific Standard Time"
                                }
                            }
                        }
                        },
                        MeetingDuration = new Duration(new TimeSpan(2, 0, 0)),
                        MaxCandidates = 15,
                        IsOrganizerOptional = false,
                        ReturnSuggestionReasons = true,
                        MinimumAttendeePercentage = 100

                    };
                    #endregion
                    var result = await authResult;
                    var meetingTimeSuggestion = await meetingService.GetMeetingsTimeSuggestions(result.AccessToken, userFindMeetingTimesRequestBody);
                    var stringBuilder = new StringBuilder();
                    foreach (var suggestion in meetingTimeSuggestion.MeetingTimeSuggestions)
                    {
                        stringBuilder.AppendLine($"start - {suggestion.MeetingTimeSlot.Start.DateTime.ToString()} and end - {suggestion.MeetingTimeSlot.End.DateTime.ToString()}");
                    }
                    await authContext.PostAsync($"Thesre are the options for meeting {stringBuilder.ToString()}");
                }, message, CancellationToken.None);
            }
            catch(Exception ex)
            {
                var msg = ex.Message;
                throw ex;
            }
            
        }

        public async Task ScheduleMeeitng(IDialogContext context, IMessageActivity message)
        {
            try
            {
               
                await context.Forward(new AuthDialog(new MSALAuthProvider(), GetAuthenticationOptions()), async (IDialogContext authContext, IAwaitable<AuthResult> authResult) =>
                {
                    #region TBD Replace with real input 
                    var meeting = new Event()
                    {
                        Subject = "My Event",
                        Body = new ItemBody()
                        {
                            ContentType = BodyType.Html,
                            Content = "Does late morning work for you?"
                        },
                        Start = new DateTimeTimeZone()
                        {
                            DateTime = "2017-07-29T07:30:00.000Z",
                            TimeZone = "UTC"
                        },
                        End = new DateTimeTimeZone()
                        {
                            DateTime = "2017-07-29T08:30:00.000Z",
                            TimeZone = "UTC"
                        },
                        Location = new Location()
                        {
                            DisplayName = "Harry's Bar"
                        },
                        Attendees = new List<Attendee>()
                    {
                        new Attendee()
                        {
                            EmailAddress =  new EmailAddress()
                            {
                                Address = "test1@Microsoft312.onmicrosoft.com",
                               Name = "Test1 User"
                            },
                            Type = AttendeeType.Required
                        },
                    }
                    };
                    #endregion
                    var result = await authResult;
                    var scheduledMeeting =  await meetingService.ScheduleMeeting(result.AccessToken, meeting);
                    await authContext.PostAsync($"Meeting with iCalUId - {scheduledMeeting.ICalUId} is scheduled.");
                }, message, CancellationToken.None);
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                throw ex;
            }
        }


    }
}