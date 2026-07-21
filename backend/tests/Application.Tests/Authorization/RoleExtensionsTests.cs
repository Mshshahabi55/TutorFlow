using TutorFlow.Application.Authorization;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Authorization;

public class RoleExtensionsTests
{
    [Theory]
    [InlineData("Student", Role.Student)]
    [InlineData("Tutor", Role.Tutor)]
    [InlineData("ParentGuardian", Role.ParentGuardian)]
    [InlineData("AdminStaff", Role.AdminStaff)]
    public void ToRole_parses_every_role_string_LoginCommandHandler_produces(string roleName, Role expected)
    {
        Assert.Equal(expected, roleName.ToRole());
    }

    [Fact]
    public void ToRole_is_null_for_null()
    {
        Assert.Null(((string?)null).ToRole());
    }

    [Theory]
    [InlineData("")]
    [InlineData("student")]
    [InlineData("Administrator")]
    public void ToRole_is_null_for_an_unrecognized_or_wrongly_cased_value(string roleName)
    {
        Assert.Null(roleName.ToRole());
    }
}
