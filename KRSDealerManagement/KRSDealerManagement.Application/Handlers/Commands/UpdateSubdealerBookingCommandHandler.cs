using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class UpdateSubdealerBookingCommandHandler : IRequestHandler<UpdateSubdealerBookingCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public UpdateSubdealerBookingCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(UpdateSubdealerBookingCommand request, CancellationToken cancellationToken)
        {
            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(request.VehicleBookingId);
            if (booking == null || booking.SubdealerId != request.SubdealerId)
                return false;

            if (booking.InvoiceDate.HasValue)
                throw new InvalidOperationException("Booking cannot be edited after the vehicle has been invoiced.");

            var changes = new List<string>();

            if (!string.Equals(booking.CustomerName?.Trim(), request.CustomerName.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Customer", booking.CustomerName, request.CustomerName.Trim()));
            if (booking.IsCompanyBooking != request.IsCompanyBooking)
                changes.Add(CorrectionNoteHelper.DescribeChange("Company Booking", booking.IsCompanyBooking, request.IsCompanyBooking));
            if (!string.Equals(booking.CustomerMobile?.Trim(), request.CustomerMobile.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Mobile", booking.CustomerMobile, request.CustomerMobile.Trim()));
            if (!string.Equals(booking.AlternativeMobile?.Trim(), request.AlternativeMobile.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Alt Mobile", booking.AlternativeMobile, request.AlternativeMobile.Trim()));
            if (!string.Equals(booking.CustomerEmail?.Trim(), request.CustomerEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Email", booking.CustomerEmail, request.CustomerEmail.Trim()));
            if (booking.DocumentTypeId != request.DocumentTypeId)
                changes.Add(CorrectionNoteHelper.DescribeChange("Document Type", booking.DocumentTypeId, request.DocumentTypeId));
            if (booking.RtoLocationId != request.RtoLocationId)
                changes.Add(CorrectionNoteHelper.DescribeChange("RTO", booking.RtoLocationId, request.RtoLocationId));
            if (booking.FancyNumber != request.FancyNumber)
                changes.Add(CorrectionNoteHelper.DescribeChange("Fancy Number", booking.FancyNumber, request.FancyNumber));
            if (!string.Equals(booking.PaymentMode, request.PaymentMode, StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Payment Mode", booking.PaymentMode, request.PaymentMode));
            if (booking.FinanceNameId != request.FinanceNameId)
                changes.Add(CorrectionNoteHelper.DescribeChange("Finance", booking.FinanceNameId, request.FinanceNameId));
            if (!string.Equals(booking.NomineeName?.Trim(), request.NomineeName.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Nominee", booking.NomineeName, request.NomineeName.Trim()));
            if (booking.NomineeDob.Date != request.NomineeDob.Date)
                changes.Add(CorrectionNoteHelper.DescribeChange("Nominee DOB", booking.NomineeDob.ToString("yyyy-MM-dd"), request.NomineeDob.ToString("yyyy-MM-dd")));
            if (!string.Equals(booking.NomineeRelationship?.Trim(), request.NomineeRelationship.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Nominee Relation", booking.NomineeRelationship, request.NomineeRelationship.Trim()));

            booking.CustomerName = request.CustomerName.Trim();
            booking.IsCompanyBooking = request.IsCompanyBooking;
            booking.CustomerMobile = request.CustomerMobile.Trim();
            booking.AlternativeMobile = request.AlternativeMobile.Trim();
            booking.CustomerEmail = request.CustomerEmail.Trim();
            booking.EAadhaarPassword = request.EAadhaarPassword.Trim();
            booking.DocumentTypeId = request.DocumentTypeId;
            booking.RtoLocationId = request.RtoLocationId;
            booking.FancyNumber = request.FancyNumber;
            booking.PaymentMode = request.PaymentMode;
            booking.FinanceNameId = request.FinanceNameId;
            booking.NomineeName = request.NomineeName.Trim();
            booking.NomineeDob = request.NomineeDob.Date;
            booking.NomineeRelationship = request.NomineeRelationship.Trim();

            if (!string.IsNullOrWhiteSpace(request.EAadhaarPath)) { changes.Add("E-Aadhaar file replaced"); booking.EAadhaarPath = request.EAadhaarPath; }
            if (!string.IsNullOrWhiteSpace(request.DocumentPath)) { changes.Add("ID document replaced"); booking.DocumentPath = request.DocumentPath; }
            if (!string.IsNullOrWhiteSpace(request.GstCertificatePath)) { changes.Add("GST certificate replaced"); booking.GstCertificatePath = request.GstCertificatePath; }
            if (!string.IsNullOrWhiteSpace(request.CustomerPhotoPath)) { changes.Add("Customer photo replaced"); booking.CustomerPhotoPath = request.CustomerPhotoPath; }
            if (!string.IsNullOrWhiteSpace(request.ChassisPhotoPath)) { changes.Add("Chassis photo replaced"); booking.ChassisPhotoPath = request.ChassisPhotoPath; }
            if (!string.IsNullOrWhiteSpace(request.CustomerSignPath)) { changes.Add("Sign/seal replaced"); booking.CustomerSignPath = request.CustomerSignPath; }

            if (request.IsCompanyBooking && string.IsNullOrWhiteSpace(booking.GstCertificatePath))
                throw new InvalidOperationException("GST certificate is required for company bookings.");

            booking.ModifiedBy = request.UpdatedBy;
            booking.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.VehicleBookings.UpdateAsync(booking);

            var reason = string.IsNullOrWhiteSpace(request.EditReason) ? "Subdealer booking update" : request.EditReason.Trim();
            var note = CorrectionNoteHelper.FormatEntry(request.UpdatedByName ?? $"User #{request.UpdatedBy}", reason, changes);

            await _auditService.LogActionAsync(
                entityType: "VehicleBooking",
                entityId: booking.VehicleBookingId,
                action: "SubdealerUpdate",
                userId: request.UpdatedBy,
                userRole: "Subdealer",
                newValue: JsonSerializer.Serialize(new { booking.VehicleBookingId, Changes = changes }),
                remarks: note);

            return true;
        }
    }
}
