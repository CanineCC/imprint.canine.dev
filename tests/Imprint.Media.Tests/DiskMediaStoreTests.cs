using System.Text;
using Imprint.Authoring.Domain;

namespace Imprint.Media.Tests;

public sealed class DiskMediaStoreTests : IDisposable
{
    private readonly TempMediaRoot _root = new();
    private readonly DiskMediaStore _store;
    private readonly AssetId _id = AssetId.New();

    public DiskMediaStoreTests() => _store = new DiskMediaStore(_root.Options);

    public void Dispose() => _root.Dispose();

    private static MemoryStream Bytes(string text) => new(Encoding.UTF8.GetBytes(text));

    // -- saving ------------------------------------------------------------------

    [Fact]
    public async Task SaveOriginal_stores_under_originals_and_returns_readable_key()
    {
        var key = await _store.SaveOriginal(_id, "photo.jpg", Bytes("jpeg-bytes"));

        Assert.Equal($"originals/{_id.Compact}/photo.jpg", key);
        await using var stream = await _store.Open(key);
        using var reader = new StreamReader(stream);
        Assert.Equal("jpeg-bytes", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task SaveDerived_stores_under_derived_and_returns_readable_key()
    {
        var key = await _store.SaveDerived(_id, "480.webp", Encoding.UTF8.GetBytes("webp-bytes"));

        Assert.Equal($"derived/{_id.Compact}/480.webp", key);
        Assert.Equal("webp-bytes", await _store.ReadAllText(key));
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\evil.exe")]
    [InlineData("/etc/passwd")]
    [InlineData("....//....//secret")]
    [InlineData(".hidden")]
    [InlineData("...")]
    [InlineData("///")]
    [InlineData("nested/path/name.png")]
    [InlineData("control\u0001\u001Fchars\u007F.png")]
    [InlineData("name\0with-nul.png")]
    public async Task SaveOriginal_with_hostile_file_name_stays_strictly_under_the_asset_directory(string fileName)
    {
        var key = await _store.SaveOriginal(_id, fileName, Bytes("content"));

        var assetDirectory = Path.Combine(_root.Path, "originals", _id.Compact);
        var stored = Assert.Single(Directory.GetFiles(assetDirectory, "*", SearchOption.AllDirectories));
        Assert.Equal(assetDirectory, Path.GetDirectoryName(stored));
        Assert.False(Path.GetFileName(stored).StartsWith('.'));
        Assert.Equal(stored, _store.PhysicalPathOf(key));

        // Nothing may have escaped: the root contains only the originals tree.
        Assert.Single(Directory.GetFiles(_root.Path, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task SaveOriginal_caps_file_name_length_at_100()
    {
        var key = await _store.SaveOriginal(_id, new string('a', 300) + ".png", Bytes("x"));

        Assert.Equal(100, Path.GetFileName(_store.PhysicalPathOf(key)).Length);
    }

    [Fact]
    public async Task SaveOriginal_with_name_that_sanitizes_to_nothing_falls_back_to_a_default()
    {
        var key = await _store.SaveOriginal(_id, "..././//...", Bytes("x"));

        Assert.Equal($"originals/{_id.Compact}/file", key);
    }

    [Fact]
    public async Task SaveOriginal_streams_large_content_faithfully()
    {
        var payload = new byte[512 * 1024];
        Random.Shared.NextBytes(payload);

        var key = await _store.SaveOriginal(_id, "big.bin", new MemoryStream(payload));

        await using var stream = await _store.Open(key);
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        Assert.Equal(payload, copy.ToArray());
    }

    // -- key validation ------------------------------------------------------------

    [Theory]
    [InlineData("..")]
    [InlineData("../outside.txt")]
    [InlineData("originals/../../outside.txt")]
    [InlineData("originals/../..")]
    [InlineData("/etc/passwd")]
    [InlineData("derived/x/../../../etc/shadow")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Open_with_key_escaping_the_root_is_rejected(string storageKey)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _store.Open(storageKey));
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("originals/../../outside.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("")]
    public void PhysicalPathOf_with_key_escaping_the_root_is_rejected(string storageKey)
    {
        Assert.Throws<ArgumentException>(() => _store.PhysicalPathOf(storageKey));
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("originals/../../outside.txt")]
    public async Task ReadAllText_with_key_escaping_the_root_is_rejected(string storageKey)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _store.ReadAllText(storageKey));
    }

    [Fact]
    public async Task Open_with_key_resolving_to_the_root_itself_is_rejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _store.Open("originals/.."));
    }

    [Fact]
    public async Task Open_with_sibling_prefix_escape_is_rejected()
    {
        // "root-evil" shares the character prefix of "root" — a naive StartsWith check
        // on the un-terminated root path would let this one through.
        var sibling = "../" + Path.GetFileName(_root.Path) + "-evil/file.txt";
        await Assert.ThrowsAsync<ArgumentException>(() => _store.Open(sibling));
    }

    [Fact]
    public void PhysicalPathOf_valid_key_returns_absolute_path_under_root()
    {
        var path = _store.PhysicalPathOf($"originals/{_id.Compact}/photo.jpg");

        Assert.True(Path.IsPathRooted(path));
        Assert.StartsWith(_root.Path + Path.DirectorySeparatorChar, path, StringComparison.Ordinal);
    }

    // -- deletion ------------------------------------------------------------------

    [Fact]
    public async Task DeleteAll_removes_original_and_derived_directories()
    {
        await _store.SaveOriginal(_id, "photo.jpg", Bytes("original"));
        await _store.SaveDerived(_id, "480.webp", Encoding.UTF8.GetBytes("derived"));

        await _store.DeleteAll(_id);

        Assert.False(Directory.Exists(Path.Combine(_root.Path, "originals", _id.Compact)));
        Assert.False(Directory.Exists(Path.Combine(_root.Path, "derived", _id.Compact)));
    }

    [Fact]
    public async Task DeleteAll_leaves_other_assets_untouched()
    {
        var other = AssetId.New();
        await _store.SaveOriginal(_id, "mine.jpg", Bytes("mine"));
        await _store.SaveOriginal(other, "theirs.jpg", Bytes("theirs"));

        await _store.DeleteAll(_id);

        Assert.True(Directory.Exists(Path.Combine(_root.Path, "originals", other.Compact)));
    }

    [Fact]
    public async Task DeleteAll_for_unknown_asset_is_a_no_op()
    {
        await _store.DeleteAll(AssetId.New());
    }

    [Fact]
    public void Constructor_with_empty_root_path_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new DiskMediaStore(new MediaOptions { RootPath = " " }));
    }
}
