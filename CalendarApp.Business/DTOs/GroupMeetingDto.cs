using System;

namespace CalendarApp.Business.DTOs
{
    public class GroupMeetingDto
    {
        public Guid Id { get; set; }
        public string MeetingName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
