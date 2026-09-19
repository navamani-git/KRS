namespace KRSDealerManagement.Shared.Constants
{
    public static class WarrantyClaimStatus
    {
        public const int Draft = 0;
        public const int Submitted = 1;
        public const int MoreInfoRequested = 2;
        public const int Rejected = 3;
        public const int Accepted = 4;
        public const int AppliedToAmpere = 5;
        public const int AmpereApproved = 6;
        public const int Complete = 7;

        [Obsolete("Use Accepted")]
        public const int Approved = Accepted;

        public static bool IsSubdealerEditable(int status) => status is Draft or MoreInfoRequested;

        public static bool CanStaffReview(int status) => status is Submitted;

        public static bool CanAccept(int status) => status is Submitted;

        public static bool CanApplyToAmpere(int status) => status is Accepted;

        public static bool CanMarkAmpereApproved(int status) => status is AppliedToAmpere;

        public static bool CanUpdateSoNumber(int status) => status is AmpereApproved or Complete;

        /// <summary>Staff may correct post-Ampere workflow steps until defective handover to Ampere is recorded.</summary>
        public static bool CanStaffEditPostAmpereWorkflow(int status, bool defectiveSentToAmpereCompleted)
            => status == AmpereApproved && !defectiveSentToAmpereCompleted;

        public static bool CanStaffEditSoNumber(int status, bool defectiveSentToAmpereCompleted)
            => CanStaffEditPostAmpereWorkflow(status, defectiveSentToAmpereCompleted);

        public static bool CanStaffEditResolutionPart(int status, bool defectiveSentToAmpereCompleted)
            => CanStaffEditPostAmpereWorkflow(status, defectiveSentToAmpereCompleted);

        public static bool CanStaffEditDealerInvoiceClosed(int status, bool defectiveSentToAmpereCompleted)
            => CanStaffEditPostAmpereWorkflow(status, defectiveSentToAmpereCompleted);

        public static bool CanStaffEditReplacementPartReceived(int status, bool defectiveSentToAmpereCompleted)
            => CanStaffEditPostAmpereWorkflow(status, defectiveSentToAmpereCompleted);

        public static bool CanStaffMarkDefectiveSentToAmpere(int status, bool defectiveSentToAmpereCompleted)
            => CanStaffEditPostAmpereWorkflow(status, defectiveSentToAmpereCompleted);

        public static bool CanStaffEditSubdealerPartReceived(int status, bool defectiveSentToAmpereCompleted)
            => CanStaffEditPostAmpereWorkflow(status, defectiveSentToAmpereCompleted);

        public static bool CanStaffEditDefectiveHandover(int status, bool defectiveSentToAmpereCompleted)
            => CanStaffEditPostAmpereWorkflow(status, defectiveSentToAmpereCompleted);

        public static bool CanSubdealerMarkSubdealerPartReceived(int status, bool completed, bool lockedByStaff)
            => CanMarkSubdealerPartReceived(status, completed) && !lockedByStaff;

        public static bool CanSubdealerMarkDefectiveHandover(int status, bool completed, bool lockedByStaff)
            => CanMarkDefectiveHandover(status, completed) && !lockedByStaff;

        public static bool IsPostAmperePhase(int status) => status is AmpereApproved or Complete;

        public static bool IsTerminalForSubdealer(int status)
            => status is Rejected or Complete;

        public static bool AllProgressFlagsComplete(
            bool resolutionPartCompleted,
            bool dealerInvoiceClosedCompleted,
            bool replacementPartReceivedCompleted,
            bool subdealerPartReceivedCompleted,
            bool defectiveHandoverCompleted,
            bool defectiveSentToAmpereCompleted)
            => resolutionPartCompleted
               && dealerInvoiceClosedCompleted
               && replacementPartReceivedCompleted
               && subdealerPartReceivedCompleted
               && defectiveHandoverCompleted
               && defectiveSentToAmpereCompleted;

        public static bool CanSaveResolutionPart(int status, bool completed)
            => status == AmpereApproved && !completed;

        public static bool CanSaveDealerInvoiceClosed(int status, bool completed)
            => status == AmpereApproved && !completed;

        public static bool CanMarkReplacementPartReceived(int status, bool completed)
            => status == AmpereApproved && !completed;

        public static bool CanMarkSubdealerPartReceived(int status, bool completed)
            => status == AmpereApproved && !completed;

        public static bool CanMarkDefectiveHandover(int status, bool completed)
            => status == AmpereApproved && !completed;

        public static bool CanMarkDefectiveSentToAmpere(int status, bool completed)
            => status == AmpereApproved && !completed;
    }

    public static class WarrantyDealerResolutionTypes
    {
        public const string Credit = "CREDIT";
        public const string Replacement = "REPLACEMENT";
        public const string SameItem = "SAME_ITEM";

        public static readonly string[] All = { Credit, Replacement, SameItem };

        public static string GetDisplayName(string? type) => type switch
        {
            Credit => "Given to subdealer as credit",
            Replacement => "Give replacement item",
            SameItem => "Give same item",
            _ => type ?? ""
        };
    }

    public static class WarrantyClaimTypes
    {
        public const string Warranty = "WARRANTY";
        public const string Campaign = "CAMPAIGN";

        public static readonly string[] All = { Warranty, Campaign };
    }

    public static class WarrantyServiceTypes
    {
        public const string Paid = "PAID";
        public const string Warranty = "WARRANTY";
        public const string Accident = "ACCIDENT";
        public const string Spare = "SPARE";

        public static readonly string[] All = { Paid, Warranty, Accident, Spare };
    }

    public static class WarrantyAttachmentTypes
    {
        public const string ChassisPhoto = "CHASSIS_PHOTO";
        public const string KmsPhoto = "KMS_PHOTO";
        public const string FailurePartWithSerialPhoto = "FAILURE_PART_SERIAL_PHOTO";
        public const string FailurePartPhoto = "FAILURE_PART_PHOTO";
        public const string FailurePartFront = "FAILURE_PART_FRONT";
        public const string FailurePartRear = "FAILURE_PART_REAR";
        public const string FailurePartTop = "FAILURE_PART_TOP";
        public const string FailurePartSideLh = "FAILURE_PART_SIDE_LH";
        public const string FailurePartSideRh = "FAILURE_PART_SIDE_RH";
        public const string FailurePartVideoFailureVehicle = "FAILURE_PART_VIDEO_FAILURE_VEHICLE";
        public const string OkPartVideoFailureVehicle = "OK_PART_VIDEO_FAILURE_VEHICLE";
        public const string FailurePartVideoOtherVehicle = "FAILURE_PART_VIDEO_OTHER_VEHICLE";
        public const string ReplacementPartWithSerialPhoto = "REPLACEMENT_PART_SERIAL_PHOTO";

        public static IReadOnlyList<string> RequiredForWarranty => new[]
        {
            ChassisPhoto, KmsPhoto, FailurePartWithSerialPhoto, FailurePartPhoto,
            FailurePartFront, FailurePartRear, FailurePartTop, FailurePartSideLh, FailurePartSideRh,
            FailurePartVideoFailureVehicle, OkPartVideoFailureVehicle, FailurePartVideoOtherVehicle
        };

        public static IReadOnlyList<string> RequiredForCampaign => new[]
        {
            ChassisPhoto, KmsPhoto, FailurePartWithSerialPhoto, ReplacementPartWithSerialPhoto
        };

        public static string GetDisplayName(string type) => type switch
        {
            ChassisPhoto => "VIN / Chassis Photo",
            KmsPhoto => "KMS Photo",
            FailurePartWithSerialPhoto => "Failure Part Photo With Serial Number",
            FailurePartPhoto => "Failure Part Photo",
            FailurePartFront => "Failure Part Front",
            FailurePartRear => "Failure Part Rear",
            FailurePartTop => "Failure Part Top",
            FailurePartSideLh => "Failure Part Side LH",
            FailurePartSideRh => "Failure Part Side RH",
            FailurePartVideoFailureVehicle => "Failure Part Video with Failure Vehicle",
            OkPartVideoFailureVehicle => "OK Part Video with Failure Vehicle",
            FailurePartVideoOtherVehicle => "Failure Part Other Vehicle Video",
            ReplacementPartWithSerialPhoto => "Replacement Part Photo With Serial Number",
            _ => type
        };

        public static string GetAttachmentAccept(string type)
            => type.Contains("VIDEO", StringComparison.OrdinalIgnoreCase) ? "video/*" : "image/*";

        public static bool IsImageAttachment(string type)
            => !type.Contains("VIDEO", StringComparison.OrdinalIgnoreCase);
    }
}
