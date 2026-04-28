using System;
using System.Threading.Tasks;
using CalendarApp.Business.DTOs;

namespace CalendarApp.Business.Services
{
    public interface IAppointmentService
    {
        Task<ConflictCheckResult> CheckConflictAsync(Guid userId, DateTime start, DateTime end, Guid? excludeId = null);
        Task<GroupMeetingDto?> FindMatchingGroupMeetingAsync(string name, TimeSpan duration);
        Task<AppointmentDto> CreateAppointmentAsync(Guid userId, AppointmentDto dto);
        Task UpdateAppointmentAsync(Guid userId, AppointmentDto dto);
        Task ReplaceAppointmentAsync(Guid userId, Guid oldAppointmentId, AppointmentDto newDto);
        Task DeleteAppointmentAsync(Guid appointmentId);
        Task CompleteAppointmentAsync(Guid appointmentId);
        Task JoinGroupMeetingAsync(Guid userId, Guid meetingId);
    }
}
