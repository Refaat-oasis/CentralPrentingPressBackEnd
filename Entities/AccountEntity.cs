using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralPress.API.Entities;

[Table("Accounts")]
public class AccountEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int AccountId { get; set; }

    [Required]
    [MaxLength(50)]
    public string AccountCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string AccountNameAr { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string DebitHeaderTextAr { get; set; } = string.Empty; // e.g. "من حـ / الإدارة العامة"

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public virtual ICollection<Tasweya> Tasweyat { get; set; } = new List<Tasweya>();
    public virtual ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();
}
