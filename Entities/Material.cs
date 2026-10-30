using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.Entities;

public class Material
{
    [Key]
    public int MaterialId { get; set; }

    [Required]
    [MaxLength(200)]
    public string MaterialNameAr { get; set; } = string.Empty;

    [MaxLength(50)]
    public string UnitNameAr { get; set; } = "فرخ"; // e.g. فرخ, كجم, باكو, زنكة

    public bool IsActive { get; set; } = true;
}
