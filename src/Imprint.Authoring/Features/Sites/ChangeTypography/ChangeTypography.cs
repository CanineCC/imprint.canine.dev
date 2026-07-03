using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.ChangeTypography;

// Typography is a whole value object on purpose: its options are chosen together in
// the theme editor, and the aggregate validates the ranges as one unit.
public sealed record ChangeTypography(SiteId SiteId, Typography Typography) : ICommand;
