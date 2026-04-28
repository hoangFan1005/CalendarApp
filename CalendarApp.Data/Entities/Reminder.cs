using System;

namespace CalendarApp.Data.Entities
{
    public class Reminder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AppointmentId { get; set; }
        public int MinutesBefore { get; set; }
        public string Type { get; set; } = "Notification";

        public Appointment? Appointment { get; set; }
    }
}
