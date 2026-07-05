using System.Security.Claims;
using Imprint.Editor.Auth;
using Imprint.Editor.Services;
using Imprint.EventSourcing;
using Microsoft.AspNetCore.Components.Authorization;

namespace Imprint.Editor.Tests;

/// <summary>
/// Pins the identity → event-metadata bridge. The original BeginScopeAsync set the
/// AsyncLocal <em>inside an async method</em>, which confines the write to that method's
/// own execution context — so every production event was silently stamped with the OS
/// user instead of the signed-in email (ownership claims "did nothing"). The actor must
/// be observable from within the dispatcher call itself, which is where ActorSource reads it.
/// </summary>
public sealed class EditorActorFlowTests
{
    private sealed record Cmd : ICommand;

    private sealed class SignedIn(string email) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Email, email)], authenticationType: "test"))));
    }

    private sealed class ActorCapturingDispatcher : ICommandDispatcher
    {
        public string? SeenActor { get; private set; }

        public Task<Result> Dispatch(ICommand command, CancellationToken ct = default)
        {
            SeenActor = EditorActor.Current;
            return Task.FromResult(Result.Ok());
        }
    }

    [Fact]
    public async Task The_signed_in_email_is_the_ambient_actor_during_dispatch()
    {
        var dispatcher = new ActorCapturingDispatcher();
        var runner = new CommandRunner(
            dispatcher, new ToastService(), new EditorActor(new SignedIn("alice@example.com")));

        await runner.Run(new Cmd());

        Assert.Equal("alice@example.com", dispatcher.SeenActor);
    }

    [Fact]
    public async Task The_ambient_actor_is_restored_after_dispatch()
    {
        var runner = new CommandRunner(
            new ActorCapturingDispatcher(), new ToastService(), new EditorActor(new SignedIn("alice@example.com")));

        await runner.Run(new Cmd());

        Assert.Null(EditorActor.Current);
    }

    [Fact]
    public async Task Without_auth_the_actor_stays_null_so_the_OS_user_fallback_applies()
    {
        var dispatcher = new ActorCapturingDispatcher();
        var runner = new CommandRunner(dispatcher, new ToastService(), new EditorActor(authState: null));

        await runner.Run(new Cmd());

        Assert.Null(dispatcher.SeenActor);
    }
}
