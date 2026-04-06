using System.Text.RegularExpressions;
using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public class ParseVoiceInputCommandHandler : IRequestHandler<ParseVoiceInputCommand, Result<List<ParsedItemDto>>>
{
    private static readonly HashSet<string> KnownUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "kg", "g", "lb", "lbs", "oz", "l", "ml", "liters", "litros", "litres",
        "cups", "cup", "tbsp", "tsp", "packs", "pack", "bags", "bag",
        "bottles", "bottle", "cans", "can", "boxes", "box", "units", "unit",
        "pieces", "piece", "dozen", "docena", "kilos", "kilo"
    };

    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;

    public ParseVoiceInputCommandHandler(
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser)
    {
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<List<ParsedItemDto>>> Handle(ParseVoiceInputCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken);
        if (list is null)
            return Result<List<ParsedItemDto>>.Failure(Error.NotFound("List not found."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result<List<ParsedItemDto>>.Failure(Error.Forbidden("You are not a member of this household."));

        var items = ParseText(request.TranscribedText);
        return Result<List<ParsedItemDto>>.Success(items);
    }

    private static List<ParsedItemDto> ParseText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var segments = Regex.Split(text.Trim(), @"\s*[,;]\s*|\s+(?:and|y)\s+", RegexOptions.IgnoreCase)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        var result = new List<ParsedItemDto>();
        foreach (var segment in segments)
        {
            var parsed = ParseSegment(segment);
            if (parsed is not null)
                result.Add(parsed);
        }

        return result;
    }

    private static ParsedItemDto? ParseSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
            return null;

        var match = Regex.Match(segment, @"^(\d+)\s*([a-zA-Z\u00e1\u00e9\u00ed\u00f3\u00fa\u00c1\u00c9\u00cd\u00d3\u00da\u00f1\u00d1]*)\s*(?:of|de)?\s*(.+)$", RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var quantity = int.Parse(match.Groups[1].Value);
            var unitCandidate = match.Groups[2].Value.Trim();
            var titleCandidate = match.Groups[3].Value.Trim();

            if (!string.IsNullOrEmpty(unitCandidate) && KnownUnits.Contains(unitCandidate))
            {
                return new ParsedItemDto(
                    string.IsNullOrWhiteSpace(titleCandidate) ? unitCandidate : titleCandidate,
                    quantity,
                    unitCandidate);
            }

            var title = string.IsNullOrWhiteSpace(titleCandidate) ? unitCandidate : $"{unitCandidate} {titleCandidate}".Trim();
            return new ParsedItemDto(title, quantity, null);
        }

        return new ParsedItemDto(segment, null, null);
    }
}
