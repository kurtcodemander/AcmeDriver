using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AcmeDriver.Utils {
    public static class AcmeJson {

        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions {
            Converters = {
                new AcmeOrderStatusConverter(),
                new AcmeAuthorizationStatusConverter()
            }, 			
        };

		//var context = new MyJsonContext(options);

		//var context = new AcmeOrderSourceGenerationContext(options);

		public static T Deserialize<T>(string content, JsonTypeInfo<T> jsonTypeInfo) {
			//todo: , new PrivateJwkConverter()

			//AcmeOrderSourceGenerationContext.Default.op

			//return JsonSerializer.Deserialize<T>(content, _options, jsonTypeInfo);
			return JsonSerializer.Deserialize<T>(content, jsonTypeInfo);
		}

        public static string Serialize<T>(T obj, JsonTypeInfo<T> jsonTypeInfo) {
            //return JsonSerializer.Serialize(obj, typeof(object), _options);
			return JsonSerializer.Serialize(obj, jsonTypeInfo);
		}

		public static string SerializeSimple<T>(T obj) {
			//return JsonSerializer.Serialize(obj, typeof(object), _options);
			return JsonSerializer.Serialize(obj);
		}
	}
}
