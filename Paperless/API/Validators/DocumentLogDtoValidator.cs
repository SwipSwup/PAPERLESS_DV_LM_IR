using Core.DTOs;
using FluentValidation;

namespace API.Validators
{
    public class DocumentLogDtoValidator : AbstractValidator<DocumentLogDto>
    {
        public DocumentLogDtoValidator()
        {
            RuleFor(documentLog => documentLog.Timestamp)
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Timestamp cannot be in the future.");

            RuleFor(documentLog => documentLog.Action)
                .NotEmpty().WithMessage("Action is required.")
                .MaximumLength(100).WithMessage("Action cannot exceed 100 characters.");

            RuleFor(documentLog => documentLog.Details)
                .MaximumLength(500).WithMessage("Details cannot exceed 500 characters.")
                .When(documentLog => documentLog.Details != null);
        }
    }
}