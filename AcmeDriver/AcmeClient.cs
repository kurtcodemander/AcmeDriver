﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AcmeDriver.Handlers;

namespace AcmeDriver {
    public class AcmeClient : IDisposable {

        private readonly HttpClient _client;
        private readonly AcmeDirectory _directory;

        public AcmeClientRegistration Registration { get; set; }

        public string Nonce { get; set; }

        public const string LETS_ENCRYPT_STAGING_URL = "https://acme-staging.api.letsencrypt.org";
        public const string LETS_ENCRYPT_PRODUCTION_URL = "https://acme-v01.api.letsencrypt.org";

        public AcmeClient(string baseUrl) : this(AcmeDirectory.FromBaseUrl(baseUrl)) {
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
            using (var client = new HttpClient()) {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/directory");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                var res = JsonConvert.DeserializeObject<AcmeDirectory>(responseContent);
                var acmeClient = new AcmeClient(res);
                return acmeClient;
            }
        }

        public async Task<AcmeDirectory> GetDirectoryAsync() {
            return await SendGetAsync<AcmeDirectory>(new Uri(_directory.DirectoryUrl));
        }

        #region Registrations

        public async Task<AcmeRegistration> NewRegistrationAsync(string[] contacts, RSA rsa = null) {
            if (rsa == null) {
                rsa = RSA.Create();
                rsa.KeySize = 2048;
            }
            var reg = new AcmeClientRegistration {
                Key = rsa
            };
            Registration = reg;
            var data = await SendPostAsync<object, AcmeRegistration>(new Uri(_directory.NewRegUrl), new {
                resource = "new-reg",
                contact = contacts
            });
            reg.Id = data.Id;
            return data;
        }

        public async Task<AcmeRegistration> GetRegistrationAsync(Uri registrationUri) {
            //$"/acme/reg/{Registration.Id}"
            var data = await SendPostAsync<object, AcmeRegistration>(registrationUri, new {
                resource = "reg"
            });
            return data;
        }

        public async Task UpdateRegistrationAsync(Uri registrationUri) {
            await Task.FromResult(0);
        }

        public async Task<AcmeRegistration> AcceptRegistrationAgreementAsync(Uri registrationUri, string agreementUrl) {
            //$"/acme/reg/{Registration.Id}"
            var data = await SendPostAsync<object, AcmeRegistration>(registrationUri, new {
                resource = "reg",
                agreement = agreementUrl
            });
            return data;
        }

        #endregion

        #region Authorizations

        public async Task<AcmeAuthorization> NewAuthorizationAsync(AcmeIdentifier identifier) {
            await EnsureNonceAsync();
            return await SendPostAsync<object, AcmeAuthorization>(new Uri(_directory.NewAuthzUrl), new {
                resource = "new-authz",
                identifier = new {
                    type = identifier.Type,
                    value = identifier.Value
                }
            });
        }

        public async Task<AcmeAuthorization> NewAuthorizationAsync(string domainName) {
            return await NewAuthorizationAsync(new AcmeIdentifier {
                Type = "dns",
                Value = domainName
            });
        }

        public async Task<AcmeAuthorization> GetAuthorizationAsync(Uri location) {
            var data = await SendGetAsync<AcmeAuthorization>(location);
            data.Location = location;
            return data;
        }

        ///<summary>
        ///<para>Deactivates authorization.</para>
        ///<para>Introduced in https://tools.ietf.org/html/draft-ietf-acme-acme-03</para>
        ///</summary>
        public async Task DeactivateAuthorizationAsync(Uri authorizationUri) {
            await SendPostAsync(authorizationUri, new {
                status = AcmeAuthorizationStatus.Deactivated.ToString().ToLower()
            });
        }

        ///<summary>
        ///<para>Deletes authorization.</para>
        ///<para>Introduced in https://tools.ietf.org/html/draft-ietf-acme-acme-02</para>
        ///<para>Removed in https://tools.ietf.org/html/draft-ietf-acme-acme-03. Use <see cref="M:DeactivateAuthorizationAsync" /></para>
        ///</summary>
        public async Task DeleteAuthorizationAsync(Uri authorizationUri) {
            await SendPostAsync(authorizationUri, new {
                resource = "authz",
                delete = true
            });
        }

        #endregion

        #region Certificates

        public async Task<Uri> NewCertificateAsync(AcmeOrder order) {
            Uri location = null;
            await SendPostAsync<object>(new Uri(_directory.NewCertUrl), new {
                resource = "new-cert",
                csr = Base64Url.Encode(order.Csr.GetPemCsrData()),
                notBefore = order.NotBefore.ToRfc3339String(),
                notAfter = order.NotAfter.ToRfc3339String(),
            }, (headers, ord) => {
                location = headers.Location;
            });
            return location;
        }

