using System.Text.Json.Serialization;

namespace AcmeDriver {

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(AcmeOrderRequirement))]
	public partial class AcmeOrderRequirementSourceGenerationContext : JsonSerializerContext {
	}

	public class AcmeOrderRequirement {

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("status")]
        public AcmeOrderRequirementStatus Status { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

    }
}
