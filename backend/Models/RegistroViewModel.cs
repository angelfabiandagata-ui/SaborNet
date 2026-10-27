using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class RegistroViewModel
{
    [Required]
    public string Nombre { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmarPassword { get; set; } = "";

    public string? AvatarUrl { get; set; } = "";
}