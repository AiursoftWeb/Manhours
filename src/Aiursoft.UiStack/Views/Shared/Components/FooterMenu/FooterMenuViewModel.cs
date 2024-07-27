using Aiursoft.UiStack.Views.Shared.Components.MegaMenu;

namespace Aiursoft.UiStack.Views.Shared.Components.FooterMenu;

public class FooterMenuViewModel
{
    public required Link[] Links { get; init; } = [];
    
    public required Link AppBrand { get; init; }
}