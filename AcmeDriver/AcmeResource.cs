using System;
using System.Text.Json.Serialization;

namespace AcmeDriver {
	
	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(AcmeResource))]
	public partial class AcmeResourceSourceGenerationContext : JsonSerializerContext {
	}

	//public abstract class AcmeResource {
	public class AcmeResource {

		[JsonIgnore]
        public Uri Location { get; set; }

    }
}
