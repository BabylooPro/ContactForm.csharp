using Xunit;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ContactForm.Tests
{
    public class EmailRequestValidationTests
    {
        // TEST TO VERIFY THAT VALIDATION FAILS WHEN EMAIL IS INVALID.
        [Fact]
        public void EmailRequest_Should_Fail_Validation_When_Email_Is_Invalid()
        {
            // ARRANGE
            var request = new EmailRequest
            {
                Email = "invalid-email",
                Username = "John Doe",
                Message = "Test message"
            };
            var validationResults = new List<ValidationResult>();

            // ACT & ASSERT: VALIDATE EMAIL REQUEST OBJECT & CHECK THAT VALIDATION RETURNS FALSE (INVALID)
            var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true);
            Assert.False(isValid);
        }

        // TEST TO VERIFY THAT VALIDATION PASSES WHEN EMAIL IS VALID.
        [Fact]
        public void EmailRequest_Should_Pass_Validation_When_Email_Is_Valid()
        {
            // ARRANGE
            var request = new EmailRequest
            {
                Email = "valid@email.com",
                Username = "Jane Doe",
                Message = "Hello World"
            };
            var validationResults = new List<ValidationResult>();

            // ACT & ASSERT: VALIDATE EMAIL REQUEST OBJECT & CHECK THAT VALIDATION RETURNS TRUE (VALID)
            var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true);
            Assert.True(isValid);
        }
    }
}
