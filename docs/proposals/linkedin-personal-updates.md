# LinkedIn updates: personal posts on a date, with images

Owner request, 2026-10-28. Imprint already posts an article's LinkedIn text as a company or showcase page when the
article is published (`DueLinkedInPoster`, `LinkedInPost`, `w_organization_social`). The owner writes 4 posts a week
under his own name, with images, planned weeks ahead; LinkedIn's API cannot create a post with a future date, so
something that is always running must send each one at its time. Imprint is that thing. The first real update is
the live test.

**Need:** the owner's own posting plan (13 posts approved for 29 Oct to 19 Nov 2026, in
`~/Hentet/kennel/linkedin/`). Nothing here is for a customer yet; "every Imprint user connects their own LinkedIn"
falls out of the design at no extra cost, but nothing beyond that is built for it.

## What is added

1. **A person as a sender.** A site's LinkedIn sender may be a member (`urn:li:person:{id}`) as well as a page
   (`urn:li:organization:{id}`). The sender record keeps its field name `OrganizationUrn`: it is in stored
   `SiteLinkedInSendersChanged` events, so renaming it would break replay; the domain reads it as "author URN".
2. **Connecting a member.** `/linkedin/connect-member`: OAuth with `openid profile w_member_social` on the member
   app (`Imprint:LinkedInMember`, its own client id and secret: LinkedIn's Community Management API product may have
   to sit alone on its app, so the page app and the member app are configured separately). The callback reads
   `/v2/userinfo`, and the token is stored under the person URN in `secrets/linkedin-members/`, owner-only, never in
   the event stream. Member tokens last about 60 days and self-serve apps get no refresh token: the editor shows
   when each expires, and an update due after its sender's expiry is refused with that reason, not sent.
3. **A LinkedIn update** (`LinkedInUpdate` aggregate, stream `linkedin-update-{id}`): one standalone post on a site:
   sender key, text (at most 3,000 characters), 0 to 9 images (site media assets, each with alt text), and a
   publish time. States: Scheduled, Cancelled, Attempting, Posted (URN), Failed (retryable or not), Unknown,
   Missed. Text, images and time can change only while Scheduled or Missed (rescheduling a missed one makes it
   Scheduled again). The send rules are `LinkedInPost`'s: the attempt is recorded before LinkedIn is called, an
   unknown result is never retried on its own, and a person resolves it with the URN or "not posted".
4. **The scheduler** (`DueLinkedInUpdatePoster`): every minute, each Scheduled update whose time has come is sent;
   one more than `MaxLate` (2 hours) behind is marked Missed instead, so a stopped server never posts last
   Tuesday's text on Friday.
5. **The client:** images go up through the Images API (`initializeUpload`, `PUT` the bytes) as the person, then
   `POST /rest/posts` with `content.media` (one image) or `content.multiImage` (several), commentary escaped as
   LinkedIn's little text format, hashtags as `{hashtag|\#|tag}`.
6. **MCP tools:** `schedule_linkedin_update`, `edit_linkedin_update`, `cancel_linkedin_update`,
   `list_linkedin_updates`, `resolve_linkedin_update`. Approval stays outside Imprint: the owner approves in
   conversation, and only then is an update scheduled.
7. **The editor:** a LinkedIn page per site listing updates by time (state, sender, first line, image count),
   with cancel and resolve; and in site settings, "Connect my LinkedIn" for the signed-in member, showing who is
   connected and until when.

## Added 2026-11-10: page senders and a first comment

Owner request: the company page posts three times a week from 11 Nov 2026, scheduled in Imprint like the owner's
own posts, and the owner's posts must not change at all.

1. **A page as an update's sender.** An update whose sender is `urn:li:organization:{id}` goes out through the page
   connection (`LinkedInPageConnection`: the page app `Imprint:LinkedIn`, the tokens in `secrets/linkedin-token.json`,
   and their refresh), which article posts now share, so the refresh lives in one place and two senders never
   refresh at once. The images are owned by the page, the post's author is the page, and a 401 is answered by one
   refresh and one more try (a 401 proves nothing was posted). A member sender goes out exactly as before: the same
   token store, calls and bodies, pinned byte for byte by `LinkedInUpdateMemberRequestTests`.
2. **A first comment** (`FirstComment`, plain text, at most 1,250 characters, usually the link) posted under the
   update as the same sender once the post is out (`POST /rest/socialActions/{post}/comments`, Comments API, with
   the sender's app's `Linkedin-Version`). It has its own states (Waiting, Attempting, Posted, Failed, Unknown,
   Missed) and events (`linkedin-update.comment-*`), recorded the post's way: the attempt before the call, an
   unknown result never retried, a refusal retried only when LinkedIn says wait. Nothing the comment does touches
   the post: a post that went out stays Posted and is never sent again. A comment that cannot go out within
   `MaxLate` (2 hours) of its post is missed, for a person to add by hand. After a person resolves an unclear post
   as posted, its comment goes out then. The comment rides on `linkedin-update.scheduled` and `.edited` as an
   optional last field, so every update stored before reads with none.
3. **Permissions.** The page app now asks for `w_organization_social w_organization_social_feed`: LinkedIn's Posts
   API names the first, its Comments API names the second for an organization's comments, and the Community
   Management API product grants both. A page connected before this asked only for the first, so a first comment
   needs the page connected again. The member app's request is unchanged.
4. **MCP:** `schedule_linkedin_update` and `edit_linkedin_update` take an optional `firstComment`;
   `list_linkedin_updates` shows it and its state (an update without one reads exactly as before); `get_site`
   reads back the LinkedIn senders, which `set_site_linkedin_senders` replaces whole, with whether an update as
   each would be sent now. **Editor:** the LinkedIn page shows the comment under the preview and where it stands.

## Not added

A calendar of best times, slot rules or a mix per product (they live with the author: `STRATEGI.md` and the
`linkedin` skill), analytics, polls (the owner skipped them, 2026-11-10), comments other than an update's first,
editing a post after it is on LinkedIn.
