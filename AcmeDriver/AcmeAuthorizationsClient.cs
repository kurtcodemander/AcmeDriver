using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AcmeDriver {
	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(NewAuthorizationIdentifier))]
	public partial class NewAuthorizationIdentifierDataSourceGenerationContext : JsonSerializerContext {
	}

	public class NewAuthorizationIdentifier {
		[JsonPropertyName("type")]
		public string? Type { get; set; }

		[JsonPropertyName("value")]
		public string? Value { get; set; }
	}


	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(NewAuthorizationData))]
	public partial class NewAuthorizationDataSourceGenerationContext : JsonSerializerContext {
	}

	public class NewAuthorizationData {
		[JsonPropertyName("resource")]
		public string? Resource { get; set; }

		[JsonPropertyName("identifier")]
		public NewAuthorizationIdentifier? Identifier { get; set; }
	}

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(PostKidData))]
	public partial class PostKidDataSourceGenerationContext : JsonSerializerContext {
	}
	
	public class PostKidData {
		[JsonPropertyName("type")]
		public string? Type { get; set; }

		[JsonPropertyName("keyAuthorization")]
		public string? KeyAuthorization { get; set; }
	}

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(DeleteAuthorizationData))]
	public partial class DeleteAuthorizationDataSourceGenerationContext : JsonSerializerContext {
	}

	public class DeleteAuthorizationData {
		[JsonPropertyName("resource")]
		public string? Resource { get; set; }

		[JsonPropertyName("delete")]
		public bool Delete { get; set; } = true; 
	}


	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(DeactivateAuthorizationData))]
	public partial class DeactivateAuthorizationDataSourceGenerationContext : JsonSerializerContext {
	}

	public class DeactivateAuthorizationData {
		[JsonPropertyName("status")]
		public string? Status { get; set; }		
	}

	public class AcmeAuthorizationsClient : IAcmeAuthorizationsClient {

		private readonly AcmeAuthenticatedClientContext _context;

		public AcmeAuthorizationsClient(AcmeAuthenticatedClientContext context) {
			_context = context;
		}

		public async Task<AcmeAuthorization> NewAuthorizationAsync(AcmeIdentifier identifier) {
			if (_context.Directory.NewAuthzUrl == null) {
				throw new NotSupportedException("New authorization endpoint is not supported");
			}
			var data = await _context.SendPostAsync<NewAuthorizationData, AcmeAuthorizationData>(_context.Directory.NewAuthzUrl, new NewAuthorizationData() {
				Resource = "new-authz",
				Identifier = new NewAuthorizationIdentifier() {
					Type = identifier.Type,
					Value = identifier.Value
				}
			}, 
			NewAuthorizationDataSourceGenerationContext.Default.NewAuthorizationData, // Input JsonTypeInfo
			AcmeAuthorizationDataSourceGenerationContext.Default.AcmeAuthorizationData // Result JsonTypeInfo
			).ConfigureAwait(false);
			
			return new AcmeAuthorization(data, _context.Registration);
		}

		public Task<AcmeAuthorization> NewAuthorizationAsync(string domainName) {
			return NewAuthorizationAsync(new AcmeIdentifier {
				Type = "dns",
				Value = domainName
			});
		}

		public async Task<AcmeAuthorization> GetAuthorizationAsync(Uri location) {
			var data = await _context.SendPostAsGetAsync<AcmeAuthorizationData>(location, AcmeAuthorizationDataSourceGenerationContext.Default.AcmeAuthorizationData).ConfigureAwait(false);
			data.Location = location;
			return new AcmeAuthorization(data, _context.Registration);
		}

		///<summary>
		///<para>Deactivates authorization.</para>
		///<para>Introduced in https://tools.ietf.org/html/draft-ietf-acme-acme-03</para>
		///</summary>
		public Task DeactivateAuthorizationAsync(Uri authorizationUri) {
			return _context.SendPostVoidAsync(authorizationUri, new DeactivateAuthorizationData () {
				Status = AcmeAuthorizationStatus.Deactivated.ToString().ToLower()
			}, DeactivateAuthorizationDataSourceGenerationContext.Default.DeactivateAuthorizationData);
		}

		///<summary>
		///<para>Deletes authorization.</para>
		///<para>Introduced in https://tools.ietf.org/html/draft-ietf-acme-acme-02</para>
		///<para>Removed in https://tools.ietf.org/html/draft-ietf-acme-acme-03. Use <see cref="M:DeactivateAuthorizationAsync" /></para>
		///</summary>
		public Task DeleteAuthorizationAsync(Uri authorizationUri) {
			return _context.SendPostVoidAsync(authorizationUri, new DeleteAuthorizationData() {
				Resource = "authz",
				Delete = true
			}, DeleteAuthorizationDataSourceGenerationContext.Default.DeleteAuthorizationData);
		}

		public async Task<AcmeChallenge> CompleteChallengeAsync(AcmeChallenge challenge) {
			var data = challenge.Data;
			var res = await _context.SendPostKidAsync<PostKidData, AcmeChallengeData>(data.Url, new PostKidData() {
				Type = data.Type,
				KeyAuthorization = data.GetKeyAuthorization(_context.Registration)
			}, 
			PostKidDataSourceGenerationContext.Default.PostKidData,
			AcmeChallengeDataSourceGenerationContext.Default.AcmeChallengeData
			);
			return AcmeChallenge.From(res, challenge.Authorization, _context.Registration);
		}

	}
}
