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
using static System.Int32;


namespace SampleAADv2Bot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<string>
    {
        //normalized inputs
        private Dictionary<string, string> _roomsDictionary;
        //Localization
        private const string DetectedLanguage = "en-US";

        //Scheduling
        private AuthResult _result;

        //For displaying current input table
        private string _displaySubject = "";
        private string _displayDuration = "";
        private string _displayNumber = "";
        private string _displayEmail = "";
        private string _displaySchedule = "";

        private readonly IMeetingService _meetingService;
        private readonly ILoggingService _loggingService;
        private readonly IRoomService _roomService;

        // TBD - Replace with dependency injection 
        //static IRoomService roomService = new RoomService();
        //static IHttpService httpService = new HttpService();
        //static ILoggingService loggingService = new LoggingService();
        //static MeetingService meetingService = new MeetingService(httpService, roomService, loggingService);

        public RootDialog(IMeetingService meetingService, IRoomService roomService, ILoggingService loggingService)
        {
            _meetingService = meetingService;
            _roomService = roomService;
            _loggingService = loggingService;
        }

        public void Reset(IDialogContext context)
        {
            context.PrivateConversationData.RemoveValue(Util.DataName.InvitationsEmailsStringArray);
            context.PrivateConversationData.RemoveValue(Util.DataName.MeeintingSubjectString);
            context.PrivateConversationData.RemoveValue(Util.DataName.MeetingDurationInt);
            context.PrivateConversationData.RemoveValue(Util.DataName.MeetingInvitationsNumInt);
            context.PrivateConversationData.RemoveValue(Util.DataName.MeetingSelectedDateDatetime);
            context.PrivateConversationData.RemoveValue(Util.DataName.MeetingSelectedEndTimeDatetime);
            context.PrivateConversationData.RemoveValue(Util.DataName.MeetingSelectedStartTimeDatetime);
            context.PrivateConversationData.RemoveValue(Util.DataName.UserEmailString);
            context.PrivateConversationData.RemoveValue(Util.DataName.UserNameString);
            _displaySubject = "";
            _displayDuration = "";
            _displayNumber = "";
            _displayEmail = "";
            _displaySchedule = "";
        }


        public async Task StartAsync(IDialogContext context)
        {
            Reset(context);
            context.Wait(MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            _roomsDictionary = new Dictionary<string, string>();
            var rooms = _roomService.GetRooms();
            foreach(var room in rooms)
            {
                _roomsDictionary.Add(room.Address, room.Name);
            }
            var message = await item;
            //Initialize AuthenticationOptions and forward to AuthDialog for token

            await context.Forward(new AuthDialog(new MSALAuthProvider(), Util.DataConverter.GetAuthenticationOptions()), ResumeAfterAuth, message, CancellationToken.None);
        }

        public async Task ResumeAfterAuth(IDialogContext authContext, IAwaitable<AuthResult> authResult)
        {
            _result = await authResult;
            // Use token to call into service                
            var json = await new HttpClient().GetWithAuthAsync(_result.AccessToken, "https://graph.microsoft.com/v1.0/me");
            authContext.PrivateConversationData.SetValue(Util.DataName.UserNameString, json.Value<string>("displayName"));
            authContext.PrivateConversationData.SetValue(Util.DataName.UserEmailString, json.Value<string>("mail"));
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(DetectedLanguage);
            PromptDialog.Text(authContext, SubjectMessageReceivedAsync, Properties.Resources.Text_Hello1 + json.Value<string>("displayName") + Properties.Resources.Text_Hello2 + Properties.Resources.Text_PleaseEnterSubject);
        }


        public async Task SubjectMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(DetectedLanguage);
            var message = await argument;
            context.PrivateConversationData.SetValue(Util.DataName.MeeintingSubjectString, message);
            _displaySubject = message;
            await context.PostAsync(Util.DataConverter.GetScheduleTicket(_displaySubject, _displayDuration, _displayNumber, _displayEmail, _displaySchedule));
            PromptDialog.Text(context, DurationReceivedAsync, Properties.Resources.Text_PleaseEnterDuration);
        }

        public async Task DurationReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(DetectedLanguage);
            var message = await argument;
            if (message.IsNaturalNumber())
            {
                var normalizedDuration = Parse(message);
                context.PrivateConversationData.SetValue(Util.DataName.MeetingDurationInt, normalizedDuration);
                _displayDuration = normalizedDuration.ToString();
                await context.PostAsync(Util.DataConverter.GetScheduleTicket(_displaySubject, _displayDuration, _displayNumber, _displayEmail, _displaySchedule));
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
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(DetectedLanguage);
            var message = await argument;
            if (message.IsNaturalNumber())
            {
                var normalizedNumber = Parse(message);
                context.PrivateConversationData.SetValue<int>(Util.DataName.MeetingInvitationsNumInt, normalizedNumber);
                _displayNumber = normalizedNumber.ToString();
                await context.PostAsync(Util.DataConverter.GetScheduleTicket(_displaySubject, _displayDuration, _displayNumber, _displayEmail, _displaySchedule));
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
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(DetectedLanguage);
            var message = await argument;
            //remove space
            message = message.Replace(" ", "").Replace("　", "");
            //This is because in Skype for business, " "(space) is automatically converted to "&#160;", which is blocking to get emails
            message = message.Replace("&#160;", "").Replace("&#160:^", "");
            //This is removing Hyperlink which Skype for business automatically adds
            message = System.Text.RegularExpressions.Regex.Replace(message, "\\(.+?\\)", "");
            if (message.IsEmailAddressList())
            {
                var normalizedEmails = message.Split(',');
                var normalizedNumber = context.PrivateConversationData.GetValue<int>(Util.DataName.MeetingInvitationsNumInt);

                if (normalizedEmails.Length == normalizedNumber)
                {
                    context.PrivateConversationData.SetValue(Util.DataName.InvitationsEmailsStringArray, normalizedEmails);
                    var stringBuilder = new System.Text.StringBuilder();
                    foreach (var i in normalizedEmails)
                        stringBuilder.Append($"{i}<br>");
                    _displayEmail = stringBuilder.ToString();
                    await context.PostAsync(Util.DataConverter.GetScheduleTicket(_displaySubject, _displayDuration, _displayNumber, _displayEmail, _displaySchedule));
                    PromptDialog.Text(context, DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);
                }
                else
                {
                    await context.PostAsync($"Please enter {_displayNumber} E-mail addresses.");                    
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
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(DetectedLanguage);
            var message = await argument;
            DateTime.TryParse(message, out DateTime dateTime);
            if (dateTime != DateTime.MinValue && dateTime != DateTime.MaxValue)
            {
                context.PrivateConversationData.SetValue<DateTime>(Util.DataName.MeetingSelectedDateDatetime, dateTime);                
                await context.PostAsync($"{Properties.Resources.Text_CheckWhen1} {message} {Properties.Resources.Text_CheckWhen2}");
                await GetMeetingSuggestions(context);
            }
            else
            {
                PromptDialog.Text(context, DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);
            }
        }

        public async Task ScheduleMessageReceivedAsync(IDialogContext context, IAwaitable<MeetingSchedule> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(DetectedLanguage);
            var date = await argument;
            context.PrivateConversationData.SetValue<DateTime>(Util.DataName.MeetingSelectedStartTimeDatetime, date.StartTime);
            context.PrivateConversationData.SetValue<DateTime>(Util.DataName.MeetingSelectedEndTimeDatetime, date.EndTime);           
            PromptDialog.Choice(context, ConfirmationAsync, date.Rooms, Properties.Resources.Text_PleaseSelectRoom, null, 3);
        }

        public async Task ConfirmedMessageReceivedAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(DetectedLanguage);
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
        private async Task GetMeetingSuggestions(IDialogContext context)
        {
            var savedDuration = context.PrivateConversationData.GetValue<int>(Util.DataName.MeetingDurationInt);
            var savedEmails = context.PrivateConversationData.GetValue<string[]>(Util.DataName.InvitationsEmailsStringArray);
            var savedDate = context.PrivateConversationData.GetValue<DateTime>(Util.DataName.MeetingSelectedDateDatetime);

            var userFindMeetingTimesRequestBody = Util.DataConverter.GetUserFindMeetingTimesRequestBody(savedDate, savedEmails, savedDuration);
            var meetingTimeSuggestion = await _meetingService.GetMeetingsTimeSuggestions(_result.AccessToken, userFindMeetingTimesRequestBody);
            var meetingScheduleSuggestions = new List<MeetingSchedule>();
            var counter = 1;
            foreach (var suggestion in meetingTimeSuggestion.MeetingTimeSuggestions)
            {
                DateTime.TryParse(suggestion.MeetingTimeSlot.Start.DateTime, out DateTime startTime);
                DateTime.TryParse(suggestion.MeetingTimeSlot.End.DateTime, out DateTime endTime);

                meetingScheduleSuggestions.Add(new MeetingSchedule()
                                    {
                                        StartTime = startTime,
                                        EndTime = endTime,
                                        Time = Util.DataConverter.GetFormatedTime(startTime, endTime, counter),
                                        Rooms = Util.DataConverter.GetMeetingSuggestionRooms(suggestion, _roomsDictionary)
                                    });
                counter++;
            }
            if (meetingScheduleSuggestions.Count != 0)
            {
                PromptDialog.Choice(context, ScheduleMessageReceivedAsync, meetingScheduleSuggestions, Properties.Resources.Text_PleaseSelectSchedule);
            }
            else
            {
                await context.PostAsync("There is no available time. Please enter another date.");
                PromptDialog.Text(context, DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);
            }
        }

        public async Task ConfirmationAsync(IDialogContext context, IAwaitable<Services.Room> message)
        {           
            try
            {
                var selectedRoom = await message;
                context.PrivateConversationData.SetValue<Room>(Util.DataName.MeetingSelectedRoomRoom, selectedRoom);
                var savedStartTime = context.PrivateConversationData.GetValue<DateTime>(Util.DataName.MeetingSelectedDateDatetime);
                var savedEndTime = context.PrivateConversationData.GetValue<DateTime>(Util.DataName.MeetingSelectedEndTimeDatetime);
                _displaySchedule = $"{Util.DataConverter.GetFormatedTime(savedStartTime, savedEndTime, 0)}<br>{selectedRoom.Name}";
                await context.PostAsync(Util.DataConverter.GetScheduleTicket(_displaySubject, _displayDuration, _displayNumber, _displayEmail, _displaySchedule));
                PromptDialog.Confirm(context, ScheduleMeetingAsync, "Are you sure to book with the above setting?", null, 3, PromptStyle.AutoText);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
                throw ex;
            }
        }

        public async Task ScheduleMeetingAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            try
            {
                var answer = await argument;
                if (answer == true)
                {
                    try
                    {
                        var selectedRoom = context.PrivateConversationData.GetValue<Room>(Util.DataName.MeetingSelectedRoomRoom);
                        var savedSubject = context.PrivateConversationData.GetValue<string>(Util.DataName.MeeintingSubjectString);
                        var savedEmails = context.PrivateConversationData.GetValue<string[]>(Util.DataName.InvitationsEmailsStringArray);
                        var savedStartTime = context.PrivateConversationData.GetValue<DateTime>(Util.DataName.MeetingSelectedStartTimeDatetime);
                        var savedEndTime = context.PrivateConversationData.GetValue<DateTime>(Util.DataName.MeetingSelectedEndTimeDatetime);

                        var meeting = Util.DataConverter.GetEvent(selectedRoom, savedEmails, savedSubject, savedStartTime, savedEndTime);
                        var scheduledMeeting = await _meetingService.ScheduleMeeting(_result.AccessToken, meeting);
                        await context.PostAsync($"Meeting '{savedSubject}' at {Util.DataConverter.GetFormatedTime(savedStartTime, savedEndTime, 0)} with attendees {String.Join(",", savedEmails)} in {selectedRoom.Name} was scheduled.");
                    }
                    catch (Exception ex)
                    {
                        _loggingService.Error(ex);
                        throw ex;
                    }
                }
                else
                {
                    await context.PostAsync(Properties.Resources.Text_Canceled);
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
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
                _loggingService.Error(ex);
                throw;
            }
        }
    }
}