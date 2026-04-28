using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using FluentValidation;
using CalendarApp.Data;
using CalendarApp.Data.Repositories;
using CalendarApp.Business.Services;
using CalendarApp.Business.Mapping;
using CalendarApp.Business.DTOs;
using CalendarApp.Business.Validators;
using CalendarApp.UI.Forms;
using CalendarApp.Data.Entities;

namespace CalendarApp.UI
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var services = new ServiceCollection();

            // 1. DbContext
            services.AddDbContext<CalendarDbContext>();

            // 2. Repositories
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<IGroupMeetingRepository, GroupMeetingRepository>();

            // 3. AutoMapper & FluentValidation
            services.AddAutoMapper(typeof(MappingProfile));
            services.AddScoped<IValidator<AppointmentDto>, AppointmentDtoValidator>();

            // 4. Services
            services.AddScoped<IAppointmentService, AppointmentService>();

            // 5. Forms
            services.AddTransient<MainCalendarForm>();

            var serviceProvider = services.BuildServiceProvider();

            // Initialize DB and Seed Data
            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();
                db.Database.EnsureCreated();

                var mockUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                if (!db.Users.Any(u => u.Id == mockUserId))
                {
                    db.Users.Add(new User { Id = mockUserId, Username = "TestUser" });
                }

                if (!db.GroupMeetings.Any())
                {
                    db.GroupMeetings.Add(new GroupMeeting
                    {
                        MeetingName = "Weekly Sync",
                        StartTime = DateTime.Today.AddHours(10),
                        EndTime = DateTime.Today.AddHours(11) // Duration = 1h
                    });
                }
                db.SaveChanges();
            }

            var mainForm = serviceProvider.GetRequiredService<MainCalendarForm>();
            Application.Run(mainForm);
        }
    }
}