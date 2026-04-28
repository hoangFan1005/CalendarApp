using AutoMapper;
using CalendarApp.Business.DTOs;
using CalendarApp.Data.Entities;

namespace CalendarApp.Business.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Appointment, AppointmentDto>()
                .ForMember(dest => dest.ReminderMinutes, opt => opt.MapFrom(src => src.Reminders.Select(r => r.MinutesBefore).ToList()));

            CreateMap<AppointmentDto, Appointment>()
                .ForMember(dest => dest.Reminders, opt => opt.Ignore()); // We map reminders manually

            CreateMap<GroupMeeting, GroupMeetingDto>();
        }
    }
}
