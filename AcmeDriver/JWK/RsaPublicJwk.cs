using AcmeDriver.Utils;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace AcmeDriver.JWK {

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(RsaPublicJwk))]
	public partial class RsaPublicJwkSourceGenerationContext : JsonSerializerContext {
	}

	public class RsaPublicJwk : PublicJsonWebKey {

        [JsonPropertyName("n")]
        public string Modulus { get; set; }

        [JsonPropertyName("e")]
        public string Exponent { get; set; }

        [JsonPropertyName("kty")]
        public override string Kty => "RSA";

        protected override string GetJwkThumbprintJson() {
            var n = AcmeJson.SerializeSimple(Modulus);
            var e = AcmeJson.SerializeSimple(Exponent);
            var kty = AcmeJson.SerializeSimple(Kty);
            return $"{{\"e\":{e},\"kty\":{kty},\"n\":{n}}}";
        }

        public static RsaPublicJwk From(RSAParameters parameters) {
            return new RsaPublicJwk {
                Modulus = Base64Url.Encode(parameters.Modulus),
                Exponent = Base64Url.Encode(parameters.Exponent)
            };
        }

    }
}
