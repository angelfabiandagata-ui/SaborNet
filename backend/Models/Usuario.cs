using System.ComponentModel.DataAnnotations;
namespace backend.Models;


public class Usuario
{
  public enum Rol{
  Usuario,
  Administrador }
    public int Id { get; set; }

    [Required(ErrorMessage ="El nombre no puede estar vacio")]
    [MinLength(3, ErrorMessage = "Nombre demasiado Corto , 3 letras minimo")]
    [MaxLength(20, ErrorMessage = "Nombre demasiado largo")]
    public string Nombre { get; set; }="";

    [Required(ErrorMessage = "El email no puede estar vacio")]
    [EmailAddress]
    public string Email { get; set; } ="";

    [Required(ErrorMessage = "La contraseña no puede estar vacia")]
    [DataType(DataType.Password)]
    public string Password { get; set; }="";

    [DataType(DataType.ImageUrl)]
    public string? AvatarUrl  {get; set;}="";
    [Required]
    public Rol rol {get; set;}
}