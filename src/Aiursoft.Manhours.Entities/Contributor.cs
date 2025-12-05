using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.Manhours.Entities;

[Index(nameof(Email), IsUnique = true)]
public class Contributor
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(255)]
    public required string Email { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }

    public IEnumerable<RepoContribution> Contributions { get; set; } = new List<RepoContribution>();
}
