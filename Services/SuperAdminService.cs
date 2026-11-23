using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;
using Hangfire;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Services;

public class SuperAdminService : ISuperAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public SuperAdminService(
        ApplicationDbContext context,
        IBackgroundJobClient backgroundJobClient)
    {
        _context = context;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<PagedResultViewModel<AuditBacklogViewModel>> GetAuditBacklogAsync(
        AuditBacklogQueryDto query,
        CancellationToken cancellationToken = default)
    {
        query ??= new AuditBacklogQueryDto();

        var queryable = _context.AuditBacklogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.ModuleName))
        {
            var module = query.ModuleName.Trim();
            queryable = queryable.Where(b => b.ModuleName == module);
        }

        if (!string.IsNullOrWhiteSpace(query.ActionType))
        {
            var action = query.ActionType.Trim();
            queryable = queryable.Where(b => b.ActionType == action);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLowerInvariant();
            if (status == "success")
            {
                queryable = queryable.Where(b => b.IsSuccess);
            }
            else if (status == "error" || status == "failed")
            {
                queryable = queryable.Where(b => !b.IsSuccess);
            }
        }

        if (query.FromDateUtc.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(query.FromDateUtc.Value, DateTimeKind.Utc);
            queryable = queryable.Where(b => b.TimestampUtc >= fromUtc);
        }

        if (query.ToDateUtc.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(query.ToDateUtc.Value, DateTimeKind.Utc);
            queryable = queryable.Where(b => b.TimestampUtc <= toUtc);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            queryable = queryable.Where(b =>
                (b.EmployeeName != null && b.EmployeeName.Contains(search)) ||
                (b.ActionType != null && b.ActionType.Contains(search)) ||
                (b.RequestPath != null && b.RequestPath.Contains(search)) ||
                (b.ErrorMessage != null && b.ErrorMessage.Contains(search)) ||
                (b.NewValuesJson != null && b.NewValuesJson.Contains(search))
            );
        }

        int totalCount = await queryable.CountAsync(cancellationToken);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);
        int page = Math.Max(1, query.Page);

        var items = await queryable
            .OrderByDescending(b => b.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new AuditBacklogViewModel
            {
                AuditId = b.AuditId,
                UserId = b.UserId,
                EmployeeName = b.EmployeeName,
                JobTitleName = b.JobTitleName,
                ActionType = b.ActionType,
                ModuleName = b.ModuleName,
                EntityName = b.EntityName,
                RecordId = b.RecordId,
                RequestPath = b.RequestPath,
                OldValuesJson = b.OldValuesJson,
                NewValuesJson = b.NewValuesJson,
                IpAddress = b.IpAddress,
                StatusCode = b.StatusCode,
                IsSuccess = b.IsSuccess,
                ErrorMessage = b.ErrorMessage,
                TimestampUtc = b.TimestampUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResultViewModel<AuditBacklogViewModel>(items, totalCount, page, pageSize);
    }

    public HangfireOverviewViewModel GetHangfireOverview(string? state = null, int limit = 50)
    {
        int clampedLimit = Math.Clamp(limit, 1, 100);
        var monitor = JobStorage.Current.GetMonitoringApi();
        var rawStats = monitor.GetStatistics();

        var statsVm = new HangfireStatsViewModel
        {
            Enqueued = rawStats.Enqueued,
            Scheduled = rawStats.Scheduled,
            Processing = rawStats.Processing,
            Succeeded = rawStats.Succeeded,
            Failed = rawStats.Failed,
            Deleted = rawStats.Deleted,
            ServersCount = rawStats.Servers
        };

        var normalizedState = string.IsNullOrWhiteSpace(state) ? "all" : state.Trim().ToLowerInvariant();
        var jobs = new List<HangfireJobItemViewModel>();
        var seenJobIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void CollectJob(string jobId, string fallbackState, DateTime? timestamp, DateTime? nextExec, string? directError)
        {
            if (string.IsNullOrWhiteSpace(jobId) || !seenJobIds.Add(jobId)) return;

            var details = monitor.JobDetails(jobId);
            string jobType = details?.Job?.Type?.Name ?? "BackgroundJob";
            string? recipientEmail = null;
            string? subject = null;

            if (details?.Job?.Args != null)
            {
                if (details.Job.Args.Count > 0) recipientEmail = details.Job.Args[0]?.ToString();
                if (details.Job.Args.Count > 2) subject = details.Job.Args[2]?.ToString();
            }

            int retryCount = 0;
            if (details?.Properties != null && details.Properties.TryGetValue("RetryCount", out var rcStr))
            {
                int.TryParse(rcStr, out retryCount);
            }

            string? errorMessage = directError;
            DateTime? lastChanged = timestamp;
            if (details?.History != null && details.History.Count > 0)
            {
                var latest = details.History[0];
                lastChanged ??= latest.CreatedAt;
                if (string.IsNullOrWhiteSpace(errorMessage) && latest.Data != null)
                {
                    if (latest.Data.TryGetValue("ExceptionMessage", out var exMsg) && !string.IsNullOrWhiteSpace(exMsg))
                    {
                        errorMessage = exMsg;
                    }
                    else if (!string.IsNullOrWhiteSpace(latest.Reason))
                    {
                        errorMessage = latest.Reason;
                    }
                }
            }

            jobs.Add(new HangfireJobItemViewModel
            {
                JobId = jobId,
                State = fallbackState,
                JobType = jobType,
                RecipientEmail = recipientEmail,
                Subject = subject,
                RetryCount = retryCount,
                CreatedAtUtc = details?.CreatedAt ?? timestamp,
                NextExecutionUtc = nextExec,
                LastStateChangedAtUtc = lastChanged,
                ErrorMessage = errorMessage
            });
        }

        if (normalizedState is "all" or "failed")
        {
            foreach (var f in monitor.FailedJobs(0, clampedLimit))
            {
                CollectJob(f.Key, "Failed", f.Value?.FailedAt, null, f.Value?.ExceptionMessage);
            }
        }

        if (normalizedState is "all" or "scheduled")
        {
            foreach (var s in monitor.ScheduledJobs(0, clampedLimit))
            {
                CollectJob(s.Key, "Scheduled", s.Value?.ScheduledAt, s.Value?.EnqueueAt, null);
            }
        }

        if (normalizedState is "all" or "processing")
        {
            foreach (var p in monitor.ProcessingJobs(0, clampedLimit))
            {
                CollectJob(p.Key, "Processing", p.Value?.StartedAt, null, null);
            }
        }

        if (normalizedState is "all" or "enqueued")
        {
            foreach (var e in monitor.EnqueuedJobs("emails", 0, clampedLimit))
            {
                CollectJob(e.Key, "Enqueued", e.Value?.EnqueuedAt, null, null);
            }
            foreach (var e in monitor.EnqueuedJobs("default", 0, clampedLimit))
            {
                CollectJob(e.Key, "Enqueued", e.Value?.EnqueuedAt, null, null);
            }
        }

        if (normalizedState is "all" or "succeeded")
        {
            foreach (var ok in monitor.SucceededJobs(0, clampedLimit))
            {
                CollectJob(ok.Key, "Succeeded", ok.Value?.SucceededAt, null, null);
            }
        }

        jobs = jobs
            .OrderByDescending(j => j.LastStateChangedAtUtc ?? j.CreatedAtUtc ?? DateTime.MinValue)
            .Take(clampedLimit)
            .ToList();

        return new HangfireOverviewViewModel
        {
            Stats = statsVm,
            Jobs = jobs
        };
    }

    public bool RequeueHangfireJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId)) return false;
        return _backgroundJobClient.Requeue(jobId.Trim());
    }

    public bool DeleteHangfireJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId)) return false;
        return _backgroundJobClient.Delete(jobId.Trim());
    }
}
