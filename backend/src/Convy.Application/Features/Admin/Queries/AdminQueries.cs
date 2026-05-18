using Convy.Application.Common.Models;
using Convy.Application.Features.Admin.DTOs;
using MediatR;

namespace Convy.Application.Features.Admin.Queries;

public record GetAdminOverviewQuery(DateTime Now) : IRequest<Result<AdminOverviewDto>>;

public record GetAdminUsageMetricsQuery(DateOnly From, DateOnly To) : IRequest<Result<AdminUsageMetricsDto>>;

public record GetAdminVoiceMetricsQuery(DateOnly From, DateOnly To) : IRequest<Result<AdminVoiceMetricsDto>>;

public record GetLatestBackupRunQuery : IRequest<Result<BackupRunDto?>>;

public record GetBackupRunsQuery(int Limit) : IRequest<Result<IReadOnlyList<BackupRunDto>>>;

public record GetAdminSystemHealthQuery : IRequest<Result<AdminSystemHealthDto>>;
