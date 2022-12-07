using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AcmeDriver {

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(PostKidAcmeOrderData))]
	public partial class PostKidAcmeOrderDataSourceGenerationContext : JsonSerializerContext {
	}

	public class PostKidAcmeOrderData {

		[JsonPropertyName("identifiers")]
		public AcmeIdentifier[]? Identifiers { get; set; }
	}

	[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(PostKidAcmeFinalizeData))]
	public partial class PostKidAcmeFinalizeDataDataSourceGenerationContext : JsonSerializerContext {
	}

	public class PostKidAcmeFinalizeData {

		[JsonPropertyName("csr")]
		public string? Csr { get; set; }
	}

	public class AcmeOrdersClient : IAcmeOrdersClient{

		private readonly AcmeAuthenticatedClientContext _context;

		public AcmeOrdersClient(AcmeAuthenticatedClientContext context) {
			_context = context;
		}

		public Task<AcmeOrder> GetOrderAsync(Uri location) {
			return _context.SendPostAsGetAsync<AcmeOrder>(location, AcmeOrderSourceGenerationContext.Default.AcmeOrder);
		}

		public async Task<AcmeOrder> NewOrderAsync(AcmeOrder order) {
			return await _context.SendPostKidAsync<PostKidAcmeOrderData, AcmeOrder>(_context.Directory.NewOrderUrl, new PostKidAcmeOrderData() {
				Identifiers = order.Identifiers, 
			}, 
			PostKidAcmeOrderDataSourceGenerationContext.Default.PostKidAcmeOrderData,
			AcmeOrderSourceGenerationContext.Default.AcmeOrder,
			(headers, ord) => {
				ord.Location = headers.Location ?? order.Location;
			}).ConfigureAwait(false);
		}

		public async Task<AcmeOrder> FinalizeOrderAsync(AcmeOrder order, string csr) {
			return await _context.SendPostKidAsync<PostKidAcmeFinalizeData, AcmeOrder>(order.Finalize, new PostKidAcmeFinalizeData () {
				Csr = Base64Url.Encode(csr.GetPemCsrData())
			},  
			PostKidAcmeFinalizeDataDataSourceGenerationContext.Default.PostKidAcmeFinalizeData, 
			AcmeOrderSourceGenerationContext.Default.AcmeOrder,
			(headers, ord) => {
				ord.Location = headers.Location ?? order.Location;
			}).ConfigureAwait(false);
		}

		public Task<string> DownloadCertificateAsync(AcmeOrder order) {
			if (order.Certificate == null) {
				throw new ArgumentException("Order's certificate field is null");
			}
			return _context.SendPostAsGetStringAsync(order.Certificate);
		}

	}
}
