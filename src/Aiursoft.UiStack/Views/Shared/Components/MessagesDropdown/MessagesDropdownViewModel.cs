using Aiursoft.UiStack.Views.Shared.Components.MegaMenu;

namespace Aiursoft.UiStack.Views.Shared.Components.MessagesDropdown;

public class MessagesDropdownViewModel
{
    public Message[] Messages { get; init; } = [];
    
    public required Link ViewAllLink { get; init; }
}

public class Message
{
    public required string SenderAvatarUrl { get; init; }
    
    public required string SenderName { get; init; }
    
    public required string LatestMessagePreview { get; init; }
    
    public required string ClickableLink { get; init; }
    
    public required DateTime LatestMessageTime { get; init; }
}