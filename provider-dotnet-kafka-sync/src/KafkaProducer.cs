using System;
using System.Text.Json;
using System.Diagnostics;
using Confluent.Kafka;

namespace Products;

public class ProductEventResponse(string id, string @event, bool processed)
{
    public string Id { get; set; } = id;

    public string Event { get; set; } = @event;

    public bool Processed { get; set; } = processed;
}

public class KafkaProducer
{
    private readonly ProducerConfig _config;
    private readonly ConsumerConfig _responseConsumerConfig;

    public KafkaProducer(string bootstrapServers = "localhost:9092")
    {
        _config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _responseConsumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = $"provider-sync-response-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = false
        };
    }

    public ProductEventResponse ProduceProductEventAndWaitForResponse(
        ProductEvent productEvent,
        string requestTopic = "products.requests",
        string responseTopic = "products.responses",
        int timeoutMs = 10000)
    {
        var correlationId = Guid.NewGuid().ToString("N");

        using var responseConsumer = new ConsumerBuilder<string, string>(_responseConsumerConfig).Build();
        using var producer = new ProducerBuilder<string, string>(_config).Build();

        try
        {
            responseConsumer.Subscribe(responseTopic);

            var message = new Message<string, string>
            {
                Key = correlationId,
                Value = JsonSerializer.Serialize(productEvent)
            };

            var deliveryResult = producer.ProduceAsync(requestTopic, message).GetAwaiter().GetResult();
            producer.Flush(TimeSpan.FromSeconds(5));
            Console.WriteLine($"Delivered '{deliveryResult.Value}' to '{deliveryResult.TopicPartitionOffset}'");

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                var response = responseConsumer.Consume(TimeSpan.FromMilliseconds(250));
                if (response?.Message == null || response.Message.Key != correlationId)
                {
                    continue;
                }

                var deserialized = JsonSerializer.Deserialize<ProductEventResponse>(response.Message.Value);
                if (deserialized == null)
                {
                    throw new InvalidOperationException("Unable to deserialize response payload");
                }

                return deserialized;
            }

            throw new TimeoutException($"Timed out waiting for response on topic '{responseTopic}'");
        }
        catch (ProduceException<Null, string> e)
        {
            Console.WriteLine($"Delivery failed: {e.Error.Reason}");
            throw;
        }
        finally
        {
            responseConsumer.Close();
        }
    }
}