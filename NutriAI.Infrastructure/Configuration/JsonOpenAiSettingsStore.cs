using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NutriAI.Application.Configuration;
using NutriAI.Application.Interfaces.Services;

namespace NutriAI.Infrastructure.Configuration;

public class JsonOpenAiSettingsStore : IOpenAiSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly OpenAiSettings _baseline;
    private readonly string _filePath;
    private readonly ILogger<JsonOpenAiSettingsStore> _logger;
    private readonly object _lock = new();
    private OpenAiRuntimeOverrides? _overrides;

    public JsonOpenAiSettingsStore(
        IOptions<OpenAiSettings> baseline,
        IHostEnvironment environment,
        ILogger<JsonOpenAiSettingsStore> logger)
    {
        _baseline = baseline.Value;
        _logger = logger;
        var dataDir = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "openai-settings.json");
        _overrides = LoadFromDisk();
    }

    public OpenAiSettings GetCurrent()
    {
        lock (_lock)
        {
            var overrides = _overrides;
            return new OpenAiSettings
            {
                ApiKey = FirstNonEmpty(overrides?.ApiKey, _baseline.ApiKey) ?? string.Empty,
                Model = FirstNonEmpty(overrides?.Model, _baseline.Model) ?? OpenAiModelOptions.DefaultModel,
                BaseUrl = _baseline.BaseUrl
            };
        }
    }

    public Task SaveAsync(string apiKey, string model, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _overrides = new OpenAiRuntimeOverrides
            {
                ApiKey = apiKey.Trim(),
                Model = model.Trim()
            };
            WriteToDisk(_overrides);
        }

        return Task.CompletedTask;
    }

    private OpenAiRuntimeOverrides? LoadFromDisk()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<OpenAiRuntimeOverrides>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read OpenAI settings from {Path}; using configuration defaults", _filePath);
            return null;
        }
    }

    private void WriteToDisk(OpenAiRuntimeOverrides overrides)
    {
        try
        {
            var json = JsonSerializer.Serialize(overrides, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist OpenAI settings to {Path}", _filePath);
            throw;
        }
    }

    private static string? FirstNonEmpty(string? primary, string? fallback) =>
        !string.IsNullOrWhiteSpace(primary) ? primary : fallback;

    private sealed class OpenAiRuntimeOverrides
    {
        public string? ApiKey { get; set; }
        public string? Model { get; set; }
    }
}
