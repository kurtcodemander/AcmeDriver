﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AcmeDriver.Handlers;
using AcmeDriver.JWK;
using AcmeDriver.Utils;

namespace AcmeDriver {
	public class AcmeClient : IDisposable {

		private readonly HttpClient _client;
		private readonly AcmeDirectory _directory;

		public AcmeClientRegistration Registration { get; set; }

		public string Nonce { get; set; }

		public static readonly Uri LETS_ENCRYPT_STAGING_URL = new Uri("https://acme-staging-v02.api.letsencrypt.org");
		public static readonly Uri LETS_ENCRYPT_PRODUCTION_URL = new Uri("https://acme-v02.api.letsencrypt.org");

		public AcmeClient(Uri baseUrl) : this(AcmeDirectory.FromBaseUrl(baseUrl)) {
		}

		public AcmeClient(AcmeDirectory directory) {
			_directory = directory;
			_client = new HttpClient(new AcmeExceptionHandler {
				InnerHandler = new AcmeNonceHandler(this) {
					InnerHandler = new HttpClientHandler {
					}
				}
			});
		}

		public static async Task<AcmeClient> CreateAcmeClient(string baseUrl) {
			using var client = new HttpClient();
			var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/directory");
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			var response = await client.SendAsync(request).ConfigureAwait(false);
			var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			var res = AcmeJson.Deserialize<AcmeDirectory>(responseContent);
			var acmeClient = new AcmeClient(res);
			return acmeClient;
		}

		public Task<AcmeDirectory> GetDirectoryAsync() {
			return GetDirectoryAsync(_directory.DirectoryUrl);
		}

		public Task<AcmeDirectory> GetDirectoryAsync(Uri directoryUrl) {
			return SendGetAsync<AcmeDirectory>(directoryUrl);
		}

		#region Registrations

		private PrivateJsonWebKey GenerateKey() {
			return RsaPrivateJwk.Create();
		}

		public async Task<AcmeRegistration> NewRegistrationAsync(string[] contacts, PrivateJsonWebKey? key = null) {
			key = key ?? GenerateKey();
			await EnsureNonceAsync().ConfigureAwait(false);
			var reg = new AcmeClientRegistration {
				Key = key
			};
			Registration = reg;
			var data = await SendPostAsync<object, AcmeRegistration>(_directory.NewAccountUrl, new {
				contact = contacts,
				termsOfServiceAgreed = true
			}).ConfigureAwait(false);
			reg.Id = data.Id;
			reg.Location = data.Location;
			return data;
		}

		public async Task<AcmeRegistration> GetRegistrationAsync(Uri registrationUri) {
			await EnsureNonceAsync().ConfigureAwait(false);
			var data = await SendPostKidAsync<object, AcmeRegistration>(registrationUri, new { }).ConfigureAwait(false);
			return data;
		}

		public Task UpdateRegistrationAsync(Uri registrationUri) {
			return Task.CompletedTask;
		}

		public Task<AcmeRegistration> AcceptRegistrationAgreementAsync(Uri agreementUrl) {
			//$"/acme/reg/{Registration.Id}"
			return SendPostKidAsync<object, AcmeRegistration>(Registration.Location, new {
				resource = "reg",
				agreement = agreementUrl
			});
		}

		#endregion

		#region Authorizations

		public async Task<AcmeAuthorization> NewAuthorizationAsync(AcmeIdentifier identifier) {
			await EnsureNonceAsync().ConfigureAwait(false);
			return await SendPostAsync<object, AcmeAuthorization>(_directory.NewAuthzUrl, new {
				resource = "new-authz",
				identifier = new {
					type = identifier.Type,
					value = identifier.Value
				}
			}).ConfigureAwait(false);
		}

		public Task<AcmeAuthorization> NewAuthorizationAsync(string domainName) {
			return NewAuthorizationAsync(new AcmeIdentifier {
				Type = "dns",
				Value = domainName
			});
		}

		public async Task<AcmeAuthorization> GetAuthorizationAsync(Uri location) {
			var data = await SendPostAsGetAsync<AcmeAuthorization>(location).ConfigureAwait(false);
			data.Location = location;
			return data;
		}

		///<summary>
		///<para>Deactivates authorization.</para>
		///<para>Introduced in https://tools.ietf.org/html/draft-ietf-acme-acme-03</para>
		///</summary>
		public Task DeactivateAuthorizationAsync(Uri authorizationUri) {
			return SendPostAsync(authorizationUri, new {
				status = AcmeAuthorizationStatus.Deactivated.ToString().ToLower()
			});
		}

		///<summary>
		///<para>Deletes authorization.</para>
		///<para>Introduced in https://tools.ietf.org/html/draft-ietf-acme-acme-02</para>
		///<para>Removed in https://tools.ietf.org/html/draft-ietf-acme-acme-03. Use <see cref="M:DeactivateAuthorizationAsync" /></para>
		///</summary>
		public Task DeleteAuthorizationAsync(Uri authorizationUri) {
			return SendPostAsync(authorizationUri, new {
				resource = "authz",
				delete = true
			});
		}

