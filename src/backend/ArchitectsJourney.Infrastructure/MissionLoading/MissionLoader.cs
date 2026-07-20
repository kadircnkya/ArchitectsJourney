using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.DTOs.Mission;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Exceptions;
using System.Text.Json;

namespace ArchitectsJourney.Infrastructure.MissionLoading;

public sealed class MissionLoader : IMissionLoader
{
    private readonly IMissionDiscovery _discovery;
    private readonly IMissionReader _reader;
    private readonly IMissionSchemaValidator _schemaValidator;
    private readonly IMissionReferenceValidator _referenceValidator;
    private readonly IMissionBuilder _builder;
    private readonly IMissionCache _cache;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MissionLoader(
        IMissionDiscovery discovery,
        IMissionReader reader,
        IMissionSchemaValidator schemaValidator,
        IMissionReferenceValidator referenceValidator,
        IMissionBuilder builder,
        IMissionCache cache)
    {
        _discovery = discovery;
        _reader = reader;
        _schemaValidator = schemaValidator;
        _referenceValidator = referenceValidator;
        _builder = builder;
        _cache = cache;
    }

    public async Task<Mission> LoadMissionAsync(string missionId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetMission(missionId, out var cachedMission))
        {
            return cachedMission!;
        }

        var filePath = await _discovery.GetMissionFilePathAsync(missionId, cancellationToken);
        var jsonPayload = await _reader.ReadMissionJsonAsync(filePath, cancellationToken);

        await _schemaValidator.ValidateAsync(missionId, jsonPayload, cancellationToken);

        MissionDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<MissionDto>(jsonPayload, _jsonOptions);
        }
        catch (Exception ex)
        {
            throw new MissionSchemaValidationException(missionId, [$"Deserialization failed: {ex.Message}"]);
        }

        if (dto == null)
        {
            throw new MissionSchemaValidationException(missionId, ["Deserialized DTO is null."]);
        }

        await _referenceValidator.ValidateAsync(missionId, dto, cancellationToken);

        var mission = _builder.Build(dto);

        _cache.CacheMission(mission);

        return mission;
    }
}
