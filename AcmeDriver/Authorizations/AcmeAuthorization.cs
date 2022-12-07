using System;
using System.Linq;
using AcmeDriver.Utils;
using System.Text.Json.Serialization;

namespace AcmeDriver {

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(AcmeAuthorization))]
	public partial class AcmeAuthorizationSourceGenerationContext : JsonSerializerContext {
	}

	public class AcmeAuthorization {

		public AcmeIdentifier Identifier { get; }

		[JsonConverter(typeof(AcmeAuthorizationStatusConverter))]
		public AcmeAuthorizationStatus Status { get; }

		public DateTimeOffset Expires { get; }

		public AcmeChallenge[] Challenges { get; }

		public bool Wildcard { get; }

		public Uri Location { get; }

		public AcmeAuthorization(AcmeAuthorizationData data, AcmeClientRegistration registration) {
			Identifier = data.Identifier;
			Status = data.Status;
			Expires = data.Expires;
			Challenges = data.Challenges
				.Select(challengeData => AcmeChallenge.From(challengeData, this, registration))
				.ToArray();
			Wildcard = data.Wildcard;
			Location = data.Location;
		}

	}
}
