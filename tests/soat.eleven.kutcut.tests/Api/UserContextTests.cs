using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using soat.eleven.kutcut.core.api.Security;

namespace soat.eleven.kutcut.tests.Api;

public class UserContextTests
{
    private static IHttpContextAccessor BuildAccessor(ClaimsPrincipal? principal)
    {
        var httpContext = new DefaultHttpContext();
        if (principal is not null)
            httpContext.User = principal;

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(httpContext);
        return accessor.Object;
    }

    [Fact]
    public void UserContext_WithNameIdentifierClaim_IsAuthenticated()
    {
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var ctx = new UserContext(BuildAccessor(principal));

        ctx.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void UserContext_WithNameIdentifierClaim_HasCorrectUserId()
    {
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var ctx = new UserContext(BuildAccessor(principal));

        ctx.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserContext_WithSubClaim_IsAuthenticatedAndHasCorrectUserId()
    {
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim("sub", userId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var ctx = new UserContext(BuildAccessor(principal));

        ctx.IsAuthenticated.Should().BeTrue();
        ctx.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserContext_WithNoClaims_IsNotAuthenticated()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var ctx = new UserContext(BuildAccessor(principal));

        ctx.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void UserContext_WithNoClaims_UserIdIsEmpty()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var ctx = new UserContext(BuildAccessor(principal));

        ctx.UserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void UserContext_WithInvalidGuidClaim_IsNotAuthenticated()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var ctx = new UserContext(BuildAccessor(principal));

        ctx.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void UserContext_WithInvalidGuidClaim_UserIdIsEmpty()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var ctx = new UserContext(BuildAccessor(principal));

        ctx.UserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void UserContext_WithNullHttpContext_IsNotAuthenticated()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var ctx = new UserContext(accessor.Object);

        ctx.IsAuthenticated.Should().BeFalse();
        ctx.UserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void UserContext_NameIdentifierTakesPrecedenceOverSub()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId1.ToString()),
            new Claim("sub", userId2.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var ctx = new UserContext(BuildAccessor(principal));

        ctx.UserId.Should().Be(userId1);
    }

    [Fact]
    public void UserContext_EmptyStringGuid_IsNotAuthenticated()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, string.Empty) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var ctx = new UserContext(BuildAccessor(principal));

        ctx.IsAuthenticated.Should().BeFalse();
    }
}
