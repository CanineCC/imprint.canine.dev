using System.Text;
using Imprint.Editor.Api;
using Microsoft.AspNetCore.Http;

namespace Imprint.Editor.Tests;

/// <summary>
/// The request parser shared by <c>POST /assets</c> and <c>POST /assets/{id}/dark</c>. It is shared
/// on purpose: a dark rendition that parsed its body even slightly differently from the base upload
/// (a different content-type default, a different empty-file verdict) would attach a mismatched file
/// to an existing asset, and the divergence would only surface as a broken diagram on a published
/// page. These tests pin the two accepted shapes and the rejections to ONE behaviour.
/// </summary>
public sealed class AuthoringUploadBodyTests
{
    private static DefaultHttpContext Raw(string body, string? fileName, string? contentType)
    {
        var http = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(body);
        http.Request.Body = new MemoryStream(bytes);
        http.Request.ContentLength = bytes.Length;
        if (fileName is not null)
        {
            http.Request.Headers["X-Filename"] = fileName;
        }

        if (contentType is not null)
        {
            http.Request.ContentType = contentType;
        }

        return http;
    }

    private static DefaultHttpContext Multipart(string body, string fileName, string contentType, string field = "file")
    {
        var http = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(body);
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, field, fileName)
        {
            Headers = new HeaderDictionary { ["Content-Type"] = contentType },
        };
        http.Request.ContentType = "multipart/form-data; boundary=----test";
        http.Request.Form = new FormCollection(null, new FormFileCollection { file });
        return http;
    }

    private static async Task<string> ReadAll(Stream s)
    {
        using var reader = new StreamReader(s, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task Raw_body_with_a_filename_header_is_read()
    {
        var (body, error) = await AuthoringApi.ReadUpload(Raw("<svg/>", "venn-dark.svg", "image/svg+xml"), default);

        Assert.Null(error);
        Assert.NotNull(body);
        Assert.Equal("venn-dark.svg", body!.FileName);
        Assert.Equal("image/svg+xml", body.ContentType);
        Assert.Equal(6, body.ByteSize);
        Assert.Equal("<svg/>", await ReadAll(body.Content));
    }

    [Fact]
    public async Task Multipart_file_is_read()
    {
        var (body, error) = await AuthoringApi.ReadUpload(Multipart("<svg/>", "venn.svg", "image/svg+xml"), default);

        Assert.Null(error);
        Assert.NotNull(body);
        Assert.Equal("venn.svg", body!.FileName);
        Assert.Equal("image/svg+xml", body.ContentType);
        Assert.Equal(6, body.ByteSize);
    }

    [Fact]
    public async Task A_multipart_form_takes_the_first_file_when_the_field_is_not_named_file()
    {
        var (body, error) = await AuthoringApi.ReadUpload(Multipart("<svg/>", "venn.svg", "image/svg+xml", field: "dark"), default);

        Assert.Null(error);
        Assert.Equal("venn.svg", body!.FileName);
    }

    [Fact]
    public async Task A_raw_body_without_a_filename_header_is_refused()
    {
        var (body, error) = await AuthoringApi.ReadUpload(Raw("<svg/>", fileName: null, "image/svg+xml"), default);

        Assert.Null(body);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<IStatusCodeHttpResult>(error).StatusCode);
    }

    [Fact]
    public async Task An_empty_body_is_refused()
    {
        var (body, error) = await AuthoringApi.ReadUpload(Raw("", "venn.svg", "image/svg+xml"), default);

        Assert.Null(body);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<IStatusCodeHttpResult>(error).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-media-type")]
    public async Task A_missing_or_malformed_content_type_falls_back_to_octet_stream(string? contentType)
    {
        var (body, error) = await AuthoringApi.ReadUpload(Raw("<svg/>", "venn.svg", contentType), default);

        Assert.Null(error);
        Assert.Equal("application/octet-stream", body!.ContentType);
    }
}
