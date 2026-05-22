using Grpc.Core;
using Microsoft.Extensions.Options;
using MyDash.Application.Options;
using MyDash.Application.UseCases;
using MyDash.Contracts.Auth.V1;

namespace MyDash.Infrastructure.GrpcServices;

public class AuthGrpcService : AuthService.AuthServiceBase
{
    private readonly RequestPinHandler _requestPin;
    private readonly VerifyPinHandler _verifyPin;

    public AuthGrpcService(RequestPinHandler requestPin, VerifyPinHandler verifyPin)
    {
        _requestPin = requestPin;
        _verifyPin = verifyPin;
    }

    public override async Task<RequestPinResponse> RequestPin(RequestPinRequest request, ServerCallContext context)
    {
        var result = await _requestPin.Handle(new RequestPin(request.ClientIp), context.CancellationToken);
        return new RequestPinResponse
        {
            ChallengeId = result.ChallengeId,
            ExpiresInSeconds = result.ExpiresInSeconds,
            PhoneMasked = result.PhoneMasked,
            Provider = result.Provider,
        };
    }

    public override async Task<VerifyPinResponse> VerifyPin(VerifyPinRequest request, ServerCallContext context)
    {
        var result = await _verifyPin.Handle(
            new VerifyPin(request.ChallengeId, request.Code, request.ClientIp),
            context.CancellationToken);

        return new VerifyPinResponse
        {
            Ok = result.Ok,
            AttemptsRemaining = result.AttemptsRemaining,
            LockedOut = result.LockedOut,
            LockoutSeconds = result.LockoutSeconds,
        };
    }
}
