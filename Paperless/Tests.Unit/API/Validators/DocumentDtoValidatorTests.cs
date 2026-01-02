using API.Validators;
using Core.DTOs;
using System;
using System.Collections.Generic;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace Tests.Unit.API.Validators
{
    [TestFixture]
    public class DocumentDtoValidatorTests
    {
        private DocumentDtoValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new DocumentDtoValidator();
        }

        [Test]
        public void ShouldHaveError_WhenFileNameIsEmpty()
        {
            var dto = new DocumentDto { FileName = "" };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.FileName);
        }

        [Test]
        public void ShouldNotHaveError_WhenFileNameIsSpecified()
        {
            var dto = new DocumentDto { FileName = "valid.pdf" };
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.FileName);
        }

        [Test]
        public void ShouldHaveError_WhenFileNameIsTooLong()
        {
            var dto = new DocumentDto { FileName = new string('a', 256) };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.FileName);
        }


        [Test]
        public void ShouldHaveError_WhenUploadedAtIsFuture()
        {
            var dto = new DocumentDto { UploadedAt = DateTime.Now.AddDays(1) };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.UploadedAt);
        }
    }
}
