namespace Imprint.Editor.Auth;

/// <summary>
/// What one person may do with one post. A site is not the only unit of access: a reviewer is
/// handed a single document and asked to clear it, which is a smaller thing than "may edit this
/// site" and was previously unrepresentable — so the reviewer was refused at the door of the very
/// page their mail linked to.
/// </summary>
public enum PostPass
{
    /// <summary>Not theirs to open. The page redirects, as it does for any inaccessible site.</summary>
    None,

    /// <summary>
    /// The reviewer's pass on a post that has been handed to them: read it, set or change the
    /// date, approve it or send it back. Deliberately NOT editing — a reviewer who could rewrite
    /// the post would be approving their own words, which is the one thing review is for.
    /// </summary>
    Review,

    /// <summary>Full editorial control: the site's owner or one of its collaborators.</summary>
    Edit,
}
