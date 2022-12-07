using System.Text;
using System.Text.Json.Serialization;

namespace AcmeDriver.JWK {

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(PublicJsonWebKey))]
	public partial class PublicJsonWebKeySourceGenerationContext : JsonSerializerContext {
	}

	/*public abstract class PublicJsonWebKey : JsonWebKey {

        //https://tools.ietf.org/html/rfc7638
        public string GetJwkThumbprint() {
            var str = GetJwkThumbprintJson();
            var hash = _sha256.ComputeHash(Encoding.UTF8.GetBytes(str));
            return Base64Url.Encode(hash);
        }

        protected abstract string GetJwkThumbprintJson();

    } */

	public class PublicJsonWebKey : JsonWebKey {

		//https://tools.ietf.org/html/rfc7638
		public string GetJwkThumbprint() {
			var str = GetJwkThumbprintJson();
			var hash = _sha256.ComputeHash(Encoding.UTF8.GetBytes(str));
			return Base64Url.Encode(hash);
		}

		protected virtual string GetJwkThumbprintJson() {
			return string.Empty; 
		}

	}
}
