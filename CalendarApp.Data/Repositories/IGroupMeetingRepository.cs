using System;
using System.Threading.Tasks;
using CalendarApp.Data.Entities;

namespace CalendarApp.Data.Repositories
{
    public interface IGroupMeetingRepository
    {
        Task<GroupMeeting?> FindMatchingMeetingAsync(string name, TimeSpan duration);
        Task<Participant> AddParticipantAsync(Participant participant);
    }
}
