using Convy.Application.Features.Invites.Queries;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Invites;

public class GetHouseholdInvitesQueryValidatorTests
{
    private readonly GetHouseholdInvitesQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var query = new GetHouseholdInvitesQuery(Guid.NewGuid());
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyHouseholdId_FailsValidation()
    {
        var query = new GetHouseholdInvitesQuery(Guid.Empty);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.HouseholdId);
    }
}
