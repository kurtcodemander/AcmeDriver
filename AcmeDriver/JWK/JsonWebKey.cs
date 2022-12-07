using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace AcmeDriver.JWK {
	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(JsonWebKey))]
	public partial class JsonWebKeySourceGenerationContext : JsonSerializerContext {
	}

	//public abstract class JsonWebKey {

 //       protected static readonly SHA256 _sha256 = SHA256.Create();

 //       [JsonPropertyName("kty")]
 //       public abstract string Kty { get; }

 //   }

	public class JsonWebKey {

		protected static readonly SHA256 _sha256 = SHA256.Create();

		[JsonPropertyName("kty")]
		public virtual string Kty { get; }

	}
}
