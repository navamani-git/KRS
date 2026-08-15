using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get specific purchase order with details
    /// </summary>
    public class GetPurchaseOrderByIdQuery : IRequest<PurchaseOrderDto>
    {
        public int OrderId { get; set; }
    }
}
