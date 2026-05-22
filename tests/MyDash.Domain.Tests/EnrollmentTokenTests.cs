using FluentAssertions;
using MyDash.Domain.Entities;

namespace MyDash.Domain.Tests;

public class EnrollmentTokenTests
{
    [Fact]
    public void Create_sets_all_fields()
    {
        var token = EnrollmentToken.Create("myserver", "hash123", ["linux"], TimeSpan.FromMinutes(60));

        token.ServerName.Should().Be("myserver");
        token.TokenHash.Should().Be("hash123");
        token.Tags.Should().Contain("linux");
        token.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(59));
        token.IsConsumed.Should().BeFalse();
        token.IsRevoked.Should().BeFalse();
        token.IsExpired.Should().BeFalse();
        token.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Consume_marks_token_consumed()
    {
        var token = EnrollmentToken.Create("s", "h", [], TimeSpan.FromMinutes(10));
        token.Consume();
        token.IsConsumed.Should().BeTrue();
        token.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Revoke_marks_token_revoked()
    {
        var token = EnrollmentToken.Create("s", "h", [], TimeSpan.FromMinutes(10));
        token.Revoke();
        token.IsRevoked.Should().BeTrue();
        token.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Expired_token_is_not_valid()
    {
        var token = EnrollmentToken.Create("s", "h", [], TimeSpan.FromSeconds(-1));
        token.IsExpired.Should().BeTrue();
        token.IsValid.Should().BeFalse();
    }
}
