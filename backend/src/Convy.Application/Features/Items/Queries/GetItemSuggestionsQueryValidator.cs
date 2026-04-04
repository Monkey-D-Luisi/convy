using FluentValidation;

namespace Convy.Application.Features.Items.Queries;

public class GetItemSuggestionsQueryValidator : AbstractValidator<GetItemSuggestionsQuery>
{
    public GetItemSuggestionsQueryValidator()
    {
        RuleFor(x => x.HouseholdId)
            .NotEmpty().WithMessage("Household ID is required.");

        RuleFor(x => x.Query)
            .MaximumLength(200).When(x => x.Query is not null)
            .WithMessage("Query must not exceed 200 characters.");
    }
}
