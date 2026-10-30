using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralPress.API.Entities;

public class TasweyaMoqaysaLink
{
    [Key]
    public int LinkId { get; set; }

    public int TasweyaId { get; set; }

    [ForeignKey(nameof(TasweyaId))]
    public virtual Tasweya? Tasweya { get; set; }

    public int MoqaysaId { get; set; }

    [ForeignKey(nameof(MoqaysaId))]
    public virtual Moqaysa? Moqaysa { get; set; }
}
