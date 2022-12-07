﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AcmeDriver.JWK;
using AcmeDriver.Utils;

namespace AcmeDriver {

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(AcmeRegistrationData))]
	public partial class AcmeRegistrationDataSourceGenerationContext : JsonSerializerContext {
	}

	public class AcmeRegistrationData {
		
		[JsonPropertyName("contacts")]
		public string[]? Contact { get; set; }

		[JsonPropertyName("termsOfServiceAgreed")]
		public bool TermsOfServiceAgreed { get; set; } = true; 
	}
	
	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(GetRegistrationData))]
	public partial class GetRegistrationDataSourceGenerationContext : JsonSerializerContext {
	}

	public class GetRegistrationData {
		
		[JsonPropertyName("onlyReturnExisting")]
		public bool OnlyReturnExisting { get; set; }
	}

	public class AcmeClient : IAcmeClient {

		private readonly AcmeClientContext _context;
		public AcmeDirectory Directory { get; }

		public static readonly Uri LETS_ENCRYPT_STAGING_URL = new Uri("https://acme-staging-v02.api.letsencrypt.org");
		public static readonly Uri LETS_ENCRYPT_PRODUCTION_URL = new Uri("https://acme-v02.api.letsencrypt.org");

		public AcmeClient(Uri baseUrl) : this(AcmeDirectory.FromBaseUrl(baseUrl)) {
		}

		public AcmeClient(AcmeDirectory directory) {
			Directory = directory;
			_context = new AcmeClientContext(directory);
		}

		public async Task<IAcmeAuthenticatedClient> AuthenticateAsync(PrivateJsonWebKey key, Uri? location = null) {
			var registration = await GetRegistrationAsync(key, location);
			return new AcmeAuthenticatedClient(new AcmeAuthenticatedClientContext(_context, registration));
		}

		public async Task<IAcmeAuthenticatedClient> RegisterAsync(string[] contacts, PrivateJsonWebKey? key = null) {
			key = key ?? GenerateKey();
			var reg = new AcmeClientRegistration(key, new Uri("https://acmedriver.com"));
			var data = await _context.SendPostAsync<AcmeRegistrationData, AcmeRegistration>(Directory.NewAccountUrl, new AcmeRegistrationData() {
				Contact = contacts,
				TermsOfServiceAgreed = true			
			}, 
			AcmeRegistrationDataSourceGenerationContext.Default.AcmeRegistrationData,
			AcmeRegistrationSourceGenerationContext.Default.AcmeRegistration,
			reg).ConfigureAwait(false);
			reg = new AcmeClientRegistration(key, data.Location);
			return new AcmeAuthenticatedClient(new AcmeAuthenticatedClientContext(_context, reg));
		}

		public async Task<AcmeDirectory> GetDirectoryAsync() {
			var res = await _context.SendGetAsync<AcmeDirectory>(Directory.DirectoryUrl, AcmeDirectorySourceGenerationContext.Default.AcmeDirectory);
			res.DirectoryUrl ??= Directory.DirectoryUrl;
			return res;
		}

		public static async Task<IAcmeClient> CreateAcmeClientAsync(Uri baseUrl) {
			using var client = new HttpClient();
			var directoryUrl = new Uri(new Uri($"{baseUrl}"), new Uri("/directory", UriKind.Relative));
			var request = new HttpRequestMessage(HttpMethod.Get, directoryUrl);
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			var response = await client.SendAsync(request).ConfigureAwait(false);
			var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			var res = AcmeJson.Deserialize<AcmeDirectory>(responseContent, AcmeDirectorySourceGenerationContext.Default.AcmeDirectory);
			res.DirectoryUrl ??= directoryUrl;
			var acmeClient = new AcmeClient(res);
			return acmeClient;
		}

		public void Dispose() {
			_context.Dispose();
		}

		private PrivateJsonWebKey GenerateKey() {
			return RsaPrivateJwk.Create();
		}

		private async Task<AcmeClientRegistration> GetRegistrationAsync(PrivateJsonWebKey key, Uri? location = null) {
			if (location != null) {
				return new AcmeClientRegistration(key, location);
			}
			var reg = new AcmeClientRegistration(key, new Uri("https://acmedriver.com"));
			var data = await _context.SendPostAsync<GetRegistrationData, AcmeRegistration>(Directory.NewAccountUrl, new GetRegistrationData () {
				OnlyReturnExisting = true
			}, 
			GetRegistrationDataSourceGenerationContext.Default.GetRegistrationData,
			AcmeRegistrationSourceGenerationContext.Default.AcmeRegistration,
			reg).ConfigureAwait(false);
			return new AcmeClientRegistration(key, data.Location);
		}

	}
}
