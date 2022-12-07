using AcmeDriver.Utils;
using System;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace AcmeDriver.JWK {
	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(EccPublicJwk))]
	public partial class EccPublicJwkSourceGenerationContext : JsonSerializerContext {
	}

	public class EccPublicJwk : PublicJsonWebKey {

        [JsonPropertyName("crv")]
        public string? Curve { get; set; }

        [JsonPropertyName("x")]
        public string? X { get; set; }

        [JsonPropertyName("y")]
        public string? Y { get; set; }

        [JsonPropertyName("kty")]
        public override string Kty => "EC";

        protected override string GetJwkThumbprintJson() {
			var crv = AcmeJson.SerializeSimple(Curve);
			var kty = AcmeJson.SerializeSimple(Kty);
			var x = AcmeJson.SerializeSimple(X);
			var y = AcmeJson.SerializeSimple(Y);
			return $"{{\"crv\":{crv},\"kty\":{kty},\"x\":{x},\"y\":{y}}}";
			return ""; 
        }

        public static EccPublicJwk From(ECParameters publicKey) {
            if (publicKey.Q.X == null) {
                throw new ArgumentNullException(nameof(publicKey.Q.X));
            }
            if (publicKey.Q.Y == null) {
                throw new ArgumentNullException(nameof(publicKey.Q.Y));
            }
            return new EccPublicJwk {
                Curve = ECUtils.GetFipsCurveName(publicKey.Curve),
                X = Base64Url.Encode(publicKey.Q.X),
                Y = Base64Url.Encode(publicKey.Q.Y)
            };
        }

    }
}
