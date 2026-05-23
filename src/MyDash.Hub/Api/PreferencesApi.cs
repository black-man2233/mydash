using MyDash.Application.Repositories;

namespace MyDash.Hub.Api;

public static class PreferencesApi
{
    public static WebApplication MapPreferencesApi(this WebApplication app)
    {
        var g = app.MapGroup("/api/preferences").RequireAuthorization();

        g.MapGet("/", async (IUserPreferencesRepository repo, CancellationToken ct) =>
        {
            var prefs = await repo.GetAsync(ct);
            return Results.Ok(new
            {
                theme = prefs.Theme,
                accent = prefs.Accent,
                defaultServiceView = prefs.DefaultServiceView,
                density = prefs.Density,
                notifyOnNewPorts = prefs.NotifyOnNewPorts,
                notifyOnDisconnect = prefs.NotifyOnDisconnect,
            });
        });

        g.MapPut("/", async (UpdatePrefsRequest req, IUserPreferencesRepository repo, CancellationToken ct) =>
        {
            var prefs = await repo.GetAsync(ct);
            if (req.Accent is not null) prefs.Accent = req.Accent;
            if (req.DefaultServiceView is not null) prefs.DefaultServiceView = req.DefaultServiceView;
            if (req.NotifyOnNewPorts is not null) prefs.NotifyOnNewPorts = req.NotifyOnNewPorts.Value;
            if (req.NotifyOnDisconnect is not null) prefs.NotifyOnDisconnect = req.NotifyOnDisconnect.Value;
            await repo.UpdateAsync(prefs, ct);
            return Results.Ok(new
            {
                theme = prefs.Theme,
                accent = prefs.Accent,
                defaultServiceView = prefs.DefaultServiceView,
                density = prefs.Density,
                notifyOnNewPorts = prefs.NotifyOnNewPorts,
                notifyOnDisconnect = prefs.NotifyOnDisconnect,
            });
        });

        return app;
    }
}

public record UpdatePrefsRequest(
    string? Accent,
    string? DefaultServiceView,
    bool? NotifyOnNewPorts,
    bool? NotifyOnDisconnect);
