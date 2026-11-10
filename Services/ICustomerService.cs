using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface ICustomerService
{
    Task<List<CustomerViewModel>> GetAllCustomersAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<CustomerViewModel?> GetCustomerByIdAsync(int id, CancellationToken ct = default);
    Task<CustomerViewModel> CreateCustomerAsync(CustomerDto dto, CancellationToken ct = default);
    Task<CustomerViewModel?> UpdateCustomerAsync(int id, CustomerDto dto, CancellationToken ct = default);
    Task<bool> ToggleCustomerActiveAsync(int id, CancellationToken ct = default);

    Task<List<AccountViewModel>> GetAllAccountsAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<AccountViewModel?> GetAccountByIdAsync(int id, CancellationToken ct = default);
    Task<AccountViewModel> CreateAccountAsync(AccountSaveDto dto, CancellationToken ct = default);
    Task<AccountViewModel?> UpdateAccountAsync(int id, AccountSaveDto dto, CancellationToken ct = default);
    Task<bool> ToggleAccountActiveAsync(int id, CancellationToken ct = default);
}
