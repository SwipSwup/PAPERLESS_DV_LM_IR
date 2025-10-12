using Core.DTOs;
using FluentValidation;

namespace API.Validators
{
    public class TagDtoValidator : AbstractValidator<TagDto>
    {
        public TagDtoValidator()
        {
            RuleFor(tag => tag.Name)
                .NotEmpty().WithMessage("Tag name is required.")
                .MaximumLength(50).WithMessage("Tag name cannot exceed 50 characters.");
        }
    }
}