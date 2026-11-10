using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface ISuperAdminService
{
    Task<SuperAdminLoginResponseViewModel?> LoginAsync(SuperAdminLoginDto dto, string? ipAddress, CancellationToken cancellationToken = default);

    Task<PagedResultViewModel<AuditBacklogViewModel>> GetAuditBacklogAsync(AuditBacklogQueryDto query, CancellationToken cancellationToken = default);
}
