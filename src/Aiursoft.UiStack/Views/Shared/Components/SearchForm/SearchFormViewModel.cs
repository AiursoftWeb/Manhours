namespace Aiursoft.UiStack.Views.Shared.Components.SearchForm;

public class SearchFormViewModel
{
    public required string SearchUrl { get; init; }
    
    public required string Placeholder { get; init; } = "Search...";
    
    public required string SearchParam { get; init; } = "q";
}