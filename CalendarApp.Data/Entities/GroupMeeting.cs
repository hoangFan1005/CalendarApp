using System;
using System.Collections.Generic;

namespace CalendarApp.Data.Entities
{
    public class GroupMeeting
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string MeetingName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public ICollection<Participant> Participants { get; set; } = new List<Participant>();

        public TimeSpan GetDuration()
        {
            return EndTime - StartTime;
        }
    }
}
