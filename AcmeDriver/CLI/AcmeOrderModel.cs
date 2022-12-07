using System;
using System.Text.Json.Serialization;

namespace AcmeDriver.CLI {

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(AcmeOrderModel))]
	public partial class AcmeOrderModelSourceGenerationContext : JsonSerializerContext {
	}

	public class AcmeOrderModel {

        public AcmeOrderStatus Status { get; set; }

        public DateTimeOffset Expires { get; set; }

        public AcmeIdentifier[] Identifiers { get; set; }

        public Uri[] Authorizations { get; set; }

        public Uri Finalize { get; set; }

        //public Uri Location { get; init; }
		public Uri Location { get; set; }

		public static AcmeOrderModel From(AcmeOrder order) {
			return new AcmeOrderModel {
				Authorizations = order.Authorizations,
				Expires = order.Expires,
				Finalize = order.Finalize,
				Identifiers = order.Identifiers,
				Location = order.Location,
				Status = order.Status,
			};
		}

    }

}
