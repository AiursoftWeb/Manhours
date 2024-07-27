namespace Aiursoft.UiStack.Views.Shared.Components.MegaMenu;

public class MegaMenuViewModel
{
    public required string MenuName { get; init; }
    public required DropDown[] DropDowns { get; init; } = [];
}

public class DropDown
{
    public required string Header { get; init; }
    public required Link[] Links { get; init; } = [];
}

public class Link
{
    public required string Text { get; init; }
    public required string Href { get; init; }
}