		#endregion

		#region Orders

		public Task<AcmeOrder> GetOrderAsync(Uri location) {
			return SendPostAsGetAsync<AcmeOrder>(location);
		}

		public async Task<AcmeOrder> NewOrderAsync(AcmeOrder order) {
			await NewNonceAsync().ConfigureAwait(false);
			return await SendPostKidAsync<object, AcmeOrder>(_directory.NewOrderUrl, new {
				identifiers = order.Identifiers,
			}, (headers, ord) => {
				ord.Location = headers.Location;
			}).ConfigureAwait(false);
		}

		public async Task<AcmeOrder> FinalizeOrderAsync(AcmeOrder order, string csr) {
			await NewNonceAsync().ConfigureAwait(false);
			return await SendPostKidAsync<object, AcmeOrder>(order.Finalize, new {
				csr = Base64Url.Encode(csr.GetPemCsrData())
			}, (headers, ord) => {
				ord.Location = headers.Location;
			}).ConfigureAwait(false);
		}

		public Task<byte[]> DownloadCertificateAsync(Uri uri) {
			return SendPostAsGetBytesAsync(uri);
		}

		public Task<string> DownloadCertificateAsync(AcmeOrder order) {
			return SendPostAsGetStringAsync(order.Certificate);
		}

		#endregion

		#region Challenges

		public Task<AcmeChallengeData> CompleteChallengeAsync(AcmeChallenge challenge) {
			return CompleteChallengeAsync(challenge.Data);
		}

		public async Task<AcmeChallengeData> CompleteChallengeAsync(AcmeChallengeData challenge) {
			await NewNonceAsync().ConfigureAwait(false);
			var data = await SendPostKidAsync<object, AcmeChallengeData>(new Uri(challenge.Uri), new {
				type = challenge.Type,
				keyAuthorization = challenge.GetKeyAuthorization(Registration)
			}, null).ConfigureAwait(false);
			return data;
		}

		#endregion

		public async Task NewNonceAsync() {
			if (_directory.NewNonceUrl != null) {
				await SendHeadAsync(_directory.NewNonceUrl).ConfigureAwait(false);
			} else {
				await GetDirectoryAsync().ConfigureAwait(false);
			}
		}

		private string ComputeSignature(byte[] data) {
			if (Registration == null) {
				throw new Exception("registration is not set");
			}
			var signature = Registration.Key.SignData(data);
			return Base64Url.Encode(signature);
		}

		private string Sign(Uri url, byte[] payload) {
			if (Registration == null) {
				throw new Exception("registration is not set");
			}
			var protectedHeader = new {
				nonce = Nonce,
				url = url.ToString(),
				alg = GetSignatureAlg(),
				jwk = Registration.Key.GetPublicJwk()
			};
			var protectedHeaderJson = AcmeJson.Serialize(protectedHeader);
			var protectedHeaderData = Encoding.UTF8.GetBytes(protectedHeaderJson);
			var protectedHeaderEncoded = Base64Url.Encode(protectedHeaderData);

			var payloadEncoded = Base64Url.Encode(payload);

			var tbs = protectedHeaderEncoded + "." + payloadEncoded;

			var json = new {
				payload = payloadEncoded,
				@protected = protectedHeaderEncoded,
				signature = ComputeSignature(Encoding.UTF8.GetBytes(tbs))
			};
			return AcmeJson.Serialize(json);
		}

		private string SignKid(Uri url, byte[] payload) {
			if (Registration == null) {
				throw new Exception("registration is not set");
			}
			var protectedHeader = new {
				nonce = Nonce,
				url = url.ToString(),
				alg = GetSignatureAlg(),
				kid = Registration.Location.ToString()
			};
			var protectedHeaderJson = AcmeJson.Serialize(protectedHeader);
			var protectedHeaderData = Encoding.UTF8.GetBytes(protectedHeaderJson);
			var protectedHeaderEncoded = Base64Url.Encode(protectedHeaderData);

			var payloadEncoded = Base64Url.Encode(payload);

			var tbs = protectedHeaderEncoded + "." + payloadEncoded;

			var json = new {
				payload = payloadEncoded,
				@protected = protectedHeaderEncoded,
				signature = ComputeSignature(Encoding.UTF8.GetBytes(tbs))
			};
			return AcmeJson.Serialize(json);
		}

		private string GetSignatureAlg() {
			return Registration?.Key?.SignatureAlgorithmName;
		}

		private Task<TResult> SendPostAsync<TSource, TResult>(Uri uri, TSource model) where TResult : AcmeResource {
			return SendPostAsync<TSource, TResult>(uri, model, (headers, authz) => {
				authz.Location = headers.Location;
			});
		}

