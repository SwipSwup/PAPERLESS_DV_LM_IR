using API.Validators;
using Core.DTOs;
using FluentValidation.TestHelper;
using NUnit.Framework;
using System;

namespace Tests.Unit.API.Validators
{
    [TestFixture]
    public class AccessLogDtoValidatorTests
    {
        private AccessLogDtoValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new AccessLogDtoValidator();
        }

        [Test]
        public void ShouldHaveError_WhenDateIsFuture()
        {
            AccessLogDto dto = new AccessLogDto { Date = DateTime.Now.AddSeconds(10) }; // Slightly future
            // Note: Precision might be an issue, adding more time
            dto.Date = DateTime.Now.AddDays(1);
            TestValidationResult<AccessLogDto>? result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Date);
        }

        [Test]
        public void ShouldNotHaveError_WhenDateIsPast()
        {
            AccessLogDto dto = new AccessLogDto { Date = DateTime.Now.AddDays(-1) };
            TestValidationResult<AccessLogDto>? result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Date);
        }

        [Test]
        public void ShouldHaveError_WhenCountIsNegative()
        {
            AccessLogDto dto = new AccessLogDto { Count = -1 };
            TestValidationResult<AccessLogDto>? result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Count);
        }

        [Test]
        public void ShouldNotHaveError_WhenCountIsZero()
        {
            AccessLogDto dto = new AccessLogDto { Count = 0 };
            TestValidationResult<AccessLogDto>? result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Count);
        }

        [Test]
        public void ShouldNotHaveError_WhenCountIsPositive()
        {
            AccessLogDto dto = new AccessLogDto { Count = 10 };
            TestValidationResult<AccessLogDto>? result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Count);
        }
    }
}
