using Convy.Application.Features.Items.Queries;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Items;

public class CheckDuplicateItemQueryValidatorTests
{
    private readonly CheckDuplicateItemQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var query = new CheckDuplicateItemQuery(Guid.NewGuid(), "Milk");
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyListId_FailsValidation()
    {
        var query = new CheckDuplicateItemQuery(Guid.Empty, "Milk");
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.ListId);
    }

    [Fact]
    public void Validate_WithEmptyTitle_FailsValidation()
    {
        var query = new CheckDuplicateItemQuery(Guid.NewGuid(), "");
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WithLongTitle_FailsValidation()
    {
        var query = new CheckDuplicateItemQuery(Guid.NewGuid(), new string('a', 201));
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }
}
