using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalendarApp.Business.DTOs;
using CalendarApp.Data.Entities;
using CalendarApp.Data.Repositories;

namespace CalendarApp.Business.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IGroupMeetingRepository _groupMeetingRepository;
        private readonly IMapper _mapper;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IGroupMeetingRepository groupMeetingRepository,
            IMapper mapper)
        {
            _appointmentRepository = appointmentRepository;
            _groupMeetingRepository = groupMeetingRepository;
            _mapper = mapper;
        }

        public async Task<ConflictCheckResult> CheckConflictAsync(Guid userId, DateTime start, DateTime end, Guid? excludeId = null)
        {
            var overlapping = await _appointmentRepository.GetOverlappingAppointmentsAsync(userId, start, end, excludeId);
            return new ConflictCheckResult
            {
                HasConflict = overlapping.Any(),
                ConflictingAppointments = overlapping.Select(a => _mapper.Map<AppointmentDto>(a)).ToList()
            };
        }

        public async Task<GroupMeetingDto?> FindMatchingGroupMeetingAsync(string name, TimeSpan duration)
        {
            var meeting = await _groupMeetingRepository.FindMatchingMeetingAsync(name, duration);
            if (meeting != null)
            {
                return _mapper.Map<GroupMeetingDto>(meeting);
            }
            return null;
        }

        public async Task<AppointmentDto> CreateAppointmentAsync(Guid userId, AppointmentDto dto)
        {
            var appointment = _mapper.Map<Appointment>(dto);
            appointment.UserId = userId;

            foreach (var min in dto.ReminderMinutes)
            {
                appointment.Reminders.Add(new Reminder
                {
                    MinutesBefore = min
                });
            }

            var created = await _appointmentRepository.AddAsync(appointment);
            dto.Id = created.Id;
            return dto;
        }

        public async Task UpdateAppointmentAsync(Guid userId, AppointmentDto dto)
        {
            var existing = await _appointmentRepository.GetByIdAsync(dto.Id);
            if (existing != null)
            {
                existing.Name = dto.Name;
                existing.Location = dto.Location;
                existing.StartTime = dto.StartTime;
                existing.EndTime = dto.EndTime;

                // Clear old reminders and set new ones
                existing.Reminders = new List<Reminder>();
                foreach (var min in dto.ReminderMinutes)
                {
                    existing.Reminders.Add(new Reminder { MinutesBefore = min, AppointmentId = existing.Id });
                }

                await _appointmentRepository.UpdateAsync(existing);
            }
        }

        public async Task DeleteAppointmentAsync(Guid appointmentId)
        {
            await _appointmentRepository.DeleteAsync(appointmentId);
        }

        public async Task CompleteAppointmentAsync(Guid appointmentId)
        {
            var existing = await _appointmentRepository.GetByIdAsync(appointmentId);
            if (existing != null)
            {
                existing.IsCompleted = true;
                await _appointmentRepository.UpdateAsync(existing);
            }
        }

        public async Task ReplaceAppointmentAsync(Guid userId, Guid oldAppointmentId, AppointmentDto newDto)
        {
            await _appointmentRepository.DeleteAsync(oldAppointmentId);
            await CreateAppointmentAsync(userId, newDto);
        }

        public async Task JoinGroupMeetingAsync(Guid userId, Guid meetingId)
        {
            var participant = new Participant
            {
                UserId = userId,
                GroupMeetingId = meetingId
            };
            await _groupMeetingRepository.AddParticipantAsync(participant);
        }
    }
}
