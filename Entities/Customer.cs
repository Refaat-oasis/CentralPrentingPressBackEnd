using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralPress.API.Entities;

public class Customer
{
    [Key]
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(250)]
    public string CustomerNameAr { get; set; } = string.Empty;

    public int AccountId { get; set; }

    [ForeignKey(nameof(AccountId))]
    public virtual AccountEntity? Account { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? BudgetCeiling { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Moqaysa> Moqaysat { get; set; } = new List<Moqaysa>();
    public virtual ICollection<Tasweya> Tasweyat { get; set; } = new List<Tasweya>();
    public virtual ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();
}
