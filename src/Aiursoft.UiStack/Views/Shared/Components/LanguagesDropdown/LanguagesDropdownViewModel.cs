namespace Aiursoft.UiStack.Views.Shared.Components.LanguagesDropdown;

public class LanguagesDropdownViewModel
{
    public required LanguageSelection SelectedLanguage { get; init; }
    public required LanguageSelection[] Languages { get; init; } = [];
}

public class LanguageSelection
{
    public required string FlagUrl { get; init; }
    
    public required string Link { get; init; }
    
    public required string Name { get; init; }
}