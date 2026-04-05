using Convy.Application.Features.Activity.Queries;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Activity;

public class GetHouseholdActivityQueryValidatorTests
{
    private readonly GetHouseholdActivityQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var query = new GetHouseholdActivityQuery(Guid.NewGuid(), 50);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithDefaultLimit_PassesValidation()
    {
        var query = new GetHouseholdActivityQuery(Guid.NewGuid());
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyHouseholdId_FailsValidation()
    {
        var query = new GetHouseholdActivityQuery(Guid.Empty, 50);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.HouseholdId);
    }

    [Fact]
    public void Validate_WithZeroLimit_FailsValidation()
    {
        var query = new GetHouseholdActivityQuery(Guid.NewGuid(), 0);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Limit);
    }

    [Fact]
    public void Validate_WithNegativeLimit_FailsValidation()
    {
        var query = new GetHouseholdActivityQuery(Guid.NewGuid(), -1);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Limit);
    }

    [Fact]
    public void Validate_WithLimitExceeding200_FailsValidation()
    {
        var query = new GetHouseholdActivityQuery(Guid.NewGuid(), 201);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Limit);
    }

    [Fact]
    public void Validate_WithLimit200_PassesValidation()
    {
        var query = new GetHouseholdActivityQuery(Guid.NewGuid(), 200);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor(x => x.Limit);
    }
}
