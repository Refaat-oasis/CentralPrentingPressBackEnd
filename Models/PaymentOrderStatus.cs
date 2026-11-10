namespace CentralPress.API.Models;

public enum PaymentOrderStatus
{
    Draft = 0,
    UnderReview = 1,
    Approved = 2,
    Paid = 3,
    Rejected = 4,
    Cancelled = 5
}
