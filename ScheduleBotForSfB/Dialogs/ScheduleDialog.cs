//Currently this is not used

using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Threading;
using System.Threading.Tasks;
using SampleAADv2Bot.Extensions;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace SampleAADv2Bot.Dialogs
{
    [Serializable]

    public class ScheduleDialog: IDialog<string>
    {
        //raw inputs
        private string _subject;
        private string _number;
        private string _duration;
        private string _emails;
        private string _date;
        private string _schedule;

        //normalized inputs
        private int _normalizedNumber;
        private int _normalizedDuration;
        private string[] _normalizedEmails;
        private DateTime normalizedDate;
        private string _detectedLanguage;
        JObject _json;

        public async Task StartAsync(IDialogContext context)
        {
            if (context.PrivateConversationData.TryGetValue<string>("detectedLanguage", out _detectedLanguage))
            {
                _detectedLanguage = context.PrivateConversationData.GetValue<string>("detectedLanguage");
            }
            if (context.PrivateConversationData.TryGetValue<JObject>("jsonData", out _json))
            {
                _json = context.PrivateConversationData.GetValue<JObject>("jsonData");
            }
            PromptDialog.Text(context, this.SubjectMessageReceivedAsync, Properties.Resources.Text_Hello1 + _json.Value<string>("displayName") + Properties.Resources.Text_Hello2 + Properties.Resources.Text_PleaseEnterSubject);
        }

        public async Task SubjectMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            await context.PostAsync(_detectedLanguage);
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(_detectedLanguage);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(_detectedLanguage);
            this._subject = await argument;
            await context.PostAsync(Properties.Resources.Text_CheckSubject1 + this._subject + Properties.Resources.Text_CheckSubject2);
            PromptDialog.Text(context, this.DurationReceivedAsync, Properties.Resources.Text_PleaseEnterDuration);
        }

        public async Task DurationReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(_detectedLanguage);
            this._duration = await argument;
            if (this._duration.IsNaturalNumber())
            {
                _normalizedDuration = Int32.Parse(this._duration);
                await context.PostAsync(Properties.Resources.Text_CheckDuration1 + this._normalizedDuration + Properties.Resources.Text_CheckDuration2);
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
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(_detectedLanguage);
            this._number = await argument;
            if (this._number.IsNaturalNumber())
            {
                _normalizedNumber = Int32.Parse(this._number);
                await context.PostAsync(Properties.Resources.Text_CheckNumberOfParticipants1 + this._normalizedNumber + Properties.Resources.Text_CheckNumberOfParticipants2);
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
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(_detectedLanguage);
            this._emails = await argument;
            //remove space
            this._emails = this._emails.Replace(" ", "").Replace("　", "");

            if (this._emails.IsEmailAddressList())
            {
                _normalizedEmails = this._emails.Split(',');
                await context.PostAsync(Properties.Resources.Text_CheckEmailAddresses);
                foreach (var i in _normalizedEmails)
                    await context.PostAsync(i);
                PromptDialog.Text(context, this.DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);
            }
            else
            {
                await context.PostAsync(Properties.Resources.Text_AlertEmailAddresses);
                PromptDialog.Text(context, this.EmailsMessageReceivedAsync, Properties.Resources.Text_PleaseEnterEmailAddresses);
            }
        }

        public async Task DateMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(_detectedLanguage);
            this._date = await argument;

            if (this._date.IsDatatime())
            {
                await context.PostAsync(Properties.Resources.Text_CheckWhen1 + this._date + Properties.Resources.Text_CheckWhen2);
                var dateCandidates = new string[] { "7/10 12:00-13:00 RoomA", "7/10 16:00-17:00 RoomB", "7/11 12:00-13:00 RoomC" };
                PromptDialog.Choice(context, this.ScheduleMessageReceivedAsync, dateCandidates, Properties.Resources.Text_PleaseSelectSchedule, null, 3);
            }
            else
            {
                await context.PostAsync(Properties.Resources.Text_AlertWhen);
                PromptDialog.Text(context, this.DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);
            }

        }

        public async Task ScheduleMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(_detectedLanguage);
            this._schedule = await argument;
            await context.PostAsync(Properties.Resources.Text_Confirmation1);
            foreach (var i in _normalizedEmails)
                await context.PostAsync(i);
            await context.PostAsync(Properties.Resources.Text_Confirmation2 + this._schedule + Properties.Resources.Text_Confirmation3);
            PromptDialog.Confirm(context, this.ConfirmedMessageReceivedAsync, Properties.Resources.Text_Confirmation4, null, 3, PromptStyle.AutoText);
        }

        public async Task ConfirmedMessageReceivedAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(_detectedLanguage);
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
    }
}