using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.SetPostControlNotes;

/// <param name="Notes">The private publication-control notes, or null/blank to clear them.</param>
public sealed record SetPostControlNotes(PostId PostId, string? Notes) : ICommand;
