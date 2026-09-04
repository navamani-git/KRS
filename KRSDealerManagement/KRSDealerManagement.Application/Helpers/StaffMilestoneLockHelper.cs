using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Application.Helpers
{
    /// <summary>
    /// Staff may set booking milestone fields only once; after save only admin may change them.
    /// </summary>
    public static class StaffMilestoneLockHelper
    {
        public sealed class MilestoneInput
        {
            public DateTime? PaperReceivedDate { get; set; }
            public DateTime? InvoiceDate { get; set; }
            public DateTime? InsuranceDate { get; set; }
            public DateTime? AgentDate { get; set; }
            public DateTime? RegistrationDate { get; set; }
            public string? RtoNumber { get; set; }
            public string? SubsidyId { get; set; }
        }

        public static string? EnforceForStaff(VehicleBooking existing, MilestoneInput input)
        {
            var paperError = EnforceDateTime(existing.PaperReceivedDate, input.PaperReceivedDate, "Paper Received");
            if (paperError != null) return paperError;
            input.PaperReceivedDate = existing.PaperReceivedDate ?? input.PaperReceivedDate;

            var invoiceError = EnforceDateTime(existing.InvoiceDate, input.InvoiceDate, "Invoice");
            if (invoiceError != null) return invoiceError;
            input.InvoiceDate = existing.InvoiceDate ?? input.InvoiceDate;

            var insuranceError = EnforceDateTime(existing.InsuranceDate, input.InsuranceDate, "Insurance");
            if (insuranceError != null) return insuranceError;
            input.InsuranceDate = existing.InsuranceDate ?? input.InsuranceDate;

            var agentError = EnforceDateTime(existing.AgentDate, input.AgentDate, "Agent");
            if (agentError != null) return agentError;
            input.AgentDate = existing.AgentDate ?? input.AgentDate;

            var registrationError = EnforceDateTime(existing.RegistrationDate, input.RegistrationDate, "Registration");
            if (registrationError != null) return registrationError;
            input.RegistrationDate = existing.RegistrationDate ?? input.RegistrationDate;

            var rtoError = EnforceText(existing.RtoNumber, input.RtoNumber, "RTO Number");
            if (rtoError != null) return rtoError;
            input.RtoNumber = string.IsNullOrWhiteSpace(existing.RtoNumber) ? input.RtoNumber : existing.RtoNumber;

            var subsidyError = EnforceText(existing.SubsidyId, input.SubsidyId, "Subsidy ID");
            if (subsidyError != null) return subsidyError;
            input.SubsidyId = string.IsNullOrWhiteSpace(existing.SubsidyId) ? input.SubsidyId : existing.SubsidyId;

            return null;
        }

        private static string? EnforceDateTime(DateTime? saved, DateTime? submitted, string label)
        {
            if (!saved.HasValue) return null;
            if (!SameDateTime(submitted, saved))
                return $"{label} cannot be changed by staff after it has been saved. Contact admin.";
            return null;
        }

        private static string? EnforceText(string? saved, string? submitted, string label)
        {
            if (string.IsNullOrWhiteSpace(saved)) return null;
            var savedTrim = saved.Trim();
            var submittedTrim = submitted?.Trim();
            if (string.IsNullOrWhiteSpace(submittedTrim)
                || !string.Equals(submittedTrim, savedTrim, StringComparison.OrdinalIgnoreCase))
            {
                return $"{label} cannot be changed by staff after it has been saved. Contact admin.";
            }
            return null;
        }

        private static bool SameDateTime(DateTime? a, DateTime? b)
        {
            if (!a.HasValue && !b.HasValue) return true;
            if (!a.HasValue || !b.HasValue) return false;
            return a.Value.ToString("yyyy-MM-ddTHH:mm") == b.Value.ToString("yyyy-MM-ddTHH:mm");
        }
    }
}
