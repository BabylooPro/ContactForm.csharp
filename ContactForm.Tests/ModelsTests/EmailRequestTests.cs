using Xunit;
using System.ComponentModel.DataAnnotations;
using ContactForm.MinimalAPI.Models;
using System.Collections.Generic;

namespace ContactForm.Tests.Models
{
    // UNIT TESTS FOR EMAIL REQUEST MODEL
    public class EmailRequestTests
    {
        // TEST FOR EMAIL REQUEST PASSES VALIDATION WHEN MODEL IS VALID
        [Fact]
        public void EmailRequest_ValidModel_PassesValidation()
        {
            // ARRANGE - CREATING VALID EMAIL REQUEST
            var request = new EmailRequest
            {
                Email = "test@example.com",
                Username = "testuser",
                Message = "Hello, this is a test message."
            };
            var context = new ValidationContext(request, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();

            // ACT - VALIDATING EMAIL REQUEST
            bool isValid = Validator.TryValidateObject(request, context, results, true);

            // ASSERT - CHECKING IF MODEL IS VALID
            Assert.True(isValid);
        }

        // TEST FOR EMAIL REQUEST FAILS VALIDATION WHEN MODEL IS INVALID
        [Theory]
        [InlineData("", "testuser", "Hello, this is a test message.", false)] // Empty email
        [InlineData("test@example.com", "", "Hello, this is a test message.", false)] // Empty username
        [InlineData("test@example.com", "testuser", "", false)] // Empty message
        [InlineData("invalid-email", "testuser", "Hello, this is a test message.", false)] // Invalid email
        public void EmailRequest_Validation_FailsOnInvalidData(string email, string username, string message, bool expectedIsValid)
        {
            // ARRANGE - CREATING EMAIL REQUEST WITH INVALID DATA
            var request = new EmailRequest
            {
                Email = email,
                Username = username,
                Message = message
            };
            var context = new ValidationContext(request, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();

            // ACT - VALIDATING EMAIL REQUEST
            bool isValid = Validator.TryValidateObject(request, context, results, true);

            // ASSERT - CHECKING IF MODEL IS VALID
            Assert.Equal(expectedIsValid, isValid);
            if (!expectedIsValid)
            {
                Assert.NotEmpty(results);
            }
        }
    }
}
