using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace AcmeDriver.JWK {
	/*public abstract class PrivateJsonWebKey : JsonWebKey {

        public abstract PublicJsonWebKey GetPublicJwk();

        public abstract byte[] SignData(byte[] data);

        public abstract string SignatureAlgorithmName { get; }

        public abstract AsymmetricAlgorithm CreateAsymmetricAlgorithm();
        
    } */


	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(PrivateJsonWebKey))]
	public partial class PrivateJsonWebKeySourceGenerationContext : JsonSerializerContext {
	}

	public class PrivateJsonWebKey : JsonWebKey {

		public virtual PublicJsonWebKey GetPublicJwk() {
			return null; 
		}

		public virtual byte[] SignData(byte[] data) {
			return null; 
		}

		public virtual string SignatureAlgorithmName { get; }


		public virtual AsymmetricAlgorithm CreateAsymmetricAlgorithm() {
			return null; 
		}

	}
}
