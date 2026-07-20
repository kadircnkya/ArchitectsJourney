using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Domain.Exceptions;
using Json.Schema;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ArchitectsJourney.Infrastructure.MissionLoading;

public sealed class MissionSchemaValidator : IMissionSchemaValidator
{
    public async Task ValidateAsync(string missionId, string jsonPayload, CancellationToken cancellationToken = default)
    {
        JsonNode? rootNode;
        try
        {
            rootNode = JsonNode.Parse(jsonPayload);
            if (rootNode == null)
            {
                throw new MissionSchemaValidationException(missionId, ["Payload is empty or invalid JSON."]);
            }
        }
        catch (JsonException ex)
        {
            throw new MissionSchemaValidationException(missionId, [$"Invalid JSON formatting: {ex.Message}"]);
        }

        var versionNode = rootNode["version"];
        if (versionNode == null)
        {
            throw new MissionSchemaValidationException(missionId, ["Missing required property 'version'."]);
        }

        string version = versionNode.GetValue<string>();
        if (version != "1.0")
        {
            throw MissionVersionNotSupportedException.ForVersion(missionId, version);
        }

        var schema = await LoadSchemaAsync(version);
        if (schema == null)
        {
            throw new MissionSchemaValidationException(missionId, [$"Schema definition for version {version} not found."]);
        }

        var results = schema.Evaluate(rootNode, new EvaluationOptions { OutputFormat = OutputFormat.List });
        
        if (!results.IsValid)
        {
            var errors = new List<string>();
            if (results.Details != null)
            {
                foreach (var detail in results.Details)
                {
                    if (detail.HasErrors && detail.Errors != null)
                    {
                        foreach (var err in detail.Errors)
                        {
                            string path = detail.InstanceLocation?.ToString() ?? "$";
                            errors.Add($"{path}: {err.Value}");
                        }
                    }
                }
            }

            if (errors.Count == 0 && results.Errors != null)
            {
                foreach(var err in results.Errors)
                {
                    errors.Add(err.Value);
                }
            }

            if (errors.Count == 0)
            {
                errors.Add("Schema validation failed for an unknown reason.");
            }

            throw new MissionSchemaValidationException(missionId, errors);
        }

        return;
    }

    private static async Task<JsonSchema?> LoadSchemaAsync(string version)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string resourceName = $"ArchitectsJourney.Infrastructure.MissionLoading.Schemas.mission.schema.v{version.Replace(".", "", StringComparison.Ordinal)}.json";
        if (version == "1.0") 
        {
            resourceName = "ArchitectsJourney.Infrastructure.MissionLoading.Schemas.mission.schema.v1.json";
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        return await JsonSchema.FromStream(stream);
    }
}
