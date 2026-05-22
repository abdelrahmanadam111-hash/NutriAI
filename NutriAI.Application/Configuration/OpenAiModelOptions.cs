namespace NutriAI.Application.Configuration;

public static class OpenAiModelOptions
{
    public static readonly IReadOnlyList<string> SupportedModels =
    [
        "gpt-4o-mini",
        "gpt-4.1-mini",
        "gpt-4.1",
        "o4-mini"
    ];

    public const string DefaultModel = "gpt-4o-mini";
}
