using Aiursoft.UiStack.Layout;

namespace Aiursoft.Manhours.Models.BackgroundJobs;

public class JobsIndexViewModel : UiStackLayoutViewModel
{
    public IEnumerable<JobInfo> AllRecentJobs { get; init; } = [];
}
