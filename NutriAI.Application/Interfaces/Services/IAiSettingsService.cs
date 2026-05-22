namespace NutriAI.Application.Interfaces.Services;

public interface IAiSettingsService
{
    Task<AiSettingsFormData> GetFormDataAsync(CancellationToken cancellationToken = default);

    Task<AiSettingsSaveResult> SaveAsync(string? apiKey, string model, CancellationToken cancellationToken = default);
}

public sealed record AiSettingsFormData(
    string Model,
    string? ApiKeyLastFour,
    IReadOnlyList<string> AvailableModels);

public sealed record AiSettingsSaveResult(bool Succeeded, string Message);
