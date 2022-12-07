using System;
using System.Net.Http.Headers;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace AcmeDriver {
	public class AcmeAuthenticatedClientContext {

		private readonly AcmeClientContext _inner;
		public AcmeClientRegistration Registration { get; }
		public AcmeDirectory Directory => _inner.Directory;

		public AcmeAuthenticatedClientContext(AcmeClientContext inner, AcmeClientRegistration registration) {
			_inner = inner;
			Registration = registration;
		}

		public Task<TResult> SendPostAsync<TSource, TResult>(Uri uri, TSource model, JsonTypeInfo<TSource> jsonTypeInfoSource, JsonTypeInfo<TResult> jsonTypeInfoResult)  where TResult : AcmeResource {
			return _inner.SendPostAsync<TSource, TResult>(uri, model, jsonTypeInfoSource, jsonTypeInfoResult, Registration);
		}

		public Task SendPostVoidAsync<TSource>(Uri uri, TSource model, JsonTypeInfo<TSource> jsonTypeInfo) {
			return _inner.SendPostVoidAsync(uri, model, jsonTypeInfo, Registration);
		}

		public Task<TResult> SendPostKidAsync<TSource, TResult>(Uri uri, TSource model, JsonTypeInfo<TSource> jsonTypeInfoSource, JsonTypeInfo<TResult> jsonTypeInfoResult, Action<HttpResponseHeaders, TResult>? headersHandler = null) where TResult : class {
			return _inner.SendPostKidAsync<TSource, TResult>(uri, model, jsonTypeInfoSource, jsonTypeInfoResult, Registration, headersHandler);
		}

		public Task<TResult> SendPostAsGetAsync<TResult>(Uri uri, JsonTypeInfo<TResult> jsonTypeInfoResult, Action<HttpResponseHeaders, TResult>? headersHandler = null) where TResult : class {
			return _inner.SendPostAsGetAsync(jsonTypeInfoResult, uri, Registration, headersHandler);
		}

		public Task<string> SendPostAsGetStringAsync(Uri uri, Action<HttpResponseHeaders, string>? headersHandler = null) {
			return _inner.SendPostAsGetStringAsync(uri, Registration, headersHandler);
		}

	}
}
