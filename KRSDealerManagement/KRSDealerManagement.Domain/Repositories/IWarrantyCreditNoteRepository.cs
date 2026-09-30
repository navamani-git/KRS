using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Domain.Repositories
{
    public interface IWarrantyCreditNoteRepository : IRepository<WarrantyCreditNote>
    {
        Task<WarrantyCreditNote?> GetByClaimIdAsync(int warrantyClaimId);
        Task UpsertAsync(WarrantyCreditNote creditNote, int userId);
    }
}