		private async Task<TResult> SendPostAsync<TSource, TResult>(Uri uri, TSource model, Action<HttpResponseHeaders, TResult> headersHandler) where TResult : class {
			var dataContent = AcmeJson.Serialize(model);
			var data = Encoding.UTF8.GetBytes(dataContent);
			var signedContent = Sign(uri, data);

			var response = await _client.PostAsync(uri, GetStringContent(signedContent)).ConfigureAwait(false);
			return await ProcessRequestAsync(response, headersHandler).ConfigureAwait(false);
		}

		private async Task<TResult> SendPostKidAsync<TSource, TResult>(Uri uri, TSource model, Action<HttpResponseHeaders, TResult>? headersHandler = null) where TResult : class {
			var dataContent = AcmeJson.Serialize(model);
			var data = Encoding.UTF8.GetBytes(dataContent);
			var signedContent = SignKid(uri, data);

			var response = await _client.PostAsync(uri, GetStringContent(signedContent)).ConfigureAwait(false);
			return await ProcessRequestAsync(response, headersHandler).ConfigureAwait(false);
		}

		private async Task<string> SendPostAsync<TSource>(Uri uri, TSource model, Action<HttpResponseHeaders, string>? headersHandler = null) {
			var dataContent = AcmeJson.Serialize(model);
			var data = Encoding.UTF8.GetBytes(dataContent);
			var signedContent = Sign(uri, data);

			var response = await _client.PostAsync(uri, GetStringContent(signedContent)).ConfigureAwait(false);
			return await ProcessRequestAsync(response, headersHandler).ConfigureAwait(false);
		}

		private async Task<TResult> SendGetAsync<TResult>(Uri uri, Action<HttpResponseHeaders, TResult>? headersHandler = null) where TResult : class {
			var request = new HttpRequestMessage(HttpMethod.Get, uri);
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			var response = await _client.SendAsync(request).ConfigureAwait(false);
			return await ProcessRequestAsync(response, headersHandler).ConfigureAwait(false);
		}

		private async Task<TResult> SendPostAsGetAsync<TResult>(Uri uri, Action<HttpResponseHeaders, TResult>? headersHandler = null) where TResult : class {
			var response = await SendPostAsGetResponseAsync(uri).ConfigureAwait(false);
			return await ProcessRequestAsync(response, headersHandler).ConfigureAwait(false);
		}

		private async Task<string> SendPostAsGetStringAsync(Uri uri, Action<HttpResponseHeaders, string>? headersHandler = null) {
			var response = await SendPostAsGetResponseAsync(uri).ConfigureAwait(false);
			return await ProcessRequestStringAsync(response, headersHandler).ConfigureAwait(false);
		}

		private async Task<byte[]> SendPostAsGetBytesAsync(Uri uri, Action<HttpResponseHeaders, byte[]> headersHandler = null) {
			var response = await SendPostAsGetResponseAsync(uri).ConfigureAwait(false);
			return await ProcessRequestBytesAsync(response, headersHandler).ConfigureAwait(false);
		}

		private async Task<HttpResponseMessage> SendPostAsGetResponseAsync(Uri uri) {
			await EnsureNonceAsync();

			var data = new byte[0];
			var signedContent = SignKid(uri, data);

			var response = await _client.PostAsync(uri, GetStringContent(signedContent)).ConfigureAwait(false);
			return response;
		}

		private async Task SendHeadAsync(Uri uri) {
			var request = new HttpRequestMessage(HttpMethod.Head, uri);
			var response = await _client.SendAsync(request).ConfigureAwait(false);
			await ProcessRequestAsync(response).ConfigureAwait(false);
		}

		private async Task<TResult> ProcessRequestAsync<TResult>(HttpResponseMessage response, Action<HttpResponseHeaders, TResult>? headersHandler = null) where TResult : class {
			var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			var res = AcmeJson.Deserialize<TResult>(responseContent);
			headersHandler?.Invoke(response.Headers, res);
			return res;
		}

		private async Task<string> ProcessRequestStringAsync(HttpResponseMessage response, Action<HttpResponseHeaders, string>? headersHandler = null) {
			var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			headersHandler?.Invoke(response.Headers, responseContent);
			return responseContent;
		}

		private async Task<byte[]> ProcessRequestBytesAsync(HttpResponseMessage response, Action<HttpResponseHeaders, byte[]>? headersHandler = null) {
			var responseContent = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
			headersHandler?.Invoke(response.Headers, responseContent);
			return responseContent;
		}

		private async Task<string> ProcessRequestAsync(HttpResponseMessage response, Action<HttpResponseHeaders, string>? headersHandler = null) {
			var res = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			headersHandler?.Invoke(response.Headers, res);
			return res;
		}

		private async Task EnsureNonceAsync() {
			if (_directory.NewNonceUrl != null) {
				await NewNonceAsync().ConfigureAwait(false);
			} else {
				await GetDirectoryAsync().ConfigureAwait(false);
			}
		}

		private StringContent GetStringContent(string val) {
			return new StringContent(val, null, "application/jose+json") {
				Headers = {
					ContentType = {
						CharSet = string.Empty //letsencrypt fails if charset is specified
                    }
				}
			};
		}

		public void Dispose() {
			_client.Dispose();
		}

	}
}
