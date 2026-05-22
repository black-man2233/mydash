using FluentAssertions;
using MyDash.Domain.Entities;

namespace MyDash.Domain.Tests;

public class PinChallengeTests
{
    [Fact]
    public void Create_sets_all_fields()
    {
        var c = PinChallenge.Create("+31621114421", "hash", TimeSpan.FromMinutes(5));

        c.Id.Should().NotBeEmpty();
        c.PhoneE164.Should().Be("+31621114421");
        c.CodeHash.Should().Be("hash");
        c.IsExpired.Should().BeFalse();
        c.IsConsumed.Should().BeFalse();
        c.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Consume_marks_challenge_consumed()
    {
        var c = PinChallenge.Create("+31621114421", "hash", TimeSpan.FromMinutes(5));
        c.Consume();
        c.IsConsumed.Should().BeTrue();
        c.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Expired_challenge_is_not_valid()
    {
        var c = PinChallenge.Create("+31621114421", "hash", TimeSpan.FromSeconds(-1));
        c.IsExpired.Should().BeTrue();
        c.IsValid.Should().BeFalse();
    }
}
