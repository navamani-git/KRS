namespace KRSDealerManagement.Application.DTOs
{
    public class VehicleChassisHistoryDto
    {
        public int VehicleId { get; set; }
        public required string ChassisNumber { get; set; }
        public required string ModelName { get; set; }
        public required string ColorName { get; set; }
        public int CurrentStatus { get; set; }
        public string? CurrentStatusName { get; set; }
        public string? CurrentHolder { get; set; }
        public List<VehicleChassisHistoryEventDto> Events { get; set; } = new();
    }

    public class VehicleChassisHistoryEventDto
    {
        public int Step { get; set; }
        public DateTime OccurredAt { get; set; }
        public DateTime OccurredAtLocal { get; set; }
        public int StatusValue { get; set; }
        public string? StatusBadgeClass { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public string? OrderNumber { get; set; }
        public string? Actor { get; set; }
        public string? Location { get; set; }
    }
}
