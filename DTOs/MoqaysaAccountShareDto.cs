using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.DTOs;

public class MoqaysaAccountShareDto
{
    [Range(1, int.MaxValue, ErrorMessage = "معرف الحساب المحاسبي غير صالح")]
    public int AccountId { get; set; }

    [Range(typeof(decimal), "0.01", "100.00", ErrorMessage = "نسبة الحساب يجب أن تكون أكبر من 0 ولا تتجاوز 100%")]
    public decimal SharePercentage { get; set; }
}
