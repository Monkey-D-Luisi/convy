using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Admin.DTOs;
using MediatR;

namespace Convy.Application.Features.Admin.Queries;

public class GetAdminOverviewQueryHandler : IRequestHandler<GetAdminOverviewQuery, Result<AdminOverviewDto>>
{
    private readonly IAdminMetricsReader _metricsReader;

    public GetAdminOverviewQueryHandler(IAdminMetricsReader metricsReader)
    {
        _metricsReader = metricsReader;
    }

    public async Task<Result<AdminOverviewDto>> Handle(GetAdminOverviewQuery request, CancellationToken cancellationToken)
    {
        var overview = await _metricsReader.GetOverviewAsync(request.Now, cancellationToken);
        return Result<AdminOverviewDto>.Success(overview);
    }
}

public class GetAdminUsageMetricsQueryHandler : IRequestHandler<GetAdminUsageMetricsQuery, Result<AdminUsageMetricsDto>>
{
    private readonly IAdminMetricsReader _metricsReader;

    public GetAdminUsageMetricsQueryHandler(IAdminMetricsReader metricsReader)
    {
        _metricsReader = metricsReader;
    }

    public async Task<Result<AdminUsageMetricsDto>> Handle(GetAdminUsageMetricsQuery request, CancellationToken cancellationToken)
    {
        if (request.To < request.From)
            return Result<AdminUsageMetricsDto>.Failure(Error.Validation("The end date must be on or after the start date."));

        var usage = await _metricsReader.GetUsageAsync(request.From, request.To, cancellationToken);
        return Result<AdminUsageMetricsDto>.Success(usage);
    }
}

public class GetAdminVoiceMetricsQueryHandler : IRequestHandler<GetAdminVoiceMetricsQuery, Result<AdminVoiceMetricsDto>>
{
    private readonly IAdminMetricsReader _metricsReader;

    public GetAdminVoiceMetricsQueryHandler(IAdminMetricsReader metricsReader)
    {
        _metricsReader = metricsReader;
    }

    public async Task<Result<AdminVoiceMetricsDto>> Handle(GetAdminVoiceMetricsQuery request, CancellationToken cancellationToken)
    {
        if (request.To < request.From)
            return Result<AdminVoiceMetricsDto>.Failure(Error.Validation("The end date must be on or after the start date."));

        var voice = await _metricsReader.GetVoiceAsync(request.From, request.To, cancellationToken);
        return Result<AdminVoiceMetricsDto>.Success(voice);
    }
}

public class GetLatestBackupRunQueryHandler : IRequestHandler<GetLatestBackupRunQuery, Result<BackupRunDto?>>
{
    private readonly IAdminMetricsReader _metricsReader;

    public GetLatestBackupRunQueryHandler(IAdminMetricsReader metricsReader)
    {
        _metricsReader = metricsReader;
    }

    public async Task<Result<BackupRunDto?>> Handle(GetLatestBackupRunQuery request, CancellationToken cancellationToken)
    {
        var backup = await _metricsReader.GetLatestBackupAsync(cancellationToken);
        return Result<BackupRunDto?>.Success(backup);
    }
}

public class GetAdminOpenAiMetricsQueryHandler : IRequestHandler<GetAdminOpenAiMetricsQuery, Result<AdminOpenAiMetricsDto>>
{
    private readonly IAdminMetricsReader _metricsReader;

    public GetAdminOpenAiMetricsQueryHandler(IAdminMetricsReader metricsReader)
    {
        _metricsReader = metricsReader;
    }

    public async Task<Result<AdminOpenAiMetricsDto>> Handle(GetAdminOpenAiMetricsQuery request, CancellationToken cancellationToken)
    {
        if (request.To < request.From)
            return Result<AdminOpenAiMetricsDto>.Failure(Error.Validation("The end date must be on or after the start date."));

        var usage = await _metricsReader.GetOpenAiAsync(request.From, request.To, cancellationToken);
        return Result<AdminOpenAiMetricsDto>.Success(usage);
    }
}

public class GetBackupRunsQueryHandler : IRequestHandler<GetBackupRunsQuery, Result<IReadOnlyList<BackupRunDto>>>
{
    private readonly IAdminMetricsReader _metricsReader;

    public GetBackupRunsQueryHandler(IAdminMetricsReader metricsReader)
    {
        _metricsReader = metricsReader;
    }

    public async Task<Result<IReadOnlyList<BackupRunDto>>> Handle(GetBackupRunsQuery request, CancellationToken cancellationToken)
    {
        if (request.Limit is < 1 or > 100)
            return Result<IReadOnlyList<BackupRunDto>>.Failure(Error.Validation("Limit must be between 1 and 100."));

        var backups = await _metricsReader.GetBackupRunsAsync(request.Limit, cancellationToken);
        return Result<IReadOnlyList<BackupRunDto>>.Success(backups);
    }
}

public class GetAdminSystemHealthQueryHandler : IRequestHandler<GetAdminSystemHealthQuery, Result<AdminSystemHealthDto>>
{
    private readonly IAdminMetricsReader _metricsReader;

    public GetAdminSystemHealthQueryHandler(IAdminMetricsReader metricsReader)
    {
        _metricsReader = metricsReader;
    }

    public async Task<Result<AdminSystemHealthDto>> Handle(GetAdminSystemHealthQuery request, CancellationToken cancellationToken)
    {
        var health = await _metricsReader.GetSystemHealthAsync(cancellationToken);
        return Result<AdminSystemHealthDto>.Success(health);
    }
}
