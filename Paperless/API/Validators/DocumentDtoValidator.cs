using Core.DTOs;
using FluentValidation;

namespace API.Validators
{
    public class DocumentDtoValidator : AbstractValidator<DocumentDto>
    {
        public DocumentDtoValidator()
        {
            RuleFor(document => document.FileName)
                .NotEmpty().WithMessage("File name is required.")
                .MaximumLength(255).WithMessage("File name cannot exceed 255 characters.");

            RuleFor(document => document.UploadedAt)
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Uploaded date cannot be in the future.");

            RuleForEach(document => document.Tags)
                .NotEmpty().WithMessage("Tags cannot be empty strings.");
        }
    }
}