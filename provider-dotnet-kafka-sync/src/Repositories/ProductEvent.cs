using System.Text.Json.Serialization;

namespace Products;

public class ProductEvent(string id, string type, string name, string version, string @event) : Product(id, type, name, version)
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = @event;
}