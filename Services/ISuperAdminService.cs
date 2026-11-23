using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface ISuperAdminService
{
    Task<PagedResultViewModel<AuditBacklogViewModel>> GetAuditBacklogAsync(AuditBacklogQueryDto query, CancellationToken cancellationToken = default);
    HangfireOverviewViewModel GetHangfireOverview(string? state = null, int limit = 50);
    bool RequeueHangfireJob(string jobId);
    bool DeleteHangfireJob(string jobId);
}
