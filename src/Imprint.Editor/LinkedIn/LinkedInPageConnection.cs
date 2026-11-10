namespace Imprint.Editor.LinkedIn;

/// <summary>
/// The page connection: the page app (configuration <c>Imprint:LinkedIn</c>), the tokens a page admin's Connect
/// stored in <see cref="LinkedInTokenStore"/>, and the refresh that keeps them usable. One instance serves both an
/// article's post and a page's scheduled update, so the secret handling lives in one place and two senders never
/// refresh the same token at once.
/// </summary>
public sealed class LinkedInPageConnection(
    HttpClient http, LinkedInOptions app, LinkedInTokenStore tokens, ILogger logger, TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly SemaphoreSlim _refresh = new(1, 1);

    /// <summary>The page app's API address and <c>Linkedin-Version</c>.</summary>
    public LinkedInOptions App => app;

    /// <summary>The page app is set up and a page admin has connected it (the article scheduler's rule).</summary>
    public bool IsConnected => app.HasApp && tokens.Read() is not null;

    /// <summary>Connected, and the connection can still give a token: the access token has a minute left, or the
    /// refresh token has not expired. What an update needs before it is sent as a page.</summary>
    public bool IsUsable
    {
        get
        {
            if (!app.HasApp || tokens.Read() is not { } stored)
            {
                return false;
            }

            var now = _clock.GetUtcNow();
            return stored.AccessExpiresAt > now.AddMinutes(1)
                || (stored.RefreshToken is { Length: > 0 } && (stored.RefreshExpiresAt is null || stored.RefreshExpiresAt > now));
        }
    }

    /// <summary>A usable access token: the stored one while it has a minute left, else a refreshed one. With
    /// <paramref name="rejected"/> (a token LinkedIn just answered 401 to) a fresh one: the stored one if another
    /// sender refreshed it meanwhile, else a refresh. Null when there is no connection or it cannot be refreshed.</summary>
    public async Task<string?> AccessToken(string? rejected, CancellationToken ct)
    {
        if (Usable(tokens.Read(), rejected) is { } quick)
        {
            return quick;
        }

        await _refresh.WaitAsync(ct);
        try
        {
            // Read again inside the gate: the other sender may have refreshed while this one waited.
            if (tokens.Read() is not { } stored)
            {
                return null;
            }

            if (Usable(stored, rejected) is { } fresh)
            {
                return fresh;
            }

            var now = _clock.GetUtcNow();
            if (stored.RefreshToken is not { Length: > 0 } refresh || stored.RefreshExpiresAt is { } dies && dies <= now)
            {
                return null;
            }

            var exchanged = await LinkedInOAuth.Exchange(http, app, new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refresh,
            }, stored.ConnectedAt, now, ct);
            if (exchanged is null)
            {
                logger.LogWarning("LinkedIn token refresh was refused.");
                return null;
            }

            // LinkedIn may not return a new refresh token on refresh: the old one stays valid until it expires.
            var next = exchanged.RefreshToken is null
                ? exchanged with { RefreshToken = stored.RefreshToken, RefreshExpiresAt = stored.RefreshExpiresAt }
                : exchanged;
            tokens.Write(next);
            return next.AccessToken;
        }
        finally
        {
            _refresh.Release();
        }
    }

    // The stored access token when it has a minute left and is not the one LinkedIn just refused.
    private string? Usable(LinkedInTokens? stored, string? rejected) =>
        stored is not null && stored.AccessExpiresAt > _clock.GetUtcNow().AddMinutes(1) && stored.AccessToken != rejected
            ? stored.AccessToken
            : null;
}
