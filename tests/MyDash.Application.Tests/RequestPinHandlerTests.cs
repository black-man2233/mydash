using FluentAssertions;
using Microsoft.Extensions.Options;
using MyDash.Application.Options;
using MyDash.Application.Repositories;
using MyDash.Application.Services;
using MyDash.Application.UseCases;
using MyDash.Domain.Entities;
using NSubstitute;

namespace MyDash.Application.Tests;

public class RequestPinHandlerTests
{
    private readonly IPinChallengeRepository _challenges = Substitute.For<IPinChallengeRepository>();
    private readonly ISmsSender _sms = Substitute.For<ISmsSender>();
    private readonly IAuditWriter _audit = Substitute.For<IAuditWriter>();
    private readonly RequestPinHandler _sut;

    public RequestPinHandlerTests()
    {
        _sms.ProviderName.Returns("TextBelt");
        _sms.SendAsync(default!, default!, default)
            .ReturnsForAnyArgs(new SmsSendResult(true, "id-1", null));
        _challenges.CountRecentByIpAsync(default!, default, default)
            .ReturnsForAnyArgs(0);

        _sut = new RequestPinHandler(
            _challenges, _sms, _audit,
            Microsoft.Extensions.Options.Options.Create(new SmsOptions { PhoneE164 = "+31621114421", From = "MyDash", ApiKey = "textbelt" }),
            Microsoft.Extensions.Options.Options.Create(new SecurityOptions { PinTtlSeconds = 300 }));
    }

    [Fact]
    public async Task Issues_challenge_and_sends_sms()
    {
        var result = await _sut.Handle(new RequestPin("1.2.3.4"), CancellationToken.None);

        result.ChallengeId.Should().NotBeNullOrEmpty();
        result.Provider.Should().Be("TextBelt");
        result.ExpiresInSeconds.Should().Be(300);
        result.PhoneMasked.Should().Contain("•••");

        await _challenges.Received(1).AddAsync(Arg.Any<PinChallenge>(), Arg.Any<CancellationToken>());
        await _sms.Received(1).SendAsync("+31621114421",
            Arg.Is<string>(b => b.Contains("MyDash")),
            Arg.Any<CancellationToken>());
        await _audit.Received(1).WriteAsync(
            Arg.Is<AuditEntry>(a => a.Action == AuditAction.PinRequested),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_rate_limit_exceeded()
    {
        _challenges.CountRecentByIpAsync(default!, default, default).ReturnsForAnyArgs(5);

        await FluentActions.Invoking(() => _sut.Handle(new RequestPin("1.2.3.4"), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Rate limit*");
    }
}
