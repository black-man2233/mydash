using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MyDash.Application.UseCases;

namespace MyDash.Hub.Api;

public static class AuthApi
{
    public static WebApplication MapAuthApi(this WebApplication app)
    {
        var g = app.MapGroup("/api/auth");

        g.MapPost("/request-pin", async (
            RequestPinHandler handler,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            try
            {
                var result = await handler.Handle(new RequestPin(ip), ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 429);
            }
        }).AllowAnonymous();

        g.MapPost("/verify-pin", async (
            VerifyPinRequest req,
            VerifyPinHandler handler,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await handler.Handle(new VerifyPin(req.ChallengeId, req.Code, ip), ct);
            if (result.Ok)
            {
                var claims = new[] { new Claim(ClaimTypes.Name, "admin") };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            }
            return Results.Ok(result);
        }).AllowAnonymous();

        g.MapPost("/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        }).RequireAuthorization();

        g.MapGet("/me", (HttpContext ctx) =>
        {
            if (ctx.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();
            return Results.Ok(new { name = ctx.User.Identity.Name });
        }).AllowAnonymous();

        return app;
    }
}

public record VerifyPinRequest(string ChallengeId, string Code);
