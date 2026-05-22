using Microsoft.Extensions.Logging;
using NutriAI.Application.Configuration;
using NutriAI.Application.Interfaces.Services;

namespace NutriAI.Infrastructure.Services;

public class AiSettingsService : IAiSettingsService
{
    private readonly IOpenAiSettingsStore _settingsStore;
    private readonly ILogger<AiSettingsService> _logger;

    public AiSettingsService(IOpenAiSettingsStore settingsStore, ILogger<AiSettingsService> logger)
    {
        _settingsStore = settingsStore;
        _logger = logger;
    }

    public Task<AiSettingsFormData> GetFormDataAsync(CancellationToken cancellationToken = default)
    {
        var current = _settingsStore.GetCurrent();
        var lastFour = GetLastFour(current.ApiKey);

        return Task.FromResult(new AiSettingsFormData(
            current.Model,
            lastFour,
            OpenAiModelOptions.SupportedModels));
    }

    public async Task<AiSettingsSaveResult> SaveAsync(string? apiKey, string model, CancellationToken cancellationToken = default)
    {
        var current = _settingsStore.GetCurrent();
        var resolvedKey = string.IsNullOrWhiteSpace(apiKey) ? current.ApiKey : apiKey.Trim();

        if (string.IsNullOrWhiteSpace(resolvedKey))
            return new AiSettingsSaveResult(false, "OpenAI API Key is required.");

        if (!OpenAiModelOptions.SupportedModels.Contains(model))
            return new AiSettingsSaveResult(false, "Please select a supported model.");

        try
        {
            await _settingsStore.SaveAsync(resolvedKey, model, cancellationToken);
        }
        catch (Exception)
        {
            return new AiSettingsSaveResult(false, "Could not save AI settings. Please try again.");
        }

        _logger.LogInformation(
            "OpenAI settings updated. Model: {Model}, ApiKey configured: {IsConfigured}",
            model,
            !string.IsNullOrWhiteSpace(resolvedKey));

        return new AiSettingsSaveResult(true, "AI settings saved successfully.");
    }

    private static string? GetLastFour(string? apiKey) =>
        string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 4
            ? null
            : apiKey[^4..];
}
