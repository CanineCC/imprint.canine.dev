namespace Imprint.Rendering;

/// <summary>
/// Mutable per-render-pass state cascaded by <see cref="PageView"/>. The first image
/// on a page is almost always the LCP candidate, so it claims eager loading +
/// <c>fetchpriority=high</c> while every later image stays lazy — a class (not a flag
/// parameter) because the claim must be shared across sibling subtrees in document
/// order.
/// </summary>
public sealed class PageRenderState
{
    private bool _firstImageClaimed;

    public bool TryClaimFirstImage()
    {
        if (_firstImageClaimed)
        {
            return false;
        }

        _firstImageClaimed = true;
        return true;
    }
}
