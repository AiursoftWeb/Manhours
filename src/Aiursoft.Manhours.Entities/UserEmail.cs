using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.Manhours.Entities;

[Index(nameof(Email), IsUnique = true)]
public class UserEmail
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public DateTime CreatedTime { get; init; } = DateTime.UtcNow;

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
