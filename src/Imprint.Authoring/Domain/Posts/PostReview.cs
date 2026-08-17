namespace Imprint.Authoring.Domain.Posts;

/// <summary>
/// Where a post stands with the person who has to clear it before the world reads it.
/// <para>Only the post's OWN state lives here. Whether a review is required at all is the
/// site's policy (a configured reviewer), checked where cross-aggregate rules belong — in the
/// slice — so a post carried between sites, or written before a reviewer existed, is never
/// retroactively "unreviewed".</para>
/// </summary>
public enum PostReview
{
    /// <summary>Never submitted. Every post starts here, and posts on sites with no reviewer stay here.</summary>
    None,

    /// <summary>Submitted and waiting on the reviewer.</summary>
    Pending,

    /// <summary>Sent back with a reason. The author's move.</summary>
    ChangesRequested,

    /// <summary>Cleared. Holds only while the words stay as they were cleared (see <c>post.approval-lapsed</c>).</summary>
    Approved,
}
