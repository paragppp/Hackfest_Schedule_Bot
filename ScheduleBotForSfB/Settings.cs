using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SampleAADv2Bot
{
    public class Settings
    {
        //this is for UserData
        public const string meeintingSubject_string = "meeintingSubject"; //string
        public const string meetingInvitationsNum_int = "meetingInvitationsNum"; //int
        public const string meetingDuration_int = "meetingDuration"; //int
        public const string userEmail_string = "userEmail"; //string
        public const string userName_string = "userName"; //string
        public const string InvitationsEmails_stringArray = "InvitationsEmails"; //string[]
        public const string meetingDate_string = "meetingDate"; //string
        public const string meetingSelectedDate_datatime = "meetingSelectedDate"; //Datetime
        public const string meetingSelectedSchedule_meetingTimeSuggestion = "eetingSelectedSchedule"; //MeetingTimeSuggestion
    }
}