namespace Cirreum.Authentication.EntraClaims;

using Cirreum.Authentication.EntraClaims.Models;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(EntraClaimsResponse))]
[JsonSerializable(typeof(EntraClaimsRequest))]
internal sealed partial class EntraClaimsJsonContext : JsonSerializerContext {
}