using System;
using System.Text.Json;

using System.Collections.Generic;
using PactNet;
using PactNet.Output.Xunit;
using PactNet.Infrastructure.Outputters;
using Xunit;
using Xunit.Abstractions;
using Match = PactNet.Matchers.Match;
using Products;

namespace Consumer.Tests
{
    public class ProductEventProcessorTests
    {
        private readonly ProductEventProcessor consumer;

        private readonly ISynchronousMessagePactBuilderV4 pact;

        public ProductEventProcessorTests(ITestOutputHelper output)
        {
            consumer = new ProductEventProcessor();

            var config = new PactConfig
            {
                PactDir = "../../../pacts/",
                Outputters = new List<IOutput>
                {
                    new XunitOutput(output),
                    new ConsoleOutput()
                },
            };

            this.pact = Pact.V4("pactflow-example-consumer-dotnet-kafka-sync", "pactflow-example-provider-dotnet-kafka-sync", config).WithSynchronousMessageInteractions();
        }

        [Fact]
        public void ProductEventHandlers_ProductUpdate_HandlesSendAndReply()
        {
            this.pact
                      .ExpectsToReceive("a product event update")
                    // TODO: Implement metadata for sync messaging
                    //   .WithMetadata("kafka_topic", "products")
                      .WithRequestJsonContent(new
                      {
                          id = Match.Type("1"),
                          type = Match.Type("Product Range"),
                          name = Match.Type("Some Product"),
                          version = Match.Type("v1"),
                          @event = Match.Regex("UPDATED", "^(CREATED|UPDATED|DELETED)$")
                      })
                    // TODO: Implement matchers for sync messaging response
                    // TODO: Ensure multiple responses are supported
                      .WithResponseJsonContent(new
                      {
                          id = "1",
                          @event = "UPDATED",
                          processed = true
                      })
                      .VerifyWithResponse<JsonElement, ProductEventProcessor.ProductEventResponse>(request =>
                      {
                          var requestProduct = new Product(
                              ReadMatcherAwareString(request, "id"),
                              ReadMatcherAwareString(request, "type"),
                              ReadMatcherAwareString(request, "name"),
                              ReadMatcherAwareString(request, "version"));

                          var productEvent = consumer.BuildProductEvent(
                              requestProduct,
                              ReadMatcherAwareString(request, "event"));

                          var providerResponse = new ProductEventProcessor.ProductEventResponse(
                              productEvent.id,
                              productEvent.Event,
                              true);

                          return consumer.HandleProductEventResponse(productEvent, providerResponse);
                      });
        }

// TODO: provide a reify method for messaging users if not already existing in pact-net
        private static string ReadMatcherAwareString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return string.Empty;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                return property.GetString() ?? string.Empty;
            }

            if (property.ValueKind == JsonValueKind.Object && property.TryGetProperty("value", out var valueProperty))
            {
                return valueProperty.GetString() ?? string.Empty;
            }

            throw new InvalidOperationException($"Property '{propertyName}' was not a string or matcher value.");
        }
    }
}