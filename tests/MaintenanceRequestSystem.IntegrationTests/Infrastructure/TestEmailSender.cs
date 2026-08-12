using System.Collections.Concurrent;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.Authentication.Models;

namespace MaintenanceRequestSystem.IntegrationTests.Infrastructure;

public sealed class TestEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();
    private int _failNextDelivery;

    public IReadOnlyList<EmailMessage> Messages =>
        _messages.ToArray();

    public void FailNextDelivery()
    {
        Interlocked.Exchange(ref _failNextDelivery, 1);
    }

    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }

        Interlocked.Exchange(ref _failNextDelivery, 0);
    }

    public Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);

        if (Interlocked.Exchange(ref _failNextDelivery, 0) == 1)
        {
            throw new InvalidOperationException(
                "Test email delivery failure.");
        }

        return Task.CompletedTask;
    }
}
