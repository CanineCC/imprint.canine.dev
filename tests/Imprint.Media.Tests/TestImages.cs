using SkiaSharp;

namespace Imprint.Media.Tests;

/// <summary>
/// Test images are generated, never checked in: binary fixtures rot silently, while a
/// drawn gradient documents exactly what the pipeline was fed.
/// </summary>
internal static class TestImages
{
    public static byte[] GradientPng(int width, int height) => Encode(width, height, SKEncodedImageFormat.Png);

    public static byte[] GradientJpeg(int width, int height) => Encode(width, height, SKEncodedImageFormat.Jpeg);

    private static byte[] Encode(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bitmap))
        {
            using var paint = new SKPaint();
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(width, height),
                [SKColors.Crimson, SKColors.RoyalBlue], SKShaderTileMode.Clamp);
            canvas.DrawRect(new SKRect(0, 0, width, height), paint);
        }

        using var data = bitmap.Encode(format, 90);
        return data.ToArray();
    }

    /// <summary>
    /// Splices a minimal EXIF APP1 segment (little-endian TIFF, one IFD entry: tag
    /// 0x0112 = orientation) after the JPEG SOI marker. Skia cannot author EXIF, so
    /// the segment is built by hand to exercise the reorientation path.
    /// </summary>
    public static byte[] WithExifOrientation(byte[] jpeg, byte orientation)
    {
        byte[] app1 =
        [
            0xFF, 0xE1, 0x00, 0x22,
            (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0x00, 0x00,
            (byte)'I', (byte)'I', 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00,
            0x01, 0x00,
            0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, orientation, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
        ];
        var result = new byte[jpeg.Length + app1.Length];
        result[0] = jpeg[0];
        result[1] = jpeg[1];
        app1.CopyTo(result, 2);
        Array.Copy(jpeg, 2, result, 2 + app1.Length, jpeg.Length - 2);
        return result;
    }
}
