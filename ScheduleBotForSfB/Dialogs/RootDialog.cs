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


namespace SampleAADv2Bot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<string>
    {
        //normalized inputs
        private Dictionary<string, string> roomsDictionary = null;
        //Localization
        private string detectedLanguage = "en-US";

        //Scheduling
        AuthResult result = null;

        //For displaying current input table
        private string displaySubject = "";
        private string displayDuration = "";
        private string displayNumber = "";
        private string displayEmail = "";
        private string displaySchedule = "";

        private readonly IMeetingService meetingService;
        private readonly ILoggingService loggingService;
        private readonly IRoomService roomService;

        // TBD - Replace with dependency injection 
        //static IRoomService roomService = new RoomService();
        //static IHttpService httpService = new HttpService();
        //static ILoggingService loggingService = new LoggingService();
        //static MeetingService meetingService = new MeetingService(httpService, roomService, loggingService);

        public RootDialog(IMeetingService meetingService, IRoomService roomService, ILoggingService loggingService)
        {
            this.meetingService = meetingService;
            this.roomService = roomService;
            this.loggingService = loggingService;
        }

        public async Task Init(IDialogContext context)
        {
            context.PrivateConversationData.RemoveValue(Util.DataName.InvitationsEmails_stringArray);
            context.PrivateConversationData.RemoveValue(Util.DataName.meeintingSubject_string);
            context.PrivateConversationData.RemoveValue(Util.DataName.meetingDuration_int);
            context.PrivateConversationData.RemoveValue(Util.DataName.meetingInvitationsNum_int);
            context.PrivateConversationData.RemoveValue(Util.DataName.meetingSelectedDate_datetime);
            context.PrivateConversationData.RemoveValue(Util.DataName.meetingSelectedEndTime_datetime);
            context.PrivateConversationData.RemoveValue(Util.DataName.meetingSelectedStartTime_datetime);
            context.PrivateConversationData.RemoveValue(Util.DataName.userEmail_string);
            context.PrivateConversationData.RemoveValue(Util.DataName.userName_string);
            displaySubject = "";
            displayDuration = "";
            displayNumber = "";
            displayEmail = "";
            displaySchedule = "";
        }
    

        public async Task StartAsync(IDialogContext context)
        {
            await Init(context);
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

            await context.Forward(new AuthDialog(new MSALAuthProvider(), Util.DataConverter.GetAuthenticationOptions()), ResumeAfterAuth, message, CancellationToken.None);
        }

        public async Task ResumeAfterAuth(IDialogContext authContext, IAwaitable<AuthResult> authResult)
        {
            result = await authResult;
            // Use token to call into service                
            var json = await new HttpClient().GetWithAuthAsync(result.AccessToken, "https://graph.microsoft.com/v1.0/me");
            authContext.PrivateConversationData.SetValue<string>(Util.DataName.userName_string, json.Value<string>("displayName"));
            authContext.PrivateConversationData.SetValue<string>(Util.DataName.userEmail_string, json.Value<string>("mail"));
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            PromptDialog.Text(authContext, SubjectMessageReceivedAsync, Properties.Resources.Text_Hello1 + json.Value<string>("displayName") + Properties.Resources.Text_Hello2 + Properties.Resources.Text_PleaseEnterSubject);
        }


        public async Task SubjectMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            var message = await argument;
            context.PrivateConversationData.SetValue<string>(Util.DataName.meeintingSubject_string, message);
            displaySubject = message;
            await context.PostAsync(Util.DataConverter.GetScheduleTicket(displaySubject, displayDuration, displayNumber, displayEmail, displaySchedule));
            PromptDialog.Text(context, DurationReceivedAsync, Properties.Resources.Text_PleaseEnterDuration);
        }

        public async Task DurationReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            var message = await argument;
            if (message.IsNaturalNumber())
            {
                var normalizedDuration = Int32.Parse(message);
                context.PrivateConversationData.SetValue<int>(Util.DataName.meetingDuration_int, normalizedDuration);
                displayDuration = normalizedDuration.ToString();
                await context.PostAsync(Util.DataConverter.GetScheduleTicket(displaySubject, displayDuration, displayNumber, displayEmail, displaySchedule));
                PromptDialog.Text(context, NumbersMessageReceivedAsync, Properties.Resources.Text_PleaseEnterNumberOfParticipants);
            }
            else
            {
                await context.PostAsync(Properties.Resources.Text_AlertDuration);
                PromptDialog.Text(context, DurationReceivedAsync, Properties.Resources.Text_PleaseEnterDuration);
            }
        }

        public async Task NumbersMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            var message = await argument;
            if (message.IsNaturalNumber())
            {
                var normalizedNumber = Int32.Parse(message);
                context.PrivateConversationData.SetValue<int>(Util.DataName.meetingInvitationsNum_int, normalizedNumber);
                displayNumber = normalizedNumber.ToString();
                await context.PostAsync(Util.DataConverter.GetScheduleTicket(displaySubject, displayDuration, displayNumber, displayEmail, displaySchedule));
                PromptDialog.Text(context, EmailsMessageReceivedAsync, Properties.Resources.Text_PleaseEnterEmailAddresses);
            }
            else
            {
                await context.PostAsync(Properties.Resources.Text_AlertNumberOfParticipants);
                PromptDialog.Text(context, NumbersMessageReceivedAsync, Properties.Resources.Text_PleaseEnterNumberOfParticipants);
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
            if (message.IsEmailAddressList())
            {
                var normalizedEmails = message.Split(',');
                int normalizedNumber = context.PrivateConversationData.GetValue<int>(Util.DataName.meetingInvitationsNum_int);

                if (normalizedEmails.Length == normalizedNumber)
                {
                    context.PrivateConversationData.SetValue<string[]>(Util.DataName.InvitationsEmails_stringArray, normalizedEmails);
                    foreach(var i in normalizedEmails)
                        displayEmail += i+"<br>";
                    await context.PostAsync(Util.DataConverter.GetScheduleTicket(displaySubject, displayDuration, displayNumber, displayEmail, displaySchedule));
                    PromptDialog.Text(context, DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);
                }
                else
                {
                    await context.PostAsync("Please enter " + displayNumber + " E-mail addresses.");                    
                    PromptDialog.Text(context, EmailsMessageReceivedAsync, Properties.Resources.Text_PleaseEnterEmailAddresses);
                }

            }
            else
            {
                await context.PostAsync(Properties.Resources.Text_AlertEmailAddresses);
                PromptDialog.Text(context, EmailsMessageReceivedAsync, Properties.Resources.Text_PleaseEnterEmailAddresses);
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
                context.PrivateConversationData.SetValue<DateTime>(Util.DataName.meetingSelectedDate_datetime, dateTime);                
                await context.PostAsync(Properties.Resources.Text_CheckWhen1 + message + Properties.Resources.Text_CheckWhen2);
                await GetMeetingSuggestions(context, argument);
            }
            else
            {
                PromptDialog.Text(context, DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);
            }
        }

        public async Task ScheduleMessageReceivedAsync(IDialogContext context, IAwaitable<MeetingSchedule> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            var date = await argument;
            context.PrivateConversationData.SetValue<DateTime>(Util.DataName.meetingSelectedStartTime_datetime, date.StartTime);
            context.PrivateConversationData.SetValue<DateTime>(Util.DataName.meetingSelectedEndTime_datetime, date.EndTime);           
            PromptDialog.Choice(context, ScheduleMeetingAsync, date.Rooms, Properties.Resources.Text_PleaseSelectSchedule, null, 3);
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
            int savedDuration = context.PrivateConversationData.GetValue<int>(Util.DataName.meetingDuration_int);
            string[] savedEmails = context.PrivateConversationData.GetValue<string[]>(Util.DataName.InvitationsEmails_stringArray);
            DateTime savedDate = context.PrivateConversationData.GetValue<DateTime>(Util.DataName.meetingSelectedDate_datetime);

            var userFindMeetingTimesRequestBody = Util.DataConverter.GetUserFindMeetingTimesRequestBody(savedDate, savedEmails, savedDuration);
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
                                        Time = Util.DataConverter.GetFormatedTime(startTime, endTime),
                                        Rooms = Util.DataConverter.GetMeetingSuggestionRooms(suggestion, roomsDictionary)
                                    });
            }
            if (meetingScheduleSuggestions.Count != 0)
            {
                PromptDialog.Choice(context, ScheduleMessageReceivedAsync, meetingScheduleSuggestions, Properties.Resources.Text_PleaseSelectSchedule, null, 3);
            }
            else
            {
                await context.PostAsync("There is no available time. Please enter another date.");
                PromptDialog.Text(context, DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);
            }
        }

        public async Task ScheduleMeetingAsync(IDialogContext context, IAwaitable<Services.Room> message)
        {
            try
            {    
                var selectedRoom = await message;
                string savedSubject = context.PrivateConversationData.GetValue<string>(Util.DataName.meeintingSubject_string);
                string[] savedEmails = context.PrivateConversationData.GetValue<string[]>(Util.DataName.InvitationsEmails_stringArray);
                DateTime savedStartTime = context.PrivateConversationData.GetValue<DateTime>(Util.DataName.meetingSelectedStartTime_datetime);
                DateTime savedEndTime = context.PrivateConversationData.GetValue<DateTime>(Util.DataName.meetingSelectedEndTime_datetime);

                var meeting = Util.DataConverter.GetEvent(selectedRoom, savedEmails, savedSubject, savedStartTime, savedEndTime);
                var scheduledMeeting = await meetingService.ScheduleMeeting(result.AccessToken, meeting);
                await context.PostAsync($"Meeting '{savedSubject}' at {Util.DataConverter.GetFormatedTime(savedStartTime, savedEndTime)} with attendees {String.Join(",", savedEmails)} in room {selectedRoom.Name} was scheduled.");
            }
            catch (Exception ex)
            {
                loggingService.Error(ex);
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
                loggingService.Error(ex);
                await context.PostAsync($"Failed with message: {ex.Message}");
            }
            finally
            {
                context.Wait(MessageReceivedAsync);
            }
        }
    }
}