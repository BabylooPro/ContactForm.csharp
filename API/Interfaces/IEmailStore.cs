using API.Models;

namespace API.Interfaces
{
    // INTERFACE FOR EMAIL STORE
    public interface IEmailStore
    {
        void Upsert(EmailResource email);
        bool TryGet(string id, out EmailResource email);
    }
}
