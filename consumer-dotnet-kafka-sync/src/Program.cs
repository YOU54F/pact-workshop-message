using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;

namespace Products
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var webHostTask = Task.Run(() => BuildWebHost(args).Run());
            var kafkaConsumerTask = Task.Run(() => new KafkaConsumer(
                "localhost:9092",
                "pactflow-example-consumer-dotnet-kafka",
                "products"
            ).Start());

            Task.WaitAll(webHostTask, kafkaConsumerTask);
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
