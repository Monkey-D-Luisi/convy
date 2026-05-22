using Convy.Application.Common.Models;
using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Admin.Queries;
using MediatR;

namespace Convy.API.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/metrics/overview", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetAdminOverviewQuery(DateTime.UtcNow));
            return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.Error!);
        });

        group.MapGet("/metrics/usage", async (DateOnly? from, DateOnly? to, IMediator mediator) =>
        {
            var (rangeFrom, rangeTo) = ResolveDateRange(from, to);
            var result = await mediator.Send(new GetAdminUsageMetricsQuery(rangeFrom, rangeTo));
            return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.Error!);
        });

        group.MapGet("/metrics/voice", async (DateOnly? from, DateOnly? to, IMediator mediator) =>
        {
            var (rangeFrom, rangeTo) = ResolveDateRange(from, to);
            var result = await mediator.Send(new GetAdminVoiceMetricsQuery(rangeFrom, rangeTo));
            return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.Error!);
        });

        group.MapGet("/metrics/openai", async (DateOnly? from, DateOnly? to, IMediator mediator) =>
        {
            var (rangeFrom, rangeTo) = ResolveDateRange(from, to);
            var result = await mediator.Send(new GetAdminOpenAiMetricsQuery(rangeFrom, rangeTo));
            return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.Error!);
        });

        group.MapGet("/backups/latest", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetLatestBackupRunQuery());
            return result.IsSuccess ? Results.Json(result.Value) : MapError(result.Error!);
        });

        group.MapGet("/backups/runs", async (int? limit, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetBackupRunsQuery(limit ?? 30));
            return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.Error!);
        });

        group.MapGet("/backups/runs/{id:guid}/download", async (Guid id, IAdminBackupFileService backupFileService, CancellationToken cancellationToken) =>
        {
            var download = await backupFileService.OpenDownloadAsync(id, cancellationToken);
            if (download is null)
                return Results.NotFound();

            return Results.File(
                download.Content,
                download.ContentType,
                download.FileName,
                lastModified: null,
                entityTag: null,
                enableRangeProcessing: true);
        });

        group.MapGet("/system/health", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetAdminSystemHealthQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.Error!);
        });
    }

    private static (DateOnly From, DateOnly To) ResolveDateRange(DateOnly? from, DateOnly? to)
    {
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? end.AddDays(-29);
        return (start, end);
    }

    private static IResult MapError(Error error) => error.Code switch
    {
        "Validation" => Results.BadRequest(error),
        _ => Results.Problem(error.Message, statusCode: 500)
    };
}
