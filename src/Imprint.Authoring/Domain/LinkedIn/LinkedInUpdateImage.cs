namespace Imprint.Authoring.Domain.LinkedIn;

/// <summary>An image on a LinkedIn update: a media asset and the alt text LinkedIn shows to people who cannot see it.</summary>
public sealed record LinkedInUpdateImage(AssetId AssetId, string Alt);
