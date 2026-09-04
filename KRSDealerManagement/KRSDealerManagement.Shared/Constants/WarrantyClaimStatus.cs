namespace KRSDealerManagement.Shared.Constants
{
    public static class WarrantyClaimStatus
    {
        public const int Draft = 0;
        public const int Submitted = 1;
        public const int MoreInfoRequested = 2;
        public const int Rejected = 3;
        public const int Approved = 4;
        public const int AppliedToAmpere = 5;
        public const int ProductReceived = 6;
        public const int CollectedBySubdealer = 7;
        public const int DefectiveSubmitted = 8;
        public const int DefectiveSentToAmpere = 9;

        public static bool IsSubdealerEditable(int status) => status is Draft or MoreInfoRequested;

        public static bool CanStaffReview(int status) => status is Submitted;

        public static bool CanApplyToAmpere(int status) => status is Approved;

        public static bool CanMarkProductReceived(int status) => status is AppliedToAmpere;

        public static bool CanSubdealerCollect(int status) => status is ProductReceived;

        public static bool CanSubdealerSubmitDefective(int status) => status is CollectedBySubdealer;

        public static bool CanMarkDefectiveSentToAmpere(int status) => status is DefectiveSubmitted;

        public static bool IsTerminalForSubdealer(int status)
            => status is Rejected or DefectiveSubmitted or DefectiveSentToAmpere;
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
