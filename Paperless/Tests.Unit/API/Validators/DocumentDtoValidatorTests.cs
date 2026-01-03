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
            DocumentDto dto = new DocumentDto { FileName = "" };
            TestValidationResult<DocumentDto>? result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.FileName);
        }

        [Test]
        public void ShouldNotHaveError_WhenFileNameIsSpecified()
        {
            DocumentDto dto = new DocumentDto { FileName = "valid.pdf" };
            TestValidationResult<DocumentDto>? result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.FileName);
        }

        [Test]
        public void ShouldHaveError_WhenFileNameIsTooLong()
        {
            DocumentDto dto = new DocumentDto { FileName = new string('a', 256) };
            TestValidationResult<DocumentDto>? result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.FileName);
        }


        [Test]
        public void ShouldHaveError_WhenUploadedAtIsFuture()
        {
            DocumentDto dto = new DocumentDto { UploadedAt = DateTime.Now.AddDays(1) };
            TestValidationResult<DocumentDto>? result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.UploadedAt);
        }
    }
}
