using System.Threading;
using System.Threading.Tasks;

namespace CentralPress.API.Services;

public interface IAuditLogService
{
    Task LogStatusTransitionAsync(
        string moduleName,
        string entityName,
        string recordId,
        string actionType,
        object? oldValues,
        object? newValues,
        string? userId = null,
        string? employeeName = null,
        string? reason = null,
        CancellationToken ct = default);
}
