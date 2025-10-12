using Core.DTOs;
using FluentValidation;

namespace API.Validators
{
    public class AccessLogDtoValidator : AbstractValidator<AccessLogDto>
    {
        public AccessLogDtoValidator()
        {
            RuleFor(accessLog => accessLog.Date)
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Date cannot be in the future.");

            RuleFor(accessLog => accessLog.Count)
                .GreaterThanOrEqualTo(0).WithMessage("Count must be zero or positive.");
        }
    }
}