using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalendarApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CalendarApp.Data.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly CalendarDbContext _context;

        public AppointmentRepository(CalendarDbContext context)
        {
            _context = context;
        }

        public async Task<Appointment?> GetByIdAsync(Guid id)
        {
            return await _context.Appointments
                .Include(a => a.Reminders)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<Appointment>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Appointments
                .Include(a => a.Reminders)
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }

        public async Task<Appointment> AddAsync(Appointment appointment)
        {
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task UpdateAsync(Appointment appointment)
        {
            // Remove old reminders first
            var oldReminders = _context.Reminders.Where(r => r.AppointmentId == appointment.Id);
            _context.Reminders.RemoveRange(oldReminders);
            await _context.SaveChangesAsync();

            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Appointment>> GetOverlappingAppointmentsAsync(Guid userId, DateTime start, DateTime end, Guid? excludeId = null)
        {
            var query = _context.Appointments
                .Where(a => a.UserId == userId && a.StartTime < end && a.EndTime > start);

            if (excludeId.HasValue)
            {
                query = query.Where(a => a.Id != excludeId.Value);
            }

            return await query.ToListAsync();
        }
    }
}
