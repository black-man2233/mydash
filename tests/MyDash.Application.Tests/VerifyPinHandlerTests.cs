using FluentAssertions;
using Microsoft.Extensions.Options;
using MyDash.Application.Options;
using MyDash.Application.Repositories;
using MyDash.Application.Services;
using MyDash.Application.UseCases;
using MyDash.Domain.Entities;
using NSubstitute;

namespace MyDash.Application.Tests;

public class VerifyPinHandlerTests
{
    private readonly IPinChallengeRepository _challenges = Substitute.For<IPinChallengeRepository>();
    private readonly IAuditWriter _audit = Substitute.For<IAuditWriter>();
    private readonly VerifyPinHandler _sut;

    public VerifyPinHandlerTests()
    {
        _sut = new VerifyPinHandler(
            _challenges, _audit,
            Microsoft.Extensions.Options.Options.Create(new SecurityOptions { MaxFailedAttempts = 3, LockoutSeconds = 900 }));
    }

    [Fact]
    public async Task Returns_not_ok_for_invalid_challenge_id()
    {
        var result = await _sut.Handle(new VerifyPin("not-a-guid", "123456", "1.2.3.4"), default);
        result.Ok.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_not_ok_when_challenge_not_found()
    {
        var id = Guid.NewGuid();
        _challenges.GetByIdAsync(id, default).Returns((PinChallenge?)null);

        var result = await _sut.Handle(new VerifyPin(id.ToString(), "123456", "1.2.3.4"), default);
        result.Ok.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_ok_with_correct_code()
    {
        var code = "999999";
        var hash = BCrypt.Net.BCrypt.HashPassword(code);
        var challenge = PinChallenge.Create("+31621114421", hash, TimeSpan.FromMinutes(5));
        _challenges.GetByIdAsync(challenge.Id, default).Returns(challenge);

        var result = await _sut.Handle(new VerifyPin(challenge.Id.ToString(), code, "1.2.3.4"), default);

        result.Ok.Should().BeTrue();
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditEntry>(a => a.Action == AuditAction.PinVerified),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_attempts_remaining_on_wrong_code()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("correct");
        var challenge = PinChallenge.Create("+31621114421", hash, TimeSpan.FromMinutes(5));
        _challenges.GetByIdAsync(challenge.Id, default).Returns(challenge);

        var result = await _sut.Handle(new VerifyPin(challenge.Id.ToString(), "wrong1", "1.2.3.4"), default);

        result.Ok.Should().BeFalse();
        result.AttemptsRemaining.Should().Be(2);
    }
}