        public async Task<byte[]> DownloadCertificateAsync(Uri uri) {
            using (var client = new HttpClient()) {
                return await client.GetByteArrayAsync(uri);
            }
        }

        #endregion

        #region Challenges

        public async Task<AcmeChallengeData> CompleteChallengeAsync(AcmeChallenge challenge) {
            return await CompleteChallengeAsync(challenge.Data);
        }

        public async Task<AcmeChallengeData> CompleteChallengeAsync(AcmeChallengeData challenge) {
            var data = await SendPostAsync<object, AcmeChallengeData>(new Uri(challenge.Uri), new {
                resource = "challenge",
                type = challenge.Type,
                keyAuthorization = challenge.GetKeyAuthorization(Registration)
            }, null);
            return data;
        }

        #endregion

        public async Task NewNonceAsync() {
            await SendHeadAsync(new Uri(_directory.NewNonceUrl));
        }

        private string ComputeSignature(byte[] data) {
            if (Registration == null) {
                throw new Exception("registration is not set");
            }
            var signature = Registration.Key.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Base64Url.Encode(signature);
        }

        private string Sign(byte[] payload) {
            if (Registration == null) {
                throw new Exception("registration is not set");
            }
            var protectedHeader = new {
                nonce = Nonce
            };
            var protectedHeaderJson = JsonConvert.SerializeObject(protectedHeader);
            var protectedHeaderData = Encoding.UTF8.GetBytes(protectedHeaderJson);
            var protectedHeaderEncoded = Base64Url.Encode(protectedHeaderData);

            var payloadEncoded = Base64Url.Encode(payload);

            var tbs = protectedHeaderEncoded + "." + payloadEncoded;

            var json = new {
                payload = payloadEncoded,
                @protected = protectedHeaderEncoded,
                header = new {
                    typ = "JWT",
                    alg = "RS256",
                    jwk = Registration.GetJwk()
                },
                signature = ComputeSignature(Encoding.UTF8.GetBytes(tbs))
            };
            return JsonConvert.SerializeObject(json);
        }

        private async Task<TResult> SendPostAsync<TSource, TResult>(Uri uri, TSource model) where TResult : AcmeResource {
            return await SendPostAsync<TSource, TResult>(uri, model, (headers, authz) => {
                authz.Location = headers.Location;
            });
        }

        private async Task<TResult> SendPostAsync<TSource, TResult>(Uri uri, TSource model, Action<HttpResponseHeaders, TResult> headersHandler) where TResult : class {
            var dataContent = JsonConvert.SerializeObject(model);
            var data = Encoding.UTF8.GetBytes(dataContent);
            var signedContent = Sign(data);

            var response = await _client.PostAsync(uri, new StringContent(signedContent, Encoding.UTF8, "application/json"));
            return await ProcessRequestAsync(response, headersHandler);
        }

        private async Task<string> SendPostAsync<TSource>(Uri uri, TSource model, Action<HttpResponseHeaders, string> headersHandler = null) {
            var dataContent = JsonConvert.SerializeObject(model);
            var data = Encoding.UTF8.GetBytes(dataContent);
            var signedContent = Sign(data);

            var response = await _client.PostAsync(uri, new StringContent(signedContent, Encoding.UTF8, "application/json"));
            return await ProcessRequestAsync(response, headersHandler);
        }

        private async Task<TResult> SendGetAsync<TResult>(Uri uri, Action<HttpResponseHeaders, TResult> headersHandler = null) where TResult : class {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await _client.SendAsync(request);
            return await ProcessRequestAsync(response, headersHandler);
        }

        private async Task SendHeadAsync(Uri uri) {
            var request = new HttpRequestMessage(HttpMethod.Head, uri);
            var response = await _client.SendAsync(request);
            await ProcessRequestAsync(response);
        }

        private async Task<TResult> ProcessRequestAsync<TResult>(HttpResponseMessage response, Action<HttpResponseHeaders, TResult> headersHandler = null) where TResult : class {
            var responseContent = await response.Content.ReadAsStringAsync();
            var res = JsonConvert.DeserializeObject<TResult>(responseContent);
            headersHandler?.Invoke(response.Headers, res);
            return res;
        }

        private async Task<string> ProcessRequestAsync(HttpResponseMessage response, Action<HttpResponseHeaders, string> headersHandler = null) {
            var res = await response.Content.ReadAsStringAsync();
            headersHandler?.Invoke(response.Headers, res);
            return res;
        }

        private async Task EnsureNonceAsync() {
            if (!string.IsNullOrWhiteSpace(_directory.NewNonceUrl)) {
                await NewNonceAsync();
            } else {
                await GetDirectoryAsync();
            }
        }

        public void Dispose() {
            _client.Dispose();
        }

    }
}
