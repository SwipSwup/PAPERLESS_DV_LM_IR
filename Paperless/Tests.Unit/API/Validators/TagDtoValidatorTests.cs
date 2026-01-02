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
            var dto = new TagDto { Name = "" };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Test]
        public void ShouldHaveError_WhenNameIsTooLong()
        {
            var dto = new TagDto { Name = new string('a', 51) };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Test]
        public void ShouldNotHaveError_WhenNameIsValid()
        {
            var dto = new TagDto { Name = "Valid Tag" };
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }
    }
}
