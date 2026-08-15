using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Web.Helpers
{
    public static class VehicleExportHelper
    {
        public static (IReadOnlyList<string> Headers, IEnumerable<IReadOnlyList<object?>> Rows) Build(
            IEnumerable<VehicleDto> vehicles,
            IReadOnlyDictionary<int, Vehicle> vehicleEntities,
            IReadOnlyDictionary<int, VehicleBooking> bookingsByVehicleId,
            IReadOnlyDictionary<int, string> documentTypes,
            IReadOnlyDictionary<int, string> rtoLocations,
            IReadOnlyDictionary<int, string> financeNames,
            IReadOnlyDictionary<int, string> bookingStatusNames,
            bool includeSubdealerColumn)
        {
            var headers = new List<string>
            {
                "Vehicle ID",
                "Chassis",
                "Model",
                "Color",
                "Vehicle Status",
            };

            if (includeSubdealerColumn)
                headers.Add("Subdealer");

            headers.AddRange(new[]
            {
                "Order Date",
                "Order #",
                "Order ID",
                "Allocated Date",
                "Current Price",
                "Original Price",
                "Motor",
                "Battery",
                "Charger",
                "Controller",
                "Converter",
                "Delivery Status",
                "Change History",
                "Vehicle Created",
                "Vehicle Modified",
                "Booking ID",
                "Booking Status",
                "Customer Name",
                "Company Booking",
                "Customer Mobile",
                "Alt Mobile",
                "Customer Email",
                "Document Type",
                "RTO Location",
                "Fancy Number",
                "Payment Mode",
                "Financier",
                "Nominee Name",
                "Nominee DOB",
                "Nominee Relationship",
                "Booking Submitted",
                "Paper Received",
                "Invoice Date",
                "Insurance Date",
                "Agent Date",
                "Registration Date",
                "RTO Number",
                "Number Plate Received",
                "Subsidy ID",
                "Subsidy Customer Name",
                "Subsidy Docs Submitted"
            });

            var rows = vehicles.Select(v =>
            {
                vehicleEntities.TryGetValue(v.VehicleId, out var entity);
                bookingsByVehicleId.TryGetValue(v.VehicleId, out var booking);

                string? docType = null;
                string? rto = null;
                string? finance = null;
                string? bookingStatus = null;

                if (booking != null)
                {
                    documentTypes.TryGetValue(booking.DocumentTypeId, out docType);
                    rtoLocations.TryGetValue(booking.RtoLocationId, out rto);
                    financeNames.TryGetValue(booking.FinanceNameId, out finance);
                    bookingStatusNames.TryGetValue(booking.BookingStatus, out bookingStatus);
                }

                var row = new List<object?>
                {
                    v.VehicleId,
                    v.ChassisNumber,
                    v.ModelName,
                    v.ColorName,
                    v.GetStatusDisplay(),
                };

                if (includeSubdealerColumn)
                    row.Add(v.SubdealerName ?? "-");

                row.AddRange(new object?[]
                {
                    v.OrderDate,
                    v.OrderNumber ?? "-",
                    v.PurchaseOrderId,
                    v.AllocatedDate,
                    v.CurrentPrice,
                    entity?.OriginalPrice ?? v.CurrentPrice,
                    v.MotorNo ?? "-",
                    v.BatteryNo ?? "-",
                    v.ChargerNo ?? "-",
                    v.ControllerNo ?? "-",
                    v.ConverterNo ?? "-",
                    v.GetDeliveryStatusDisplay(),
                    v.Notes ?? "-",
                    v.CreatedDate,
                    v.ModifiedDate,
                    booking?.VehicleBookingId,
                    booking != null ? (bookingStatus ?? booking.BookingStatus.ToString()) : "-",
                    booking?.CustomerName ?? "-",
                    booking == null ? "-" : (booking.IsCompanyBooking ? "Yes" : "No"),
                    booking?.CustomerMobile ?? "-",
                    booking?.AlternativeMobile ?? "-",
                    booking?.CustomerEmail ?? "-",
                    docType ?? "-",
                    rto ?? "-",
                    booking == null ? "-" : (booking.FancyNumber ? "Yes" : "No"),
                    booking != null ? VehiclePaymentModes.GetLabel(booking.PaymentMode) : "-",
                    finance ?? "-",
                    booking?.NomineeName ?? "-",
                    booking != null && booking.NomineeDob != default ? booking.NomineeDob.Date : null,
                    booking?.NomineeRelationship ?? "-",
                    booking?.SubmittedDate,
                    booking?.PaperReceivedDate,
                    booking?.InvoiceDate,
                    booking?.InsuranceDate,
                    booking?.AgentDate,
                    booking?.RegistrationDate,
                    booking?.RtoNumber ?? "-",
                    booking?.NumberPlateReceivedDate,
                    booking?.SubsidyId ?? "-",
                    booking?.SubsidyCustomerNameCaps ?? "-",
                    booking?.SubsidyDocsSubmittedDate
                });

                return (IReadOnlyList<object?>)row;
            });

            return (headers, rows);
        }
    }
}
