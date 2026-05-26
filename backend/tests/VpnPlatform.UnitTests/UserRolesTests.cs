using VpnPlatform.Application.Common;
using Xunit;

namespace VpnPlatform.UnitTests;

public class UserRolesTests
{
    [Fact]
    public void Parse_Should_Default_To_User_When_Roles_Are_Empty()
    {
        var roles = UserRoles.Parse("");

        Assert.Contains(UserRoles.User, roles);
        Assert.Single(roles);
    }

    [Fact]
    public void NormalizeCsv_Should_Remove_Unknown_And_Duplicate_Roles()
    {
        var roles = UserRoles.NormalizeCsv("SuperAdmin,Unknown,SuperAdmin,SupportAgent");

        Assert.Equal("SuperAdmin,SupportAgent", roles);
    }
}
