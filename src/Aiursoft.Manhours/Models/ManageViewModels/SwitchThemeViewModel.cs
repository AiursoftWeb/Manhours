using System.ComponentModel.DataAnnotations;

namespace Aiursoft.Manhours.Models.ManageViewModels;

public class SwitchThemeViewModel
{
    [Required]
    public required string Theme { get; set; }
}
