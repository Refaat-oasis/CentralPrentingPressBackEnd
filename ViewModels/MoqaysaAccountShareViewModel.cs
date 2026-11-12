namespace CentralPress.API.ViewModels;
public class MoqaysaAccountShareViewModel
{
    public int AccountId { get; set; }
    public string AccountNameAr { get; set; } = string.Empty;
    public string DebitHeaderTextAr { get; set; } = string.Empty;
    public decimal SharePercentage { get; set; }
    public decimal AllocatedAmount { get; set; }
}
