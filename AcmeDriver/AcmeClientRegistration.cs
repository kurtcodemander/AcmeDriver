using System;
using System.Text;
using System.Text.Json.Serialization;
using AcmeDriver.JWK;
using AcmeDriver.Utils;

namespace AcmeDriver {

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(ProtectedHeader))]
	public partial class ProtectedHeaderSourceGenerationContext : JsonSerializerContext {
	}

	public class ProtectedHeader {
		[JsonPropertyName("nonce")]
		public string? Nonce { get; set; }

		[JsonPropertyName("url")]
		public string? Url { get; set; }

		[JsonPropertyName("alg")]
		public string? Alg { get; set; }

		[JsonPropertyName("kid")]
		public string? Kid { get; set; }

		[JsonPropertyName("jwk")]
		//public PublicJsonWebKey? Jwk { get; set; }
		//public RsaPublicJwk? Jwk { get; set; }
		public EccPublicJwk? Jwk { get; set; }
	}

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(SignKidPayload))]
	public partial class SignKidPayloadSourceGenerationContext : JsonSerializerContext {
	}

	public class SignKidPayload {
		[JsonPropertyName("payload")]
		public string? Payload { get; set; }

		[JsonPropertyName("protected")]
		public string? Protected { get; set; }

		[JsonPropertyName("signature")]
		public string? Signature { get; set; }		
	}

	public class AcmeClientRegistration {

		public PrivateJsonWebKey Key { get; }

		public Uri Location { get; }

		public AcmeClientRegistration(PrivateJsonWebKey key, Uri location) {
			Key = key;
			Location = location;
		}

		public string GetJwkThumbprint() {
			return Key.GetPublicJwk().GetJwkThumbprint();
		}

		public string SignKid(Uri url, string nonce, byte[] payload) {
			//var protectedHeader = new {
			//	nonce = nonce,
			//	url = url.ToString(),
			//	alg = GetSignatureAlg(),
			//	kid = Location.ToString()
			//};

			var protectedHeader = new ProtectedHeader () {
				Nonce = nonce,
				Url = url.ToString(),
				Alg = GetSignatureAlg(),
				Kid = Location.ToString()
			};

			var protectedHeaderJson = AcmeJson.Serialize(protectedHeader, ProtectedHeaderSourceGenerationContext.Default.ProtectedHeader);
			var protectedHeaderData = Encoding.UTF8.GetBytes(protectedHeaderJson);
			var protectedHeaderEncoded = Base64Url.Encode(protectedHeaderData);

			var payloadEncoded = Base64Url.Encode(payload);

			var tbs = protectedHeaderEncoded + "." + payloadEncoded;

			//var json = new SignKidPayload() {
			//	Payload = payloadEncoded,
			//	@protected = protectedHeaderEncoded,
			//	signature = ComputeSignature(Encoding.UTF8.GetBytes(tbs))
			//};

			var json = new SignKidPayload() {
				Payload = payloadEncoded,
				Protected = protectedHeaderEncoded,
				Signature = ComputeSignature(Encoding.UTF8.GetBytes(tbs))
			};
			return AcmeJson.Serialize(json, SignKidPayloadSourceGenerationContext.Default.SignKidPayload);
		}

		public string Sign(Uri url, string nonce, byte[] payload) {			
			var protectedHeader = new ProtectedHeader() {
				Nonce = nonce,
				Url = url.ToString(),
				Alg = GetSignatureAlg(),
				Jwk = (EccPublicJwk) Key.GetPublicJwk()
				//Jwk = (RsaPublicJwk)Key.GetPublicJwk()	
			};

			var protectedHeaderJson = AcmeJson.Serialize(protectedHeader, ProtectedHeaderSourceGenerationContext.Default.ProtectedHeader);
			var protectedHeaderData = Encoding.UTF8.GetBytes(protectedHeaderJson);
			var protectedHeaderEncoded = Base64Url.Encode(protectedHeaderData);

			var payloadEncoded = Base64Url.Encode(payload);

			var tbs = protectedHeaderEncoded + "." + payloadEncoded;

			var json = new SignKidPayload() {
				Payload = payloadEncoded,
				Protected = protectedHeaderEncoded,
				Signature = ComputeSignature(Encoding.UTF8.GetBytes(tbs))
			};
			return AcmeJson.Serialize(json, SignKidPayloadSourceGenerationContext.Default.SignKidPayload);
		}

		private string ComputeSignature(byte[] data) {
			var signature = Key.SignData(data);
			return Base64Url.Encode(signature);
		}

		private string GetSignatureAlg() {
			return Key.SignatureAlgorithmName;
		}

	}
}
