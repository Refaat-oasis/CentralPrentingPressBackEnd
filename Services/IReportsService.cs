using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface IReportsService
{
    Task<List<AccountSummaryViewModel>> GetAccountsSummaryAsync(CancellationToken ct = default);
    Task<List<CustomerSummaryViewModel>> GetCustomersSummaryAsync(CustomerSummaryFilterDto filter, CancellationToken ct = default);
    Task<MoqaysatDetailedReportViewModel> GetMoqaysatDetailedAsync(MoqaysatDetailedFilterDto filter, CancellationToken ct = default);
    Task<List<PaymentOrderRegisterItemViewModel>> GetPaymentOrdersRegisterAsync(PaymentOrdersRegisterFilterDto filter, CancellationToken ct = default);
}
