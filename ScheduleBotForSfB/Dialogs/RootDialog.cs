using BotAuth.AADv2;
using BotAuth.Dialogs;
using BotAuth.Models;
using BotAuth;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using SampleAADv2Bot.Extensions;
using LanguageDetection;
using System.Globalization;
using System.Text;
using SampleAADv2Bot.Services;
using Microsoft.Graph;

namespace SampleAADv2Bot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<string>
    {
        //Localization
        private string detectedLanguage = "en-US";
        //Autentication
        AuthResult result = null;
        // TBD - Replace with dependency injection 
        MeetingService meetingService = new MeetingService(new RoomService());

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
            context.UserData.RemoveValue(Settings.meetingSelectedDate_datatime);
            context.UserData.RemoveValue(Settings.meetingSelectedSchedule_meetingTimeSuggestion);
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
            var message = await item;
            //Initialize AuthenticationOptions and forward to AuthDialog for token
            AuthenticationOptions options = new AuthenticationOptions()
            {
                Authority = ConfigurationManager.AppSettings["aad:Authority"],
                ClientId = ConfigurationManager.AppSettings["aad:ClientId"],
                ClientSecret = ConfigurationManager.AppSettings["aad:ClientSecret"],
                Scopes = new string[] { "User.Read", "Calendars.ReadWrite", "Calendars.ReadWrite.Shared" },
                RedirectUrl = ConfigurationManager.AppSettings["aad:Callback"]
            };
            await context.Forward(new AuthDialog(new MSALAuthProvider(), options), ResumeAfterAuth, message, CancellationToken.None);
        }

        public async Task ResumeAfterAuth(IDialogContext context, IAwaitable<AuthResult> authResult)
        {
            this.result = await authResult;
            // Use token to call into service                
            var json = await new HttpClient().GetWithAuthAsync(result.AccessToken, "https://graph.microsoft.com/v1.0/me");
            if (json != null)
            {
                try
                {
                    context.UserData.SetValue<string>(Settings.userName_string, json.Value<string>("displayName"));
                    context.UserData.SetValue<string>(Settings.userEmail_string, json.Value<string>("mail"));
                }
                catch (Exception e)
                {
                }
            }
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            PromptDialog.Text(context, this.SubjectMessageReceivedAsync, Properties.Resources.Text_Hello1 + json.Value<string>("displayName") + Properties.Resources.Text_Hello2 + Properties.Resources.Text_PleaseEnterSubject);
        }

        public async Task SubjectMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            var message = await argument;
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
            string date = null;
            context.UserData.TryGetValue<string>(Settings.meetingDate_string, out date);
            string startDate = date + "T00:00:00.0000000";
            string endDate = date + "T12:00:00.0000000";

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
            //adding user himself
            inputAttendee.Add(
                 new Attendee()
                 {
                     EmailAddress = new EmailAddress()
                     {
                         Address = userEmail
                     }
                 }
                );


            int duration;
            context.UserData.TryGetValue<int>(Settings.meetingDuration_int, out duration);

            Duration inputDuration = new Duration(new TimeSpan(0, duration, 0));

            var userFindMeetingTimesRequestBody = new UserFindMeetingTimesRequestBody()
            {
                Attendees = inputAttendee,
                //LocationConstraint = new LocationConstraint()
                //{
                //    IsRequired = false,
                //    SuggestLocation = true,
                //    Locations = new List<LocationConstraintItem>()
                //{
                //    new LocationConstraintItem()
                //    {
                //        DisplayName = "Conf Room 32/1368",
                //        LocationEmailAddress = "conf32room1368@imgeek.onmicrosoft.com"
                //    }
                //}
                //},
                TimeConstraint = new TimeConstraint()
                {
                    Timeslots = new List<TimeSlot>()
                        {
                            new TimeSlot()
                            {
                                Start = new DateTimeTimeZone()
                                {
                                    DateTime = startDate,
                                    TimeZone = "Tokyo Standard Time"
                                },
                                End = new DateTimeTimeZone()
                                {
                                    DateTime = endDate,
                                    TimeZone = "Tokyo Standard Time"
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
            var stringBuilder = new StringBuilder();
            int scheduleCount = 0;
            foreach (var suggestion in meetingTimeSuggestion.MeetingTimeSuggestions)
            {
                DateTime startTime, endTime;
                DateTime.TryParse(suggestion.MeetingTimeSlot.Start.DateTime, out startTime);
                DateTime.TryParse(suggestion.MeetingTimeSlot.End.DateTime, out endTime);

                stringBuilder.AppendLine($"<b>{scheduleCount}</b>: " +
                    $"{startTime.AddHours(9).Year.ToString("D4")}/{startTime.AddHours(9).Month.ToString("D2")}/{startTime.AddHours(9).Day.ToString("D2")} " +
                    $"{startTime.AddHours(9).Hour.ToString("D2")}:{startTime.AddHours(9).Minute.ToString("D2")}" +
                    $"  - {endTime.AddHours(9).Hour.ToString("D2")}:{endTime.AddHours(9).Minute.ToString("D2")}\n");
                scheduleCount++;
            }
            if (meetingTimeSuggestion.MeetingTimeSuggestions.Count() > 0)
            {
                await context.PostAsync($"There are the options for meeting:<br>");
                await context.PostAsync(stringBuilder.ToString());

                //prompt for schedule suggestion
                context.UserData.SetValue<MeetingTimeSuggestionsResult>("MeetingTimeSuggestionsResult", meetingTimeSuggestion);
                PromptDialog.Text(context, this.ScheduleMessageReceivedAsync, Properties.Resources.Text_PleaseEnterSchedule);
            }
            else
            {
                PromptDialog.Text(context, this.DateMessageReceivedAsync, "Could not find appropriate schedule. Please enter another date.");
            }
        }

        public async Task ScheduleMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            var message = await argument;
            if (message.IsNumber())
            {
                MeetingTimeSuggestionsResult meetingtimeSuggestions;
                context.UserData.TryGetValue<MeetingTimeSuggestionsResult>($"MeetingTimeSuggestionsResult", out meetingtimeSuggestions);
                int inputNumber = Int32.Parse(message);
                if (inputNumber < meetingtimeSuggestions.MeetingTimeSuggestions.Count())
                {
                    context.UserData.SetValue<MeetingTimeSuggestion>(Settings.meetingSelectedSchedule_meetingTimeSuggestion, meetingtimeSuggestions.MeetingTimeSuggestions.ElementAt(inputNumber));
                    await context.PostAsync(this.GetScheduleTicket(context));
                    PromptDialog.Confirm(context, ScheduleMeeitng, "Are you sure to book with the above setting?", null, 3, PromptStyle.AutoText);
                }
                else
                {
                    await context.PostAsync($"Please type option number from 0 to { meetingtimeSuggestions.MeetingTimeSuggestions.Count()-1}");
                    PromptDialog.Text(context, this.ScheduleMessageReceivedAsync, Properties.Resources.Text_PleaseEnterSchedule);
                }
            }
            else
            {
                await context.PostAsync($"Please type only number.");
                PromptDialog.Text(context, this.ScheduleMessageReceivedAsync, Properties.Resources.Text_PleaseEnterSchedule);
            }
        }

        public async Task ScheduleMeeitng(IDialogContext context, IAwaitable<bool> argument)
        {
            bool answer = await argument;
            if (answer == true)
            {
                MeetingTimeSuggestion suggestion;
                context.UserData.TryGetValue<MeetingTimeSuggestion>(Settings.meetingSelectedSchedule_meetingTimeSuggestion, out suggestion);

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
                //adding user himself
                inputAttendee.Add(
                     new Attendee()
                     {
                         EmailAddress = new EmailAddress()
                         {
                             Address = userEmail
                         }
                     }
                    );

                string subject;
                context.UserData.TryGetValue<string>(Settings.meeintingSubject_string, out subject);



                var meeting = new Event()
                {
                    Subject = subject,
                    Body = new ItemBody()
                    {
                        ContentType = BodyType.Html,
                        Content = ""
                    },
                    Start = new DateTimeTimeZone()
                    {
                        DateTime = suggestion.MeetingTimeSlot.Start.DateTime,
                        TimeZone = "UTC"
                    },
                    End = new DateTimeTimeZone()
                    {
                        DateTime = suggestion.MeetingTimeSlot.End.DateTime,
                        TimeZone = "UTC"
                    },
                    Location = new Location()
                    {
                        DisplayName = "RoomA"
                    },
                    Attendees = inputAttendee
                };
                var scheduledMeeting = await meetingService.ScheduleMeeting(result.AccessToken, meeting);
                await context.PostAsync($"Meeting is scheduled! Thank you!");
                context.Done<object>(null);
            }
            else
            {
                await context.PostAsync($"Booling is cancelled.");
                context.Done<object>(null);
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
                    if (context.UserData.TryGetValue<string>(Settings.userEmail_string, out userEmail))
                    {
                        displayEmail = "You:<br>"+displayEmail + userEmail+"<br>Invitation:<br>";
                        foreach (var i in emails)
                            displayEmail = displayEmail + i + "<br>";
                    }
                }
                else
                {
                    displayEmail = "";
                }
            }
                


                if (displaySchedule.Equals(""))
                {
                    MeetingTimeSuggestion suggestion = null;
                    if (context.UserData.TryGetValue<MeetingTimeSuggestion>(Settings.meetingSelectedSchedule_meetingTimeSuggestion, out suggestion))
                    {
                        var stringBuilder = new StringBuilder();
                        DateTime startTime, endTime;
                        DateTime.TryParse(suggestion.MeetingTimeSlot.Start.DateTime, out startTime);
                        DateTime.TryParse(suggestion.MeetingTimeSlot.End.DateTime, out endTime);

                        stringBuilder.AppendLine($"{startTime.AddHours(9).Year.ToString("D4")}/{startTime.AddHours(9).Month.ToString("D2")}/{startTime.AddHours(9).Day.ToString("D2")} " +
                                $"{startTime.AddHours(9).Hour.ToString("D2")}:{startTime.AddHours(9).Minute.ToString("D2")}" +
                                $"  - {endTime.AddHours(9).Hour.ToString("D2")}:{endTime.AddHours(9).Minute.ToString("D2")}\n");
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

            htmlTicket += "</td></tr><tr><th>Attendances</th><td>";
            htmlTicket += displayEmail ?? "";            

            htmlTicket += "</td></tr><tr><th>Scheduled</th><td>";
            htmlTicket += displaySchedule ?? "";

            // htmlTicket += "</td></tr><tr><th>Candidate</th><td>";
            // htmlTicket += this.candidate ?? "";

            htmlTicket += "</td></tr></tbody></table>";

            return htmlTicket;
        }
    }
}