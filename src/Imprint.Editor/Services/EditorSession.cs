using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;

namespace Imprint.Editor.Services;

public enum EditorPanel { Pages, Layers, Blocks, Assets, Theme, Translations }

public enum CanvasViewport { Desktop, Tablet, Phone }

/// <summary>
/// Per-circuit editor state: which page is open, what is selected, which locale is
/// being edited, viewport preview, panel choice, inline-edit state. Components read
/// this and subscribe to <see cref="Changed"/>; every mutation notifies. Domain state
/// lives in read models — this is UI state only.
/// </summary>
public sealed class EditorSession(PageDrafts drafts, SiteOverview site) : IDisposable
{
    public event Action? Changed;

    public PageId? CurrentPageId { get; private set; }
    public NodeId? Selection { get; private set; }
    public NodeId? EditingNodeId { get; private set; }
    public EditorPanel Panel { get; private set; } = EditorPanel.Pages;
    public bool PanelCollapsed { get; private set; }
    public CanvasViewport Viewport { get; private set; } = CanvasViewport.Desktop;
    public bool CanvasDark { get; private set; }

    private Locale? _editLocale;

    /// <summary>The locale the canvas renders and inline edits write to. Defaults to the site default.</summary>
    public Locale EditLocale
    {
        get => _editLocale ?? ActiveSite?.DefaultLocale ?? new Locale("en");
        set
        {
            _editLocale = value;
            Notify();
        }
    }

    public Page? CurrentPage =>
        CurrentPageId is { } id ? drafts.Get(id) : null;

    /// <summary>
    /// The site currently being edited: the open page's owning site, falling back to the
    /// first site when nothing is open yet (fresh load, between pages). Everything that
    /// belongs to the edited site — its locales, theme, navigation, default locale — must
    /// read THIS, not <see cref="SiteOverview.Current"/>, so opening a page from any
    /// site's dashboard card edits <em>that</em> site, not always the first one.
    /// </summary>
    public Site? ActiveSite =>
        (CurrentPage?.SiteId is { } siteId ? site.Get(siteId) : null) ?? site.Current;

    public SiteId? ActiveSiteId => ActiveSite?.Id;

    public Node? SelectedNode =>
        Selection is { } nodeId ? CurrentPage?.Tree.Find(nodeId) : null;

    public string ViewportWidth => Viewport switch
    {
        CanvasViewport.Phone => "390px",
        CanvasViewport.Tablet => "768px",
        _ => "100%",
    };

    public void OpenPage(PageId id)
    {
        if (CurrentPageId == id)
        {
            return;
        }

        CurrentPageId = id;
        Selection = null;
        EditingNodeId = null;
        Notify();
    }

    public void Select(NodeId? nodeId)
    {
        if (Selection == nodeId)
        {
            return;
        }

        Selection = nodeId;
        Notify();
    }

    /// <summary>Esc semantics: node → parent → clear (docs/editor-ux.md §2).</summary>
    public void SelectParent()
    {
        if (Selection is not { } current || CurrentPage is not { } page || !page.Tree.Contains(current))
        {
            Select(null);
            return;
        }

        Select(page.Tree.ParentOf(current)?.Id);
    }

    /// <summary>Ancestor path of the selection, root-first — the breadcrumb.</summary>
    public IReadOnlyList<Node> SelectionPath()
    {
        if (SelectedNode is not { } node || CurrentPage is not { } page)
        {
            return [];
        }

        var path = new List<Node> { node };
        for (var parent = page.Tree.ParentOf(node.Id); parent is not null; parent = page.Tree.ParentOf(parent.Id))
        {
            path.Insert(0, parent);
        }

        return path;
    }

    public void BeginInlineEdit(NodeId nodeId)
    {
        EditingNodeId = nodeId;
        Notify();
    }

    public void EndInlineEdit()
    {
        EditingNodeId = null;
        Notify();
    }

    public void ShowPanel(EditorPanel panel)
    {
        // Clicking the active rail icon toggles the panel away — more canvas space.
        if (Panel == panel && !PanelCollapsed)
        {
            PanelCollapsed = true;
        }
        else
        {
            Panel = panel;
            PanelCollapsed = false;
        }

        Notify();
    }

    public void SetViewport(CanvasViewport viewport)
    {
        Viewport = viewport;
        Notify();
    }

    public void ToggleCanvasDark()
    {
        CanvasDark = !CanvasDark;
        Notify();
    }

    private void Notify() => Changed?.Invoke();

    public void Dispose() => Changed = null;
}
