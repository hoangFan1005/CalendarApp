using System;
using System.Collections.Generic;

namespace CalendarApp.Data.Entities
{
    public class Appointment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsCompleted { get; set; } = false;

        public User? User { get; set; }
        public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

        public TimeSpan GetDuration()
        {
            return EndTime - StartTime;
        }
    }
}
