using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarApp.Data.Entities;

namespace CalendarApp.Data.Repositories
{
    public interface IAppointmentRepository
    {
        Task<Appointment?> GetByIdAsync(Guid id);
        Task<List<Appointment>> GetByUserIdAsync(Guid userId);
        Task<Appointment> AddAsync(Appointment appointment);
        Task UpdateAsync(Appointment appointment);
        Task DeleteAsync(Guid id);
        Task<List<Appointment>> GetOverlappingAppointmentsAsync(Guid userId, DateTime start, DateTime end, Guid? excludeId = null);
    }
}
