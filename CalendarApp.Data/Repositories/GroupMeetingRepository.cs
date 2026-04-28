using System;
using System.Linq;
using System.Threading.Tasks;
using CalendarApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CalendarApp.Data.Repositories
{
    public class GroupMeetingRepository : IGroupMeetingRepository
    {
        private readonly CalendarDbContext _context;

        public GroupMeetingRepository(CalendarDbContext context)
        {
            _context = context;
        }

        public async Task<GroupMeeting?> FindMatchingMeetingAsync(string name, TimeSpan duration)
        {
            var meetings = await _context.GroupMeetings.ToListAsync();
            // We calculate duration on client side due to TimeSpan support in EF Core limitations
            return meetings.FirstOrDefault(m => 
                m.MeetingName.Equals(name, StringComparison.OrdinalIgnoreCase) && 
                m.GetDuration() == duration);
        }

        public async Task<Participant> AddParticipantAsync(Participant participant)
        {
            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();
            return participant;
        }
    }
}
