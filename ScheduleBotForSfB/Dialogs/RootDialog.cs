using BotAuth.AADv2;
using BotAuth.Dialogs;
using BotAuth.Models;
using BotAuth;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SampleAADv2Bot.Extensions;
using System.Globalization;
using SampleAADv2Bot.Services;
using Microsoft.Graph;

namespace SampleAADv2Bot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<string>
    {
        //raw inputs
        private string subject = null;
        private string number = null;
        private string duration = null;
        private string emails = null;
        private string date = null;
        private string schedule = null;
        private string roomName = null;
        private string roomEmail = null;


        //normalized inputs
        private int normalizedNumber = 0;
        private int normalizedDuration = 0;
        private string[] normalizedEmails;
        private DateTime normalizedDate;
        private string normalizedSchedule = null;
        private Dictionary<string, string> roomsDictionary = null;
        private DateTime startTime;
        private DateTime endTime;
        //Localization
        private string detectedLanguage = "en-US";

        //Scheduling
        AuthResult result = null;

        static RoomService roomService = new RoomService();
        // TBD - Replace with dependency injection 
        static MeetingService meetingService = new MeetingService(roomService);

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            roomsDictionary = new Dictionary<string, string>();
            var rooms = roomService.GetRooms();
            foreach(var room in rooms)
            {
                roomsDictionary.Add(room.Address, room.Name);
            }
            var message = await item;
            //Initialize AuthenticationOptions and forward to AuthDialog for token
           
            await context.Forward(new AuthDialog(new MSALAuthProvider(), Util.DataConverter.GetAuthenticationOptions()), async (IDialogContext authContext, IAwaitable<AuthResult> authResult) =>
            {
                this.result = await authResult;
                // Use token to call into service                
                var json = await new HttpClient().GetWithAuthAsync(result.AccessToken, "https://graph.microsoft.com/v1.0/me");
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
                PromptDialog.Text(authContext, this.SubjectMessageReceivedAsync, Properties.Resources.Text_Hello1 + json.Value<string>("displayName") + Properties.Resources.Text_Hello2 + Properties.Resources.Text_PleaseEnterSubject);
            }, message, CancellationToken.None);
        }

        public async Task SubjectMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            this.subject = await argument;
            await context.PostAsync(this.GetScheduleTicket());
            PromptDialog.Text(context, this.DurationReceivedAsync, Properties.Resources.Text_PleaseEnterDuration);
        }

        public async Task DurationReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            this.duration = await argument;
            if (this.duration.IsNaturalNumber())
            {
                normalizedDuration = Int32.Parse(this.duration);
                await context.PostAsync(this.GetScheduleTicket());
                PromptDialog.Text(context, this.NumbersMessageReceivedAsync, Properties.Resources.Text_PleaseEnterNumberOfParticipants);
            }
            else
            {
                await context.PostAsync(Properties.Resources.Text_AlertDuration);
                PromptDialog.Text(context, this.DurationReceivedAsync, Properties.Resources.Text_PleaseEnterDuration);
            }
        }

        public async Task NumbersMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            this.number = await argument;
            if (this.number.IsNaturalNumber())
            {
                normalizedNumber = Int32.Parse(this.number);
                await context.PostAsync(this.GetScheduleTicket());
                PromptDialog.Text(context, this.EmailsMessageReceivedAsync, Properties.Resources.Text_PleaseEnterEmailAddresses);
            }
            else
            {
                await context.PostAsync(Properties.Resources.Text_AlertNumberOfParticipants);
                PromptDialog.Text(context, this.NumbersMessageReceivedAsync, Properties.Resources.Text_PleaseEnterNumberOfParticipants);
            }
        }

        public async Task EmailsMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            this.emails = await argument;
            //remove space
            this.emails = this.emails.Replace(" ", "").Replace("　", "");
            this.emails = this.emails.Replace("&#160;", "").Replace("&#160:^", "");
            this.emails = System.Text.RegularExpressions.Regex.Replace(this.emails, "\\(.+?\\)", "");
            if (this.emails.IsEmailAddressList())
            {
                normalizedEmails = this.emails.Split(',');
                if (normalizedEmails.Length == normalizedNumber)
                {
                    await context.PostAsync(Properties.Resources.Text_CheckEmailAddresses);
                    await context.PostAsync(this.GetScheduleTicket());
                    PromptDialog.Text(context, this.DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);
                }
                else
                {
                    await context.PostAsync("Please enter " + normalizedNumber + " E-mail addresses.");                    
                    PromptDialog.Text(context, this.EmailsMessageReceivedAsync, Properties.Resources.Text_PleaseEnterEmailAddresses);
                }

            }
            else
            {
                await context.PostAsync(Properties.Resources.Text_AlertEmailAddresses);
                PromptDialog.Text(context, this.EmailsMessageReceivedAsync, Properties.Resources.Text_PleaseEnterEmailAddresses);
            }
        }

        public async Task DateMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            this.date = await argument;
            DateTime dateTime;
            DateTime.TryParse(date, out dateTime);
            if (dateTime != DateTime.MinValue && dateTime != DateTime.MaxValue)
            {
                await context.PostAsync(Properties.Resources.Text_CheckWhen1 + this.date + Properties.Resources.Text_CheckWhen2);
                await GetMeetingSuggestions(context, argument);
            }
            else
            {
                PromptDialog.Text(context, this.DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);
            }
        }

        public async Task ScheduleMessageReceivedAsync(IDialogContext context, IAwaitable<MeetingSchedule> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            var data = await argument;
            startTime = data.StartTime;
            endTime = data.EndTime;
            schedule = data.Time;
            PromptDialog.Choice(context, ScheduleMeetingAsync, data.Rooms, Properties.Resources.Text_PleaseSelectSchedule, null, 3);
        }

        public async Task ConfirmedMessageReceivedAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            var confirmed = await argument;

            if (confirmed)
            {
                await context.PostAsync(Properties.Resources.Text_Arranged);
            }
            else
            {
                await context.PostAsync(Properties.Resources.Text_Canceled);
            }

            context.Done<object>(null);
        }

        // TBD - inject function logic for the interaction with Graph API 
        private async Task GetMeetingSuggestions(IDialogContext context, IAwaitable<string> argument)
        {
            string startDate = date + "T00:00:00.000Z";
            string endDate = date + "T10:00:00.00Z";
            List<Attendee> inputAttendee = new List<Attendee>();
            foreach (var i in normalizedEmails)
            {
                inputAttendee.Add(
                     new Attendee()
                     {
                         EmailAddress = new EmailAddress()
                         {
                             Address = i
                         }
                     }
                    );
            }
            Duration inputDuration = new Duration(new TimeSpan(0, normalizedDuration, 0));


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

            var meetingTimeSuggestion = await meetingService.GetMeetingsTimeSuggestions(result.AccessToken, userFindMeetingTimesRequestBody);
            var meetingScheduleSuggestions = new List<MeetingSchedule>();
            foreach (var suggestion in meetingTimeSuggestion.MeetingTimeSuggestions)
            {
                DateTime startTime, endTime;
                DateTime.TryParse(suggestion.MeetingTimeSlot.Start.DateTime, out startTime);
                DateTime.TryParse(suggestion.MeetingTimeSlot.End.DateTime, out endTime);

                meetingScheduleSuggestions.Add(new MeetingSchedule()
                                    {
                                        StartTime = startTime,
                                        EndTime = endTime,
                                        Time = $"{startTime.AddHours(9).ToString("yyyy-MM-dd")} -  {startTime.AddHours(9).ToShortTimeString()}  - {endTime.AddHours(9).ToShortTimeString()}",
                                        Rooms = Util.DataConverter.GetMeetingSuggestionRooms(suggestion, roomsDictionary)
                                    });
            }

            PromptDialog.Choice(context, ScheduleMessageReceivedAsync, meetingScheduleSuggestions, Properties.Resources.Text_PleaseSelectSchedule, null, 3);
        }

        public async Task ScheduleMeetingAsync(IDialogContext context, IAwaitable<Services.Room> message)
        {
            try
            {    
                    var selectedRoom = await message;
                    var attendees = new List<Attendee>();
                    foreach(var email in normalizedEmails)
                    {
                        attendees.Add(new Attendee
                        {
                             EmailAddress = new EmailAddress()
                             {
                                 Address = email
                             }
                        });
                    }
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
                            DateTime = startTime.ToString(),
                            TimeZone = "UTC"
                        },
                        End = new DateTimeTimeZone()
                        {
                            DateTime = endTime.ToString(),
                            TimeZone = "UTC"
                        },
                        Location = new Location()
                        {
                            DisplayName = selectedRoom.Name,
                            LocationEmailAddress = selectedRoom.Address
                        },
                        Attendees = attendees
                    };

                    var scheduledMeeting = await meetingService.ScheduleMeeting(result.AccessToken, meeting);
                    await context.PostAsync($"Meeting with iCalUId - {scheduledMeeting.ICalUId} is scheduled.");
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                throw ex;
            }
        }



        private async Task ResumeAfterOptionDialog(IDialogContext context, IAwaitable<object> argument)
        {
            try
            {
                var message = await argument;
            }
            catch (Exception ex)
            {
                await context.PostAsync($"Failed with message: {ex.Message}");
            }
            finally
            {
                context.Wait(this.MessageReceivedAsync);
            }
        }

        private string GetScheduleTicket()
        {
            var htmlTicket = "<table><tbody><tr><th>Subject</th><td>";
            htmlTicket += this.subject ?? "";

            htmlTicket += "</td></tr><tr><th>Duration</th><td>";
            htmlTicket += this.duration ?? "";

            htmlTicket += "</td></tr><tr><th>Number of people</th><td>";
            htmlTicket += this.number ?? "";

            htmlTicket += "</td></tr><tr><th>Attendances</th><td>";
            htmlTicket += this.emails ?? "";

            htmlTicket += "</td></tr><tr><th>Scheduled</th><td>";
            htmlTicket += this.schedule ?? "";

            htmlTicket += "</td></tr></tbody></table>";

            return htmlTicket;
        }
    }
}