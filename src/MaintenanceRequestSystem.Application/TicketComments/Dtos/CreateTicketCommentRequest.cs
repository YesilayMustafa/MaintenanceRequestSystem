namespace MaintenanceRequestSystem.Application.TicketComments.Dtos;

public sealed class CreateTicketCommentRequest
{
    public string Content { get; init; } = string.Empty;
}