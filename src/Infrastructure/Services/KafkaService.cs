using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Infrastructure.Services;

public class KafkaService : IKafkaService
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaService> _logger;
    private const string RequestLogTopic = "datasense-request-logs";
    private const string PricingTopic = "datasense-pricing";
    private const string OllamaRequestsTopic = "datasense-ollama-requests";

    public KafkaService(ILogger<KafkaService> logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092",
            Acks = Acks.All,
            Retries = 3,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger = logger;
    }

    public async Task ProduceAsync(string topic, string message, Dictionary<string, string>? headers = null)
    {
        try
        {
            var kafkaMessage = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = message
            };

            if (headers != null)
            {
                kafkaMessage.Headers = new Headers();
                foreach (var header in headers)
                {
                    kafkaMessage.Headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));
                }
            }

            await _producer.ProduceAsync(topic, kafkaMessage);
            _logger.LogDebug("Message produced to topic: {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error producing message to Kafka topic: {Topic}", topic);
            throw;
        }
    }

    public async Task ProduceRequestLogAsync(RequestLog log)
    {
        try
        {
            var json = JsonSerializer.Serialize(log);
            await ProduceAsync(RequestLogTopic, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error producing request log to Kafka");
            throw;
        }
    }

    public async Task ProducePricingRecordAsync(PricingRecord record)
    {
        try
        {
            var json = JsonSerializer.Serialize(record);
            await ProduceAsync(PricingTopic, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error producing pricing record to Kafka");
            throw;
        }
    }

    public async Task ProduceOllamaRequestAsync(string conversationId, string prompt, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var request = new
            {
                conversationId,
                prompt,
                timestamp = DateTime.UtcNow,
                metadata = metadata ?? (object?)null
            };

            var json = JsonSerializer.Serialize(request);
            var headers = new Dictionary<string, string>
            {
                { "conversation-id", conversationId }
            };

            await ProduceAsync(OllamaRequestsTopic, json, headers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error producing Ollama request to Kafka");
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}

