using Aiursoft.UiStack.Views.Shared.Components.MegaMenu;

namespace Aiursoft.UiStack.Views.Shared.Components.NotificationsDropdown;

public class NotificationsDropdownViewModel
{
    public IReadOnlyCollection<Notification> Notifications { get; init; } = [];
    
    public required Link ViewAllLink { get; init; }
}

public class Notification
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required DateTime TriggerTime { get; init; }

    public required string Icon { get; init; } = "bell";
    public required string IconClass { get; init; } = "text-warning";
}