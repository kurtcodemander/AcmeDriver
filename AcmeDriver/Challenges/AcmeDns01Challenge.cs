using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using AcmeDriver.Utils;

namespace AcmeDriver {

	[JsonSourceGenerationOptions(
		WriteIndented = true,
		PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(GoogleDnsApiResponse))]
	public partial class GoogleDnsApiResponseSourceGenerationContext : JsonSerializerContext {
	}

	public class GoogleDnsApiResponse {

		[JsonPropertyName("Answer")]
		public IList<GoogleDnsApiResponseAnswer> Answers { get; } = new List<GoogleDnsApiResponseAnswer>();
	}

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(GoogleDnsApiResponseAnswer))]
	public partial class GoogleDnsApiResponseAnswerSourceGenerationContext : JsonSerializerContext {
	}

	public class GoogleDnsApiResponseAnswer {

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("type")]
		public AcmeDns01Challenge.GoogleDnsRecordType Type { get; set; }

		[JsonPropertyName("data")]
		public string Data { get; set; }

	}

	public class AcmeDns01Challenge : AcmeChallenge {

        public string DnsRecord => "_acme-challenge";

        public string DnsAddress => $"{DnsRecord}.{Domain}";

        public string DnsRecordContent { get; }

        public string NslookupCmd => $"nslookup -type=TXT {DnsAddress}";

        public string GoogleApiUrl => $"https://dns.google.com/resolve?name={DnsAddress}&type=TXT";

        public string GoogleUiApiUrl => $"https://dns.google.com/query?name={DnsAddress}&type=TXT";

        public AcmeDns01Challenge(AcmeChallengeData data, AcmeAuthorization authorization, AcmeClientRegistration registration) : base(data, authorization, registration) {
            var keyAuthorization = data.GetKeyAuthorization(registration);
			using (var sha256 = SHA256.Create()) {
                DnsRecordContent = Base64Url.Encode(sha256.ComputeHash(Encoding.UTF8.GetBytes(keyAuthorization)));
			}	
        }

        public override async Task<bool> PrevalidateAsync() {
            try {
                using (var client = new HttpClient()) {
                    var responseContent = await client.GetStringAsync(GoogleApiUrl).ConfigureAwait(false);
                    var res = AcmeJson.Deserialize<GoogleDnsApiResponse>(responseContent, GoogleDnsApiResponseSourceGenerationContext.Default.GoogleDnsApiResponse);
                    return res.Answers.Any(a => a.Type == GoogleDnsRecordType.TXT && (a.Data == DnsRecordContent || a.Data == $"\"{DnsRecordContent}\""));
                }
            } catch {
                return false;
            }
        }
		
        public enum GoogleDnsRecordType {
            TXT = 16
        }

    }
}
