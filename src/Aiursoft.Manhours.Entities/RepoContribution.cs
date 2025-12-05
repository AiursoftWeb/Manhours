using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aiursoft.Manhours.Entities;

public class RepoContribution
{
    [Key]
    public Guid Id { get; set; }

    public Guid RepoId { get; set; }

    [ForeignKey(nameof(RepoId))]
    public Repo? Repo { get; set; }

    public Guid ContributorId { get; set; }

    [ForeignKey(nameof(ContributorId))]
    public Contributor? Contributor { get; set; }

    public int CommitCount { get; set; }

    public int ActiveDays { get; set; }

    public double TotalWorkHours { get; set; }
}
