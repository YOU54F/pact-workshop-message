using Confluent.Kafka;
using System;
using System.Diagnostics;
using System.Text.Json;
using static Products.ProductEventProcessor;

namespace Products
{
    public class KafkaRequestReplyClient
    {
        private readonly string _bootstrapServers;
        private readonly string _groupId;
        private readonly string _requestTopic;
        private readonly string _responseTopic;
        private readonly ProductEventProcessor _processor;

        public KafkaRequestReplyClient(
            string bootstrapServers = "localhost:9092",
            string groupId = "pactflow-example-consumer-dotnet-kafka-sync-client",
            string requestTopic = "products.requests",
            string responseTopic = "products.responses")
        {
            _bootstrapServers = bootstrapServers;
            _groupId = groupId;
            _requestTopic = requestTopic;
            _responseTopic = responseTopic;
            _processor = new ProductEventProcessor();
        }

        public ProductEventResponse SendProductEventAndWaitForResponse(Product product, string eventType, int timeoutMs = 10000)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            var requestEvent = _processor.BuildProductEvent(product, eventType);

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers
            };

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = false
            };

            using (var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build())
            using (var producer = new ProducerBuilder<string, string>(producerConfig).Build())
            {
                consumer.Subscribe(_responseTopic);

                try
                {
                    var requestMessage = new Message<string, string>
                    {
                        Key = correlationId,
                        Value = JsonSerializer.Serialize(requestEvent)
                    };

                    var deliveryResult = producer.ProduceAsync(_requestTopic, requestMessage).GetAwaiter().GetResult();
                    producer.Flush(TimeSpan.FromSeconds(5));
                    Console.WriteLine($"Delivered '{deliveryResult.Value}' to '{deliveryResult.TopicPartitionOffset}'");

                    var sw = Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds < timeoutMs)
                    {
                        var response = consumer.Consume(TimeSpan.FromMilliseconds(250));
                        if (response?.Message == null || response.Message.Key != correlationId)
                        {
                            continue;
                        }

                        var deserialized = JsonSerializer.Deserialize<ProductEventResponse>(response.Message.Value);
                        if (deserialized == null)
                        {
                            throw new InvalidOperationException("Unable to deserialize product response payload");
                        }

                        return _processor.HandleProductEventResponse(requestEvent, deserialized);
                    }

                    throw new TimeoutException($"Timed out waiting for response on topic '{_responseTopic}'");
                }
                catch (ProduceException<string, string> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                    throw;
                }
                finally
                {
                    consumer.Close();
                }
            }
        }
    }
}
