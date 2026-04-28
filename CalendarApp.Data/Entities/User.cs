using System;
using System.Collections.Generic;

namespace CalendarApp.Data.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<Participant> Participations { get; set; } = new List<Participant>();
    }
}
