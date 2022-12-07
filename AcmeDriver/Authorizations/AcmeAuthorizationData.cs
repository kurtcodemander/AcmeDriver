using System;
using System.Text.Json.Serialization;
using AcmeDriver.Utils;

namespace AcmeDriver {

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(AcmeAuthorizationData))]
	public partial class AcmeAuthorizationDataSourceGenerationContext : JsonSerializerContext {
	}

	public class AcmeAuthorizationData : AcmeResource {

        [JsonPropertyName("identifier")]
        public AcmeIdentifier Identifier { get; set; }

        [JsonPropertyName("status")]
		[JsonConverter(typeof(AcmeAuthorizationStatusConverter))]
		public AcmeAuthorizationStatus Status { get; set; }

        [JsonPropertyName("expires")]
        public DateTimeOffset Expires { get; set; }

        [JsonPropertyName("challenges")]
        public AcmeChallengeData[] Challenges { get; set; }

        [JsonPropertyName("wildcard")]
        public bool Wildcard { get; set; }

    }
}
