using System.Text.Json.Serialization;

namespace AcmeDriver {

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(AcmeRegistrationKey))]
	public partial class AcmeRegistrationKeySourceGenerationContext : JsonSerializerContext {
	}

	public class AcmeRegistrationKey {

        [JsonPropertyName("kty")]
        public string? Type { get; set; }

        [JsonPropertyName("n")]
        public string? Modulus { get; set; }

        [JsonPropertyName("e")]
        public string? Exponent { get; set; }

    }
}
