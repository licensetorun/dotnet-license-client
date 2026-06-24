using System.Text.Json;

namespace Licensetorun.Licensing
{
    /// <summary>
    /// Outcome of a licensing API call. <see cref="Ok"/> is true for a 2xx
    /// response; <see cref="Status"/> is 0 on a transport error, in which case
    /// <see cref="Body"/> is <c>{ "error", "message" }</c>.
    /// </summary>
    public sealed class LicenseResult
    {
        /// <summary>True for a 2xx HTTP response.</summary>
        public bool Ok { get; }

        /// <summary>HTTP status code, or 0 on a transport error.</summary>
        public int Status { get; }

        /// <summary>Parsed JSON body (the root object), detached from its document.</summary>
        public JsonElement Body { get; }

        public LicenseResult(bool ok, int status, JsonElement body)
        {
            Ok = ok;
            Status = status;
            Body = body;
        }
    }
}
