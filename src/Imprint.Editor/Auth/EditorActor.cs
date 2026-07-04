using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Imprint.Editor.Auth;

/// <summary>
/// Bridges the per-circuit signed-in identity to the process-wide
/// <c>EventMetadataProvider.ActorSource</c>. That provider is a singleton and cannot see a
/// Blazor circuit's scoped services, so the write path (<see cref="Services.CommandRunner"/>,
/// and the admin widget review) pushes the current user's email onto an
/// <see cref="AsyncLocal{T}"/> immediately before dispatching a command. The ActorSource
/// delegate — wired once in <c>Program.cs</c> — reads it. An AsyncLocal set just before an
/// <c>await</c> flows into the awaited call, so the dispatcher's command scope (and every
/// event it appends) is stamped with the real user; no <c>HttpContext</c> is needed inside
/// the interactive circuit.
///
/// When authentication is disabled the <see cref="AuthenticationStateProvider"/> is absent,
/// the resolved actor is <c>null</c>, and ActorSource falls back to the OS user — preserving
/// the pre-auth behaviour for dev, tests and single-operator installs.
/// </summary>
public sealed class EditorActor(AuthenticationStateProvider? authState = null)
{
    private static readonly AsyncLocal<string?> CurrentActor = new();

    private string? _resolved;
    private bool _hasResolved;

    /// <summary>The actor for the command currently dispatching on this async flow, if any.</summary>
    public static string? Current => CurrentActor.Value;

    /// <summary>
    /// Pushes the signed-in user's email as the ambient actor until the returned scope is
    /// disposed. Resolve the identity once per circuit and cache it — a circuit is one user.
    /// </summary>
    public async ValueTask<IDisposable> BeginScopeAsync()
    {
        var actor = await ResolveAsync();
        var previous = CurrentActor.Value;
        CurrentActor.Value = actor;
        return new Restore(previous);
    }

    private async ValueTask<string?> ResolveAsync()
    {
        if (_hasResolved)
        {
            return _resolved;
        }

        if (authState is not null)
        {
            var user = (await authState.GetAuthenticationStateAsync()).User;
            if (user.Identity?.IsAuthenticated == true)
            {
                // Prefer the verified email (that is what site ownership is keyed on); fall
                // back to the login name so the actor is never blank for an authenticated user.
                _resolved = user.FindFirstValue(ClaimTypes.Email)
                            ?? user.FindFirstValue("email")
                            ?? user.FindFirstValue("preferred_username")
                            ?? user.Identity.Name;
            }
        }

        _hasResolved = true;
        return _resolved;
    }

    private sealed class Restore(string? previous) : IDisposable
    {
        public void Dispose() => CurrentActor.Value = previous;
    }
}
