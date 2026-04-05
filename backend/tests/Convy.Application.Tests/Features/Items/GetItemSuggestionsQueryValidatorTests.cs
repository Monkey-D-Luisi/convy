using Convy.Application.Features.Items.Queries;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Items;

public class GetItemSuggestionsQueryValidatorTests
{
    private readonly GetItemSuggestionsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidDataAndQuery_PassesValidation()
    {
        var query = new GetItemSuggestionsQuery(Guid.NewGuid(), "Milk");
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullQuery_PassesValidation()
    {
        var query = new GetItemSuggestionsQuery(Guid.NewGuid());
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyHouseholdId_FailsValidation()
    {
        var query = new GetItemSuggestionsQuery(Guid.Empty, "Milk");
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.HouseholdId);
    }

    [Fact]
    public void Validate_WithLongQuery_FailsValidation()
    {
        var query = new GetItemSuggestionsQuery(Guid.NewGuid(), new string('a', 201));
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Query);
    }
}
