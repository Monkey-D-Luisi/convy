using Convy.Application.Features.Admin.DTOs;

namespace Convy.Application.Common.Interfaces;

public interface IAdminMetricsReader
{
    Task<AdminOverviewDto> GetOverviewAsync(DateTime now, CancellationToken cancellationToken = default);
    Task<AdminUsageMetricsDto> GetUsageAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<AdminVoiceMetricsDto> GetVoiceAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<AdminOpenAiMetricsDto> GetOpenAiAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<BackupRunDto?> GetLatestBackupAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupRunDto>> GetBackupRunsAsync(int limit, CancellationToken cancellationToken = default);
    Task<AdminSystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default);
}
