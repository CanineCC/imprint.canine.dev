# Imprint — Code Conventions

The point of a reference project is that every file teaches. These conventions are
enforced by `TreatWarningsAsErrors`, analyzers, and code review.

## Solution-wide

- .NET 10, `LangVersion latest`, nullable enabled, implicit usings on,
  `TreatWarningsAsErrors=true` — all via root `Directory.Build.props`.
- Central package management (`Directory.Packages.props`). Runtime dependency
  allowlist: `Microsoft.Data.Sqlite`, `SkiaSharp` (media only, MIT) — that's it.
  Anything ASP.NET/Blazor comes from the framework reference. Tests may add
  xunit.v3, Microsoft.Playwright. **Adding any other package is an architecture
  decision, not a convenience.**
- No reflection magic beyond the two sanctioned scans (event registry, handler/
  projection registration), both in `Imprint.EventSourcing` and both explained by
  comments at the scan site.
- File-scoped namespaces; one public type per file (records that form a closed union,
  e.g. `Node` subtypes or an aggregate's events, may share a file — the union *is* the
  concept). `sealed` by default.
- XML doc comments on every public type in `Imprint.EventSourcing` (it reads as a
  library); elsewhere, comments explain *why* (invariants, races, trade-offs), never
  *what*.

## Naming

- Commands: imperative verb phrases (`MoveNode`, not `NodeMoveCommand`); the record is
  the message: `public sealed record MoveNode(PageId PageId, NodeId NodeId, NodeId NewParentId, int NewIndex) : ICommand;`
- Events: past tense (`NodeMoved`); handlers `MoveNodeHandler`; slices live in
  `Features/<Area>/<UseCase>/` with `<UseCase>.cs` (command + validator) and
  `<UseCase>Handler.cs`.
- Projections: noun read models (`PageList`), file pairs `<Name>Projection.cs`
  (event folding) + `<Name>` (the immutable snapshot types).
- CSS: published-site classes `ip-*` (+ custom props `--ip-*`); editor chrome `ed-*`
  (+ `--ed-*`). They must never mix.

## Slice shape (the golden path)

```csharp
// Features/Pages/MoveNode/MoveNode.cs
public sealed record MoveNode(PageId PageId, NodeId NodeId, NodeId NewParentId, int NewIndex) : ICommand;

// Features/Pages/MoveNode/MoveNodeHandler.cs
public sealed class MoveNodeHandler(IAggregateStore store) : ICommandHandler<MoveNode>
{
    public async Task<Result> Handle(MoveNode cmd, CancellationToken ct)
    {
        var page = await store.Load<Page>(cmd.PageId.Stream, ct);
        page.MoveNode(cmd.NodeId, cmd.NewParentId, cmd.NewIndex);
        await store.Save(page, ct);
        return Result.Ok();
    }
}
```

Handlers stay this thin. Validation of *data shape* (ranges, formats) lives in the
command's `Validate()` (from `IValidatableCommand`); *invariants* live in the
aggregate; *cross-aggregate checks* consult read models inside the handler with a
comment naming the accepted race. `DomainException` → failed `Result` happens in the
dispatcher — handlers don't try/catch.

## Testing

- xunit.v3. Naming: `Behavior_condition_outcome`
  (`MoveNode_into_own_descendant_is_rejected`).
- Aggregate tests: pure, no store —
  `AggregateSpec.For<Page>().Given(created, nodeAdded).When(p => p.MoveNode(…)).ThenRaised(new NodeMoved(…))`.
- Slice tests: `AuthoringTestHost` (real SQLite in-memory store, real dispatcher, real
  projections) — dispatch, then assert events and read models.
- Every event type: serialization round-trip via the registry (one reflective theory
  covers all; adding an event without registering fails the test).
- JS: `rich-toolbar.js#normalize` and slot geometry get node-free unit tests executed
  via Playwright's page context in E2E (no separate JS test runner — one less
  toolchain).
- E2E: Playwright .NET, chromium, drives the real editor against a temp data dir;
  publishes; asserts on the static output (the §Performance budget included).

## Git

- Conventional commits (`feat:`, `fix:`, `docs:`, `test:`, `refactor:`).
- `main` stays green: build + tests pass at every commit.
