using System;

namespace CalendarApp.Data.Entities
{
    public class Participant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GroupMeetingId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedDate { get; set; } = DateTime.Now;

        public GroupMeeting? GroupMeeting { get; set; }
        public User? User { get; set; }
    }
}
