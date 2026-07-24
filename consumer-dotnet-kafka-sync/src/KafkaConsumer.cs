using Confluent.Kafka;
using System;
using System.Threading;
using static Products.ProductEventProcessor;

namespace Products
{
    public class KafkaConsumer
    {
        private readonly string _bootstrapServers;
        private readonly string _groupId;
        private readonly string _topic;

        public KafkaConsumer(string bootstrapServers, string groupId, string topic)
        {
            _bootstrapServers = bootstrapServers;
            _groupId = groupId;
            _topic = topic;
        }

        public void Start()
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true; // prevent the process from terminating.
                cts.Cancel();
            };

            using (var consumer = new ConsumerBuilder<string, string>(config).Build())
            {
                consumer.Subscribe(_topic);
                var processor = new ProductEventProcessor();
                try
                {
                    while (true)
                    {
                        var cr = consumer.Consume(cts.Token);
                        Console.WriteLine($"Consumed event from topic {_topic}: key = {cr.Message.Key,-10} value = {cr.Message.Value}");
                        var productEvent = System.Text.Json.JsonSerializer.Deserialize<ProductEvent>(cr.Message.Value);
                        processor.ProductEventHandler(productEvent);

                    }
                }
                catch (OperationCanceledException)
                {
                    // Ctrl-C was pressed.
                }
                finally
                {
                    consumer.Close();
                }
            }
        }
    }
}
