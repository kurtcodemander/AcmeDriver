using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AcmeDriver {

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(AcceptAgreementData))]
	public partial class AcceptAgreementDataSourceGenerationContext : JsonSerializerContext {
	}

	public class AcceptAgreementData {
		[JsonPropertyName("resource")]
		public string Resource { get; set; }

		[JsonPropertyName("agreement")]
		public Uri Agreement { get; set; }
	}

	public class AcmeRegistrationsClient : IAcmeRegistrationsClient {

		private readonly AcmeAuthenticatedClientContext _context;

		public AcmeRegistrationsClient(AcmeAuthenticatedClientContext context) {
			_context = context;
		}

		public Task<AcmeRegistration> AcceptAgreementAsync() {
			var tosUrl = _context.Directory.Meta?.TermsOfService;
			if (tosUrl != null) {
				return AcceptAgreementAsync(tosUrl);
			}
			return GetRegistrationAsync();
		}

		public Task<AcmeRegistration> AcceptAgreementAsync(Uri agreementUrl) {
			return _context.SendPostKidAsync<AcceptAgreementData, AcmeRegistration>(_context.Registration.Location, new AcceptAgreementData() {
				Resource = "reg",
				Agreement = agreementUrl
			}, 
			AcceptAgreementDataSourceGenerationContext.Default.AcceptAgreementData,
			AcmeRegistrationSourceGenerationContext.Default.AcmeRegistration
			);
		}

		public Task<AcmeRegistration> GetRegistrationAsync() {
			return _context.SendPostAsGetAsync<AcmeRegistration>(
				_context.Registration.Location, AcmeRegistrationSourceGenerationContext.Default.AcmeRegistration, 
				(headers, reg) => {
				reg.Location = headers.Location ?? _context.Registration.Location;
			});
		}

		public Task UpdateRegistrationAsync(Uri registrationUri) {
			return Task.CompletedTask;
		}

	}
}
