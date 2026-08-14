namespace MaintenanceRequestSystem.Domain.Enums;

public enum SlaStatus
{
    OnTrack = 1,
    DueSoon = 2,
    Breached = 3,
    Met = 4,
    NotApplicable = 5
}
