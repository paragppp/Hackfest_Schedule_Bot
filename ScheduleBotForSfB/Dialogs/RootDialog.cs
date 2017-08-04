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
using System.Text;

namespace SampleAADv2Bot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<string>
    {
        //normalized inputs
        private Dictionary<string, string> roomsDictionary = null;
        //Localization
        private string detectedLanguage = "en-US";

        //Autentication
        AuthResult result = null;

        static RoomService roomService = new RoomService();
        // TBD - Replace with dependency injection 
        static MeetingService meetingService = new MeetingService(roomService);

        //for displaying current input
        private string displaySubject = "";
        private string displayDuration = "";
        private string displayNumber = "";
        private string displayEmail = "";
        private string displaySchedule = "";

        public async Task StartAsync(IDialogContext context)
        {
            Init(context);
            context.Wait(MessageReceivedAsync);
        }

        public void Init(IDialogContext context)
        {
            context.UserData.RemoveValue(Settings.InvitationsEmails_stringArray);
            context.UserData.RemoveValue(Settings.meeintingSubject_string);
            context.UserData.RemoveValue(Settings.meetingDate_string);
            context.UserData.RemoveValue(Settings.meetingDuration_int);
            context.UserData.RemoveValue(Settings.meetingInvitationsNum_int);
            context.UserData.RemoveValue(Settings.meetingSelectedRoom_room);
            context.UserData.RemoveValue(Settings.meetingSelectedSchedule_meetingSchedule);
            context.UserData.RemoveValue(Settings.userEmail_string);
            context.UserData.RemoveValue(Settings.userName_string);

            displaySubject = "";
            displayDuration = "";
            displayNumber = "";
            displayEmail = "";
            displaySchedule = "";
        }


        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            roomsDictionary = new Dictionary<string, string>();
            var rooms = roomService.GetRooms();
            foreach (var room in rooms)
            {
                roomsDictionary.Add(room.Address, room.Name);
            }
            var message = await item;
            //Initialize AuthenticationOptions and forward to AuthDialog for token           
            await context.Forward(new AuthDialog(new MSALAuthProvider(), Util.DataConverter.GetAuthenticationOptions()), ResumeAfterAuthAsync, message, CancellationToken.None);
        }

        // Created the Resume After Auth function because UserData could not be set in annonymous function
        public async Task ResumeAfterAuthAsync(IDialogContext context, IAwaitable<AuthResult> authResult)
        {
            this.result = await authResult;
            // Use token to call into service                
            var json = await new HttpClient().GetWithAuthAsync(result.AccessToken, "https://graph.microsoft.com/v1.0/me");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            PromptDialog.Text(context, this.SubjectMessageReceivedAsync, Properties.Resources.Text_Hello1 + json.Value<string>("displayName") + Properties.Resources.Text_Hello2 + Properties.Resources.Text_PleaseEnterSubject);
        }

        public async Task SubjectMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            var message = await argument;
            message = message.ToSpaceAltered();
            context.UserData.SetValue<string>(Settings.meeintingSubject_string, message);
            await context.PostAsync(this.GetScheduleTicket(context));
            PromptDialog.Text(context, this.DurationReceivedAsync, Properties.Resources.Text_PleaseEnterDuration);
        }

        public async Task DurationReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            var message = await argument;
            if (message.IsNaturalNumber())
            {
                context.UserData.SetValue<int>(Settings.meetingDuration_int, Int32.Parse(message));
                await context.PostAsync(this.GetScheduleTicket(context));
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
            var message = await argument;

            if (message.IsNaturalNumber())
            {
                context.UserData.SetValue<int>(Settings.meetingInvitationsNum_int, Int32.Parse(message));
                await context.PostAsync(this.GetScheduleTicket(context));
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
            var message = await argument;
            //remove space
            message = message.Replace(" ", "").Replace("　", "");
            message = message.Replace("&#160;", "").Replace("&#160:^", "");
            message = System.Text.RegularExpressions.Regex.Replace(message, "\\(.+?\\)", "");

            //get Invitation Num
            int invitationsNum = 0;
            context.UserData.TryGetValue<int>(Settings.meetingInvitationsNum_int, out invitationsNum);

            if (message.IsEmailAddressList())
            {
                string[] normalizedEmails = message.Split(',');
                if (normalizedEmails.Length == invitationsNum)
                {
                    context.UserData.SetValue<string[]>(Settings.InvitationsEmails_stringArray, normalizedEmails);
                    await context.PostAsync(this.GetScheduleTicket(context));
                    PromptDialog.Text(context, this.DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);
                }
                else
                {
                    await context.PostAsync("Please enter " + invitationsNum + " E-mail addresses.");
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
            var message = await argument;
            DateTime dateTime;
            DateTime.TryParse(message, out dateTime);
            if (dateTime != DateTime.MinValue && dateTime != DateTime.MaxValue)
            {
                context.UserData.SetValue<string>(Settings.meetingDate_string, message);
                await context.PostAsync(Properties.Resources.Text_CheckWhen1 + message + Properties.Resources.Text_CheckWhen2);
                await GetMeetingSuggestions(context, argument);
            }
            else
            {
                PromptDialog.Text(context, this.DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);
            }
        }

        // TBD - inject function logic for the interaction with Graph API 
        private async Task GetMeetingSuggestions(IDialogContext context, IAwaitable<string> argument)
        {
            string date = null;
            context.UserData.TryGetValue<string>(Settings.meetingDate_string, out date);
            string startDate = date + "T00:00";
            string endDate = date + "T10:00";

            string[] emails;
            context.UserData.TryGetValue<string[]>(Settings.InvitationsEmails_stringArray, out emails);

            string userEmail;
            context.UserData.TryGetValue<string>(Settings.userEmail_string, out userEmail);

            List<Attendee> inputAttendee = new List<Attendee>();
            //adding participants
            foreach (var i in emails)
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

            int duration;
            context.UserData.TryGetValue<int>(Settings.meetingDuration_int, out duration);

            Duration inputDuration = new Duration(new TimeSpan(0, duration, 0));

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
            if (meetingScheduleSuggestions.Count == 0)
            {
                await context.PostAsync(Properties.Resources.Text_AlertNoSuggestion);
                PromptDialog.Text(context, this.DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);

            }
            else
            {
                PromptDialog.Choice(context, ScheduleSelectedAsync, meetingScheduleSuggestions, Properties.Resources.Text_PleaseSelectSchedule, null, 3);
            }
        }

        public async Task ScheduleSelectedAsync(IDialogContext context, IAwaitable<MeetingSchedule> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            var data = await argument;
            context.UserData.SetValue<MeetingSchedule>(Settings.meetingSelectedSchedule_meetingSchedule, data);
            PromptDialog.Choice(context, RoomMessageReceivedAsync, data.Rooms, Properties.Resources.Text_PleaseSelectSchedule, null, 3);
        }

        public async Task RoomMessageReceivedAsync(IDialogContext context, IAwaitable<Room> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            var room = await argument;
            context.UserData.SetValue<Room>(Settings.meetingSelectedRoom_room, room);
            await context.PostAsync(this.GetScheduleTicket(context));
            PromptDialog.Confirm(context, BookingMeetingAsync, "Are you sure to book with the above setting?", null, 3, PromptStyle.AutoText);
        }

        public async Task BookingMeetingAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var answer = await argument;
            if (answer == true)
            {
                try
                {
                    Room selectedRoom;
                    context.UserData.TryGetValue<Room>(Settings.meetingSelectedRoom_room, out selectedRoom);

                    string[] emails;
                    context.UserData.TryGetValue<string[]>(Settings.InvitationsEmails_stringArray, out emails);
                    var attendees = new List<Attendee>();
                    foreach (var email in emails)
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

                    string subject = null;
                    context.UserData.TryGetValue<string>(Settings.meeintingSubject_string, out subject);

                    MeetingSchedule selectedMeetingSchedule;
                    context.UserData.TryGetValue<MeetingSchedule>(Settings.meetingSelectedSchedule_meetingSchedule, out selectedMeetingSchedule);

                    var meeting = new Event()
                    {
                        Subject = subject,
                        Start = new DateTimeTimeZone()
                        {
                            DateTime = selectedMeetingSchedule.StartTime.ToString(),
                            TimeZone = "UTC"
                        },
                        End = new DateTimeTimeZone()
                        {
                            DateTime = selectedMeetingSchedule.EndTime.ToString(),
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
                    await context.PostAsync(Properties.Resources.Text_Arranged);
                    context.Done<object>(null);
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                    throw ex;
                }
            }
            else
            {
                await context.PostAsync(Properties.Resources.Text_Canceled);
                context.Done<object>(null);
            }
        }

        private string GetScheduleTicket(IDialogContext context)
        {
            if (displaySubject.Equals(""))
            {
                if (context.UserData.TryGetValue<string>(Settings.meeintingSubject_string, out displaySubject))
                {

                }
                else
                {
                    displaySubject = "";
                }
            }

            if (displayDuration.Equals(""))
            {
                int rawDuration = 0;
                if (context.UserData.TryGetValue<int>(Settings.meetingDuration_int, out rawDuration))
                {
                    if (rawDuration > 0)
                        displayDuration = rawDuration.ToString();
                }
                else
                {
                    displayDuration = "";
                }
            }

            if (displayNumber.Equals(""))
            {
                int rawNumber = 0;
                if (context.UserData.TryGetValue<int>(Settings.meetingInvitationsNum_int, out rawNumber))
                {
                    if (rawNumber > 0)
                        displayNumber = rawNumber.ToString();
                }
                else
                {
                    displayNumber = "";
                }
            }


            if (displayEmail.Equals(""))
            {

                string[] emails = new string[] { "" };
                string userEmail;
                if (context.UserData.TryGetValue<string[]>(Settings.InvitationsEmails_stringArray, out emails))
                {
                    foreach (var i in emails)
                        displayEmail += i + "<br>";                
                }
                else
                {
                    displayEmail = "";
                }
            }


            if (displaySchedule.Equals(""))
            {
                MeetingSchedule selectedSchedule;
                if (context.UserData.TryGetValue<MeetingSchedule>(Settings.meetingSelectedSchedule_meetingSchedule, out selectedSchedule))
                {
                    var stringBuilder = new StringBuilder();
                    DateTime startTime, endTime;
                    startTime = selectedSchedule.StartTime;
                    endTime = selectedSchedule.EndTime;

                    stringBuilder.AppendLine($"{startTime.AddHours(9).Year.ToString("D4")}/{startTime.AddHours(9).Month.ToString("D2")}/{startTime.AddHours(9).Day.ToString("D2")} " +
                            $"{startTime.AddHours(9).Hour.ToString("D2")}:{startTime.AddHours(9).Minute.ToString("D2")}" +
                            $"  - {endTime.AddHours(9).Hour.ToString("D2")}:{endTime.AddHours(9).Minute.ToString("D2")}\n");

                    Room selectedRoom;
                    if (context.UserData.TryGetValue<Room>(Settings.meetingSelectedRoom_room, out selectedRoom))
                    {
                        stringBuilder.AppendLine(selectedRoom.Name);
                    }
                    displaySchedule = stringBuilder.ToString();
                }
                else
                {
                    displaySchedule = "";
                }
            }


            var htmlTicket = "<table><tbody><tr><th>Subject</th><td>";
            htmlTicket += displaySubject ?? "";

            htmlTicket += "</td></tr><tr><th>Duration</th><td>";
            htmlTicket += displayDuration ?? "";

            htmlTicket += "</td></tr><tr><th>Number of Invitation</th><td>";
            htmlTicket += displayNumber ?? "";

            htmlTicket += "</td></tr><tr><th>Attendees</th><td>";
            htmlTicket += displayEmail ?? "";

            htmlTicket += "</td></tr><tr><th>Schedule</th><td>";
            htmlTicket += displaySchedule ?? "";

            htmlTicket += "</td></tr></tbody></table>";

            return htmlTicket;
        }
    }
}