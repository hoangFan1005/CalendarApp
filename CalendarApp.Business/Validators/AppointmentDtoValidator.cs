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

            RuleFor(x => x.Location)
                .NotEmpty().WithMessage("Địa điểm không được để trống.");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime).WithMessage("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
        }
    }
}
