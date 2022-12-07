using System.Text.Json.Serialization;

namespace AcmeDriver {
	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(AcmeExceptionInfo))]
	public partial class AcmeExceptionInfoSourceGenerationContext : JsonSerializerContext {
	}

	public class AcmeExceptionInfo {

        [JsonPropertyName("type")]
        public string? Type { get; set; }


        [JsonPropertyName("detail")]
        public string? Detail { get; set; }


		[JsonPropertyName("status")]
		public int Status { get; set; } = 0; 

    }

}
