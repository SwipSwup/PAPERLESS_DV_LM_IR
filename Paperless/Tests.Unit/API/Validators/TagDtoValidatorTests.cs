using API.Validators;
using Core.DTOs;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace Tests.Unit.API.Validators
{
    [TestFixture]
    public class TagDtoValidatorTests
    {
        private TagDtoValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new TagDtoValidator();
        }

        [Test]
        public void ShouldHaveError_WhenNameIsEmpty()
        {
            TagDto dto = new TagDto { Name = "" };
            TestValidationResult<TagDto>? result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Test]
        public void ShouldHaveError_WhenNameIsTooLong()
        {
            TagDto dto = new TagDto { Name = new string('a', 51) };
            TestValidationResult<TagDto>? result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Test]
        public void ShouldNotHaveError_WhenNameIsValid()
        {
            TagDto dto = new TagDto { Name = "Valid Tag" };
            TestValidationResult<TagDto>? result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }
    }
}
