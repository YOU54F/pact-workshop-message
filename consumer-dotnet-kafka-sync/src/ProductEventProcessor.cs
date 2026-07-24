using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Products
{
    public class ProductEventProcessor
    {
        private readonly ProductRepository _repository;

        public ProductEventProcessor()
        {
            _repository = ProductRepository.GetInstance();
        }

        public class ProductEvent(string id, string type, string name, string version, string @event) : Product(id, type, name, version)
        {
            [JsonPropertyName("event")]
            public string Event { get; set; } = @event;
        }

        public class ProductEventResponse(string id, string @event, bool processed)
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = id;

            [JsonPropertyName("event")]
            public string Event { get; set; } = @event;

            [JsonPropertyName("processed")]
            public bool Processed { get; set; } = processed;
        }

        public ProductEventResponse ProductEventHandler(ProductEvent productEvent)
        {
            Console.WriteLine($"Received product: {productEvent}");
            Console.WriteLine($"Received product event: {productEvent.Event}");
            Console.WriteLine($"Received product id: {productEvent.id}");

            if (productEvent.Event == "CREATED" || productEvent.Event == "UPDATED")
            {
                _repository.AddProduct(new Product(productEvent.id,productEvent.type,productEvent.name,productEvent.version));
            }
            else if (productEvent.Event == "DELETED")
            {
                _repository.RemoveProduct(productEvent.id);
            }
            else
            {
                throw new InvalidOperationException("Unable to process event");
            }

            var response = new ProductEventResponse(
                productEvent.id,
                productEvent.Event,
                true);

            Console.WriteLine($"Response payload: {JsonSerializer.Serialize(response)}");

            return response;
        }
    }
}