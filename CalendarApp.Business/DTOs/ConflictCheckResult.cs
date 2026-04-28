using System.Collections.Generic;

namespace CalendarApp.Business.DTOs
{
    public class ConflictCheckResult
    {
        public bool HasConflict { get; set; }
        public List<AppointmentDto> ConflictingAppointments { get; set; } = new List<AppointmentDto>();
    }
}
