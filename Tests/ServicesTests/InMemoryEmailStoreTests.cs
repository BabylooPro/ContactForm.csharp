using API.Models;
using API.Services;

namespace Tests.ServicesTests
{
    // UNIT TESTS FOR IN-MEMORY EMAIL STORE
    public class InMemoryEmailStoreTests
    {
        // TEST FOR UPSERT AND TRYGET RETURNS STORED EMAIL (CASE INSENSITIVE KEY)
        [Fact]
        public void Upsert_And_TryGet_ReturnsStoredEmail_CaseInsensitiveKey()
        {
            // ARRANGE - CREATE STORE AND EMAIL
            var store = new InMemoryEmailStore();
            var email = new EmailResource { Id = "AbC123" };

            // ACT - UPSERT EMAIL
            store.Upsert(email);

            // ASSERT - CHECK RESULT
            Assert.True(store.TryGet("aBc123", out var found));
            Assert.Equal("AbC123", found.Id);
        }

        // TEST FOR TRYGET WHEN MISSING RETURNS FALSE
        [Fact]
        public void TryGet_WhenMissing_ReturnsFalse()
        {
            // ARRANGE - CREATE STORE
            var store = new InMemoryEmailStore();

            // ACT - TRYGET MISSING EMAIL
            // ASSERT - CHECK RESULT
            Assert.False(store.TryGet("missing", out _));
        }
    }
}
