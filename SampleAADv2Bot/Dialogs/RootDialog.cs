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

        //normalized inputs
        private int normalizedNumber = 0;
        private int normalizedDuration = 0;
        private string[] normalizedEmails;
        private DateTime normalizedDate;
        private string normalizedSchedule = null;

        //Localization
        private string detectedLanguage = "en-US";


        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
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
                Scopes = new string[] { "User.Read" },
                RedirectUrl = ConfigurationManager.AppSettings["aad:Callback"]
            };
            await context.Forward(new AuthDialog(new MSALAuthProvider(), options), async (IDialogContext authContext, IAwaitable<AuthResult> authResult) =>
            {
                var result = await authResult;
                // Use token to call into service                
                var json = await new HttpClient().GetWithAuthAsync(result.AccessToken, "https://graph.microsoft.com/v1.0/me");
                //await authContext.PostAsync($"I'm a simple bot that doesn't do much, but I know your name is {json.Value<string>("displayName")} and your UPN is {json.Value<string>("userPrincipalName")}.But expect a lot more from me shortly!");
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(detectedLanguage);
                PromptDialog.Text(authContext, this.SubjectMessageReceivedAsync, Properties.Resources.Text_Hello1 + json.Value<string>("displayName") + Properties.Resources.Text_Hello2 + Properties.Resources.Text_PleaseEnterSubject);
            }, message, CancellationToken.None);
        }

        public async Task SubjectMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
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
            this.emails = this.emails.Replace("&#160;", "").Replace("&#160:^", "");
            this.emails = System.Text.RegularExpressions.Regex.Replace(this.emails, "\\(.+?\\)", "");
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
    }
}