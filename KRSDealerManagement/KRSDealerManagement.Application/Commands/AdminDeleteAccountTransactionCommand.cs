using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class AdminDeleteAccountTransactionCommand : IRequest<bool>
    {
        public int TransactionId { get; set; }
        public string DeleteReason { get; set; } = "";
        public int DeletedBy { get; set; }
        public string? DeletedByName { get; set; }
    }
}
