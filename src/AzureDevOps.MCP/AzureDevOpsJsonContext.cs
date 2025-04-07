using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureDevOps.MCP;

// using AzureDevOps.MCP.HealthChecks; // Temporarily disabled
// .NET 9: Source generation context for JSON serialization performance
[JsonSerializable (typeof (JsonElement))]
[JsonSerializable (typeof (object))]
[JsonSerializable (typeof (Dictionary<string, object>))]
[JsonSerializable (typeof (string[]))]
public partial class AzureDevOpsJsonContext : JsonSerializerContext
{
}
