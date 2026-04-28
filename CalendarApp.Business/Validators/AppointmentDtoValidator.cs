using CalendarApp.Business.DTOs;
using FluentValidation;

namespace CalendarApp.Business.Validators
{
    public class AppointmentDtoValidator : AbstractValidator<AppointmentDto>
    {
        public AppointmentDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên cuộc hẹn không được để trống.");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime).WithMessage("Khoảng thời gian (Duration) không được là số âm (Thời gian kết thúc phải lớn hơn bắt đầu).");
        }
    }
}
