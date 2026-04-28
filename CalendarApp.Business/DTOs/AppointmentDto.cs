using System;
using System.Collections.Generic;

namespace CalendarApp.Business.DTOs
{
    public class AppointmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsCompleted { get; set; }
        public List<int> ReminderMinutes { get; set; } = new List<int>();

        public TimeSpan Duration => EndTime - StartTime;
    }
}
