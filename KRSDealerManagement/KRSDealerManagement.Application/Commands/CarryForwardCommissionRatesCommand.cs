using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class CarryForwardCommissionRatesCommand : IRequest<int>
    {
        public int CreatedBy { get; set; }
    }
}
