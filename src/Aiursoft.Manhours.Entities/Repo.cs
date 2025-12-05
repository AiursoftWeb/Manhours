using System.ComponentModel.DataAnnotations;

namespace Aiursoft.Manhours.Entities;

public class Repo
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(1000)]
    public required string Url { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }

    public DateTime LastUpdateTime { get; set; } = DateTime.MinValue;

    public IEnumerable<RepoContribution> Contributions { get; set; } = new List<RepoContribution>();
}
