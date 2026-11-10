using System;

namespace CentralPress.API.DTOs;

public class MoqaysatDetailedFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? CustomerId { get; set; }
}
