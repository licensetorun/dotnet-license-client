using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Licensetorun.Licensing
{
    /// <summary>
    /// Client for the Licensetorun.com licensing API — activate, validate, swap
    /// and check updates for your licensed product, against licensetorun.com or
    /// your own self-hosted on-prem server.
    /// </summary>
    /// <remarks>
    /// Every method returns a <see cref="LicenseResult"/> and never throws on a
    /// network/HTTP error — a transport failure surfaces as <c>Ok == false</c>,
    /// <c>Status == 0</c> and <c>Body.error == "network_error"</c>.
    /// <code>
    /// var license = new LicenseClient("https://licensetorun.com", "PRODUCT-UUID", "CUSTOMER-KEY");
    /// if (await license.IsValidAsync()) { /* licensed feature */ }
    /// </code>
    /// </remarks>
    public sealed class LicenseClient
    {
        private readonly string _apiBase;
        private readonly string _productId;
        private readonly string _licenseKey;
        private readonly string _instance;
        private readonly HttpClient _http;

        /// <param name="apiBase">e.g. "https://licensetorun.com" or your self-hosted URL.</param>
        /// <param name="productId">The product's public id (UUID) from the dashboard.</param>
        /// <param name="licenseKey">The customer's license key.</param>
        /// <param name="instance">Stable id for this install. Defaults to the machine name.</param>
        /// <param name="timeout">Per-request timeout. Defaults to 15s (only applied when no HttpClient is supplied).</param>
        /// <param name="httpClient">Optional shared HttpClient. When omitted, one is created internally.</param>
        public LicenseClient(
            string apiBase,
            string productId,
            string licenseKey = "",
            string instance = null,
            TimeSpan? timeout = null,
            HttpClient httpClient = null)
        {
            if (string.IsNullOrEmpty(apiBase) || string.IsNullOrEmpty(productId))
                throw new ArgumentException("LicenseClient requires apiBase and productId.");

            _apiBase = apiBase.TrimEnd('/');
            _productId = productId;
            _licenseKey = licenseKey ?? string.Empty;
            _instance = string.IsNullOrEmpty(instance) ? (Environment.MachineName ?? "app") : instance;

            _http = httpClient ?? new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(15) };
            if (httpClient != null && timeout.HasValue)
                _http.Timeout = timeout.Value;
        }

        /// <summary>The install identity used for activation/validation.</summary>
        public string Instance => _instance;

        /// <summary>Activate this install and consume a seat.</summary>
        public Task<LicenseResult> ActivateAsync(IDictionary<string, string> extra = null, CancellationToken cancellationToken = default)
        {
            var form = new Dictionary<string, string> { ["instance"] = _instance };
            if (extra != null)
                foreach (var kv in extra) form[kv.Key] = kv.Value;
            return PostAsync("activate", form, cancellationToken);
        }

        /// <summary>Release the seat held by this install.</summary>
        public Task<LicenseResult> DeactivateAsync(CancellationToken cancellationToken = default)
            => PostAsync("deactivate", new Dictionary<string, string> { ["instance"] = _instance }, cancellationToken);

        /// <summary>Validate the license + this install. Cache the result yourself if hot.</summary>
        public Task<LicenseResult> ValidateAsync(CancellationToken cancellationToken = default)
            => PostAsync("validate", new Dictionary<string, string> { ["instance"] = _instance }, cancellationToken);

        /// <summary>Boolean convenience over <see cref="ValidateAsync"/>.</summary>
        public async Task<bool> IsValidAsync(CancellationToken cancellationToken = default)
            => (await ValidateAsync(cancellationToken).ConfigureAwait(false)).Ok;

        /// <summary>Move this seat from one install to another.</summary>
        public Task<LicenseResult> SwapAsync(string fromInstance, string toInstance, IDictionary<string, string> extra = null, CancellationToken cancellationToken = default)
        {
            var form = new Dictionary<string, string> { ["from_instance"] = fromInstance, ["to_instance"] = toInstance };
            if (extra != null)
                foreach (var kv in extra) form[kv.Key] = kv.Value;
            return PostAsync("swap", form, cancellationToken);
        }

        /// <summary>List the installs currently holding a seat.</summary>
        public Task<LicenseResult> ActivationsAsync(CancellationToken cancellationToken = default)
            => PostAsync("activations", new Dictionary<string, string>(), cancellationToken);

        /// <summary>Ask the update server whether a newer release exists.</summary>
        public Task<LicenseResult> CheckForUpdateAsync(string currentVersion, CancellationToken cancellationToken = default)
            => GetAsync("update", new Dictionary<string, string> { ["instance"] = _instance, ["version"] = currentVersion }, cancellationToken);

        // -- internals --------------------------------------------------------

        private string Url(string path)
            => _apiBase + "/api/v1/products/" + Uri.EscapeDataString(_productId) + "/" + path;

        private async Task<LicenseResult> PostAsync(string path, IDictionary<string, string> payload, CancellationToken cancellationToken)
        {
            var form = new Dictionary<string, string>(payload) { ["license_key"] = _licenseKey };
            try
            {
                using (var content = new FormUrlEncodedContent(form))
                using (var request = new HttpRequestMessage(HttpMethod.Post, Url(path)) { Content = content })
                {
                    request.Headers.Accept.ParseAdd("application/json");
                    return await SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                return NetworkError(ex);
            }
        }

        private async Task<LicenseResult> GetAsync(string path, IDictionary<string, string> query, CancellationToken cancellationToken)
        {
            var all = new Dictionary<string, string>(query) { ["license_key"] = _licenseKey };
            var qs = string.Join("&", all.Select(kv => Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value ?? string.Empty)));
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, Url(path) + "?" + qs))
                {
                    request.Headers.Accept.ParseAdd("application/json");
                    return await SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                return NetworkError(ex);
            }
        }

        private async Task<LicenseResult> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false))
            {
                var raw = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var status = (int)response.StatusCode;
                return new LicenseResult(status >= 200 && status < 300, status, Parse(raw));
            }
        }

        private static LicenseResult NetworkError(Exception ex)
        {
            var json = "{\"error\":\"network_error\",\"message\":" + JsonSerializer.Serialize(ex.Message) + "}";
            return new LicenseResult(false, 0, Parse(json));
        }

        private static JsonElement Parse(string raw)
        {
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    using (var doc = JsonDocument.Parse(raw))
                    {
                        if (doc.RootElement.ValueKind == JsonValueKind.Object)
                            return doc.RootElement.Clone();
                    }
                }
                catch (JsonException)
                {
                    // fall through to an empty object
                }
            }
            using (var empty = JsonDocument.Parse("{}"))
                return empty.RootElement.Clone();
        }
    }
}
