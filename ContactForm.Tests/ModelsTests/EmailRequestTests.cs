using System.ComponentModel.DataAnnotations;
using ContactForm.MinimalAPI.Models;

namespace ContactForm.Tests.Models
{
    // UNIT TESTS FOR EMAIL REQUEST MODEL
    public class EmailRequestTests
    {
        // TEST FOR EMAIL REQUEST PASSES VALIDATION WHEN MODEL IS VALID
        [Fact]
        public void EmailRequest_ValidModel_PassesValidation()
        {
            // ARRANGE - CREATE MODEL
            var request = new EmailRequest
            {
                Email = "test@example.com",
                Username = "testuser",
                Message = "Hello, this is a test message."
            };
            var context = new ValidationContext(request, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();

            // ACT - VALIDATE MODEL
            bool isValid = Validator.TryValidateObject(request, context, results, true);

            // ASSERT - MODEL VALID
            Assert.True(isValid);
        }

        // TEST FOR EMAIL REQUEST FAILS VALIDATION WHEN MODEL IS INVALID
        [Theory]
        [InlineData("", "testuser", "Hello, this is a test message.", false)] // EMPTY EMAIL
        [InlineData("test@example.com", "testuser", "", false)] // EMPTY MESSAGE
        [InlineData("invalid-email", "testuser", "Hello, this is a test message.", false)] // INVALID EMAIL
        public void EmailRequest_Validation_FailsOnInvalidData(string email, string username, string message, bool expectedIsValid)
        {
            // ARRANGE - INIT TEST DATA
            var request = new EmailRequest
            {
                Email = email,
                Username = username,
                Message = message
            };
            var context = new ValidationContext(request, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();

            // ACT - VALIDATE MODEL
            bool isValid = Validator.TryValidateObject(request, context, results, true);

            // ASSERT - VERIFY RESULT
            Assert.Equal(expectedIsValid, isValid);
            if (!expectedIsValid)
            {
                Assert.NotEmpty(results);
            }
        }

        // TEST FOR EMAIL REQUEST WITH EMPTY USERNAME STILL PASSES VALIDATION
        [Fact]
        public void EmailRequest_EmptyUsername_PassesValidation()
        {
            // ARRANGE - CREATE MODEL
            var request = new EmailRequest
            {
                Email = "test@example.com",
                Username = "",
                Message = "Hello, this is a test message."
            };
            var context = new ValidationContext(request, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();

            // ACT - VALIDATE MODEL
            bool isValid = Validator.TryValidateObject(request, context, results, true);

            // ASSERT - VALID MODEL
            Assert.True(isValid);
            Assert.Empty(results);
        }
    }
}
