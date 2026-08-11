namespace MaintenanceRequestSystem.Application.Authentication.Models;

public sealed record EmailMessage(
    string To,
    string Subject,
    string TextBody,
    string HtmlBody);
