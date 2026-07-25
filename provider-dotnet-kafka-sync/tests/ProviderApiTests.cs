using System;
using System.IO;
using System.Collections.Generic;
using PactNet.Infrastructure.Outputters;
using PactNet.Output.Xunit;
using PactNet.Verifier;
using PactNet;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;
using Products;

namespace tests;

public class ProviderApiTests : IDisposable
{
    private ITestOutputHelper _outputHelper { get; }
    private string pactUrl;
    private readonly PactVerifier verifier;

    public void Dispose()
    {
        // make sure you dispose the verifier to stop the internal messaging server
        GC.SuppressFinalize(this);
        this.verifier.Dispose();
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ProviderApiTests(ITestOutputHelper output)
    {
        _outputHelper = output;
                var config = new PactVerifierConfig
        {

            // NOTE: We default to using a ConsoleOutput,
            // however xUnit 2 does not capture the console output,
            // so a custom outputter is required.
            Outputters = new List<IOutput>
                            {
                                new XunitOutput(_outputHelper),
                                new ConsoleOutput()
                            },

            // Output verbose verification logs to the test output
            LogLevel = PactLogLevel.Debug,
        };

        string providerName = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PACT_PROVIDER_NAME"))
                                ? Environment.GetEnvironmentVariable("PACT_PROVIDER_NAME")
                                : "pactflow-example-provider-dotnet-kafka-sync";
        verifier = new PactVerifier(providerName, config);
        pactUrl = Environment.GetEnvironmentVariable("PACT_URL") ?? Path.Combine(AppContext.BaseDirectory, "..","..","..","..","..","consumer-dotnet-kafka-sync","tests","pacts","pactflow-example-consumer-dotnet-kafka-sync-pactflow-example-provider-dotnet-kafka-sync.json");

    }

    [Fact]
    public void EnsureProviderApiHonoursPactWithConsumer()
    {
        // Arrange
        verifier
        .WithHttpEndpoint(new Uri("http://localhost:49152"))
        .WithMessages(scenarios =>
                {
                    scenarios.Add("a product event update", () => new ProductEventResponse("1", "UPDATED", true));

                    // scenarios
                    
                    // .Add("a product event update", builder =>
                    //      {
                    //          builder
                    //          .WithMetadata(new
                    //                 {
                    //                     ContentType = "application/json",
                    //                     kafka_topic = "products"
                    //                 })
                    //         .WithContent(() => new ProductEventProducer().CreateEvent(new Product("1", "food", "pancake", "1.0.0"), "UPDATED"));
                    //      });
                }, Options)
        .WithFileSource(new FileInfo(pactUrl))
        // Act
        .Verify();

    }
}
