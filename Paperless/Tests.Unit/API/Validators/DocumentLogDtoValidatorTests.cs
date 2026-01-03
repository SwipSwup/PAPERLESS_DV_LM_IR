using API.Validators;
using Core.DTOs;
using FluentValidation.TestHelper;
using NUnit.Framework;
using System;

namespace Tests.Unit.API.Validators
{
    [TestFixture]
    public class DocumentLogDtoValidatorTests
    {
        private DocumentLogDtoValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new DocumentLogDtoValidator();
        }

        [Test]
        public void ShouldHaveError_WhenTimestampIsFuture()
        {
            DocumentLogDto dto = new DocumentLogDto { Timestamp = DateTime.Now.AddDays(1) };
            TestValidationResult<DocumentLogDto>? result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Timestamp);
        }

        [Test]
        public void ShouldHaveError_WhenActionIsEmpty()
        {
            DocumentLogDto dto = new DocumentLogDto { Action = "" };
            TestValidationResult<DocumentLogDto>? result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Action);
        }

        [Test]
        public void ShouldHaveError_WhenActionIsTooLong()
        {
            DocumentLogDto dto = new DocumentLogDto { Action = new string('a', 101) };
            TestValidationResult<DocumentLogDto>? result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Action);
        }

        [Test]
        public void ShouldHaveError_WhenDetailsIsTooLong()
        {
            DocumentLogDto dto = new DocumentLogDto { Details = new string('a', 501) };
            TestValidationResult<DocumentLogDto>? result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Details);
        }

        [Test]
        public void ShouldNotHaveError_WhenDetailsIsNull()
        {
            DocumentLogDto dto = new DocumentLogDto { Details = null };
            // Need valid action
            dto.Action = "Valid Action";
            TestValidationResult<DocumentLogDto>? result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Details);
        }
    }
}
