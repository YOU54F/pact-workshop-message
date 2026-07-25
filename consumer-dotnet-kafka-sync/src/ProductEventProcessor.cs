using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

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

        public ProductEvent BuildProductEvent(Product product, string eventType)
        {
            var normalizedEvent = NormalizeEventType(eventType);
            var productId = string.IsNullOrWhiteSpace(product.id) ? Guid.NewGuid().ToString("N") : product.id;

            var productEvent = new ProductEvent(
                productId,
                product.type,
                product.name,
                product.version,
                normalizedEvent);

            Console.WriteLine($"Request payload: {JsonSerializer.Serialize(productEvent)}");
            return productEvent;
        }

        public ProductEventResponse HandleProductEventResponse(ProductEvent requestEvent, ProductEventResponse response)
        {
            Console.WriteLine($"Received response payload: {JsonSerializer.Serialize(response)}");

            if (response == null)
            {
                throw new InvalidOperationException("Response payload was null");
            }

            if (!response.Processed)
            {
                throw new InvalidOperationException($"Provider failed to process product event '{requestEvent.Event}' for product '{requestEvent.id}'");
            }

            if (!string.Equals(response.Event, requestEvent.Event, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Response event type did not match request event type");
            }

            if (!string.Equals(response.Id, requestEvent.id, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Response product id did not match request product id");
            }

            ApplyToReadModel(requestEvent);

            return response;
        }

        private void ApplyToReadModel(ProductEvent productEvent)
        {
            Console.WriteLine($"Applying processed event '{productEvent.Event}' for product '{productEvent.id}' to read model");

            if (productEvent.Event == "CREATED" || productEvent.Event == "UPDATED")
            {
                _repository.RemoveProduct(productEvent.id);
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
        }

        private static string NormalizeEventType(string eventType)
        {
            if (string.IsNullOrWhiteSpace(eventType))
            {
                throw new InvalidOperationException("Event type must be provided");
            }

            var normalized = eventType.Trim().ToUpperInvariant();
            var allowed = new HashSet<string> { "CREATED", "UPDATED", "DELETED" };

            if (!allowed.Contains(normalized))
            {
                throw new InvalidOperationException("Unable to process event");
            }

            return normalized;
        }
    }
}