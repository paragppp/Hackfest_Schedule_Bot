using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SampleAADv2Bot.Util
{
    public class DataName
    {
        // This is for PrivateConversationData
        // Please write {name}_{type} = {name} 
        public const string userEmail_string = "userEmail";
        public const string userName_string = "userName";
        public const string meeintingSubject_string = "meeintingSubject";
        public const string meetingInvitationsNum_int = "meetingInvitationsNum";
        public const string meetingDuration_int = "meetingDuration";
        public const string InvitationsEmails_stringArray = "InvitationsEmails";
        public const string meetingSelectedDate_datetime = "meetingSelectedDate";
        public const string meetingSelectedStartTime_datetime = "meetingSelectedStartTime";
        public const string meetingSelectedEndTime_datetime = "meetingSelectedEndTime";

    }
}