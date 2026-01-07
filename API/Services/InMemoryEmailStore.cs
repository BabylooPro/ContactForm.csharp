using System.Collections.Concurrent;
using API.Interfaces;
using API.Models;

namespace API.Services
{
    // IN-MEMORY EMAIL STORE IMPLEMENTATION
    public class InMemoryEmailStore : IEmailStore
    {
        private readonly ConcurrentDictionary<string, EmailResource> _emails = new(StringComparer.OrdinalIgnoreCase);

        // UPSERT EMAIL RESOURCE
        public void Upsert(EmailResource email)
        {
            _emails[email.Id] = email;
        }

        // TRY GET EMAIL RESOURCE BY ID
        public bool TryGet(string id, out EmailResource email)
        {
            return _emails.TryGetValue(id, out email!);
        }
    }
}
