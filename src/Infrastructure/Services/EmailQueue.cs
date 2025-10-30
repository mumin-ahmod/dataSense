using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DataSenseAPI.Infrastructure.Services;

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
    public int RetryCount { get; set; }
}

public interface IEmailQueue
{
    void Enqueue(EmailMessage message);
    bool TryDequeue(out EmailMessage? message);
    int Count { get; }
}

public class EmailQueue : IEmailQueue
{
    private readonly ConcurrentQueue<EmailMessage> _queue = new();

    public void Enqueue(EmailMessage message)
    {
        _queue.Enqueue(message);
    }

    public bool TryDequeue(out EmailMessage? message)
    {
        return _queue.TryDequeue(out message);
    }

    public int Count => _queue.Count;
}

