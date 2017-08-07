//Currently this is not used

using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Net.Http;
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
        private string subject = null;
        private string number = null;
        private string duration = null;
        private string emails = null;
        private string date = null;
        private string schedule = null;

        //normalized inputs
        private int normalizedNumber = 0;
        private int normalizedDuration = 0;
        private string[] normalizedEmails;
        private DateTime normalizedDate;
        private string normalizedSchedule = null;
        private string detectedLanguage = null;
        JObject json = null;

        public async Task StartAsync(IDialogContext context)
        {
            if (context.PrivateConversationData.TryGetValue<string>("detectedLanguage", out detectedLanguage))
            {
                detectedLanguage = context.PrivateConversationData.GetValue<string>("detectedLanguage");
            }
            if (context.PrivateConversationData.TryGetValue<JObject>("jsonData", out json))
            {
                json = context.PrivateConversationData.GetValue<JObject>("jsonData");
            }
            this.detectedLanguage = detectedLanguage;
            PromptDialog.Text(context, this.SubjectMessageReceivedAsync, Properties.Resources.Text_Hello1 + json.Value<string>("displayName") + Properties.Resources.Text_Hello2 + Properties.Resources.Text_PleaseEnterSubject);
        }

        public async Task SubjectMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            await context.PostAsync(detectedLanguage);
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(detectedLanguage);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            this.subject = await argument;
            await context.PostAsync(Properties.Resources.Text_CheckSubject1 + this.subject + Properties.Resources.Text_CheckSubject2);
            PromptDialog.Text(context, this.DurationReceivedAsync, Properties.Resources.Text_PleaseEnterDuration);
        }

        public async Task DurationReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            this.duration = await argument;
            if (this.duration.IsNaturalNumber())
            {
                normalizedDuration = Int32.Parse(this.duration);
                await context.PostAsync(Properties.Resources.Text_CheckDuration1 + this.normalizedDuration + Properties.Resources.Text_CheckDuration2);
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
                await context.PostAsync(Properties.Resources.Text_CheckNumberOfParticipants1 + this.normalizedNumber + Properties.Resources.Text_CheckNumberOfParticipants2);
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

            if (this.emails.IsEmailAddressList())
            {
                normalizedEmails = this.emails.Split(',');
                await context.PostAsync(Properties.Resources.Text_CheckEmailAddresses);
                foreach (var i in normalizedEmails)
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
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            this.date = await argument;

            if (this.date.IsDatatime())
            {
                await context.PostAsync(Properties.Resources.Text_CheckWhen1 + this.date + Properties.Resources.Text_CheckWhen2);
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
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
            this.schedule = await argument;
            await context.PostAsync(Properties.Resources.Text_Confirmation1);
            foreach (var i in normalizedEmails)
                await context.PostAsync(i);
            await context.PostAsync(Properties.Resources.Text_Confirmation2 + this.schedule + Properties.Resources.Text_Confirmation3);
            PromptDialog.Confirm(context, this.ConfirmedMessageReceivedAsync, Properties.Resources.Text_Confirmation4, null, 3, PromptStyle.AutoText);
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
    }
}