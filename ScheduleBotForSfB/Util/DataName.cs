namespace SampleAADv2Bot.Util
{
    /// <summary>
    /// Class abstracting variables we are using for storing user private data
    /// </summary>
    public class DataName
    {
        // Let's follow VS 2017 and ReShapered recommendations for naming 
        public const string UserEmailString = "userEmail";
        public const string UserNameString = "userName";
        public const string MeeintingSubjectString = "meeintingSubject";
        public const string MeetingInvitationsNumInt = "meetingInvitationsNum";
        public const string MeetingDurationInt = "meetingDuration";
        public const string InvitationsEmailsStringArray = "InvitationsEmails";
        public const string MeetingSelectedDateDatetime = "meetingSelectedDate";
        public const string MeetingSelectedStartTimeDatetime = "meetingSelectedStartTime";
        public const string MeetingSelectedEndTimeDatetime = "meetingSelectedEndTime";
        public const string meetingSelectedRoomRoom = "meetingSelectedRoom";
    }
}