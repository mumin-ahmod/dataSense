using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataSenseAPI.Infrastructure.Services;

public class EmailSenderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEmailQueue _emailQueue;
    private readonly ILogger<EmailSenderBackgroundService> _logger;
    private readonly TimeSpan _processInterval = TimeSpan.FromSeconds(2);
    private readonly int _maxRetries = 3;

    public EmailSenderBackgroundService(
        IServiceProvider serviceProvider,
        IEmailQueue emailQueue,
        ILogger<EmailSenderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _emailQueue = emailQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Sender Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessEmailQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email queue");
            }

            await Task.Delay(_processInterval, stoppingToken);
        }

        _logger.LogInformation("Email Sender Background Service stopped");
    }

    private async Task ProcessEmailQueueAsync(CancellationToken cancellationToken)
    {
        while (_emailQueue.TryDequeue(out var emailMessage) && emailMessage != null)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                _logger.LogDebug("Processing email to {To}", emailMessage.To);
                await emailSender.SendEmailAsync(emailMessage.To, emailMessage.Subject, emailMessage.HtmlBody);
                _logger.LogInformation("Email sent successfully to {To}", emailMessage.To);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}. Retry count: {RetryCount}", 
                    emailMessage.To, emailMessage.RetryCount);

                // Retry logic
                if (emailMessage.RetryCount < _maxRetries)
                {
                    emailMessage.RetryCount++;
                    _emailQueue.Enqueue(emailMessage);
                    _logger.LogInformation("Email to {To} re-queued for retry ({RetryCount}/{MaxRetries})",
                        emailMessage.To, emailMessage.RetryCount, _maxRetries);
                }
                else
                {
                    _logger.LogError("Email to {To} failed after {MaxRetries} attempts and will be discarded",
                        emailMessage.To, _maxRetries);
                }
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email Sender Background Service is stopping. Queue size: {QueueSize}", _emailQueue.Count);
        return base.StopAsync(cancellationToken);
    }
}

