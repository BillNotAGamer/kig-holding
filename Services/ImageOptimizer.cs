using SkiaSharp;

namespace KIGHolding.Services;

/// <summary>Re-encodes uploaded raster images as WebP, scaled down to a sensible maximum size.</summary>
public interface IImageOptimizer
{
    /// <summary>
    /// Returns a WebP version of <paramref name="source"/> scaled to fit <paramref name="maxDimension"/>,
    /// or null when the input is unsupported, cannot be decoded, or the result would not be smaller.
    /// Never throws for bad input: callers fall back to the original bytes.
    /// </summary>
    OptimizedImage? TryOptimize(byte[] source, string extension, int maxDimension);
}

public sealed record OptimizedImage(byte[] Content, int Width, int Height);

public sealed class SkiaImageOptimizer : IImageOptimizer
{
    public const string WebpExtension = ".webp";
    public const string WebpContentType = "image/webp";
    public const int DefaultMaxDimension = 2000;
    public const int MenuPageMaxDimension = 2400;
    private const int WebpQuality = 82;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly ILogger<SkiaImageOptimizer> _logger;

    public SkiaImageOptimizer(ILogger<SkiaImageOptimizer> logger)
    {
        _logger = logger;
    }

    public static int MaxDimensionFor(ImageCategory category)
    {
        return category == ImageCategory.MenuPages ? MenuPageMaxDimension : DefaultMaxDimension;
    }

    public OptimizedImage? TryOptimize(byte[] source, string extension, int maxDimension)
    {
        if (source is null || source.Length == 0 || !SupportedExtensions.Contains(extension ?? string.Empty))
        {
            return null;
        }

        SKBitmap? decoded = null;
        SKBitmap? oriented = null;
        SKBitmap? resized = null;

        try
        {
            using var data = SKData.CreateCopy(source);
            using var codec = SKCodec.Create(data);
            if (codec is null || codec.FrameCount > 1)
            {
                // Unknown format or animated image: keep the original.
                return null;
            }

            decoded = SKBitmap.Decode(codec);
            if (decoded is null)
            {
                return null;
            }

            oriented = ApplyOrigin(decoded, codec.EncodedOrigin);
            var current = oriented ?? decoded;

            resized = ResizeToFit(current, Math.Max(1, maxDimension));
            var final = resized ?? current;

            using var encoded = final.Encode(SKEncodedImageFormat.Webp, WebpQuality);
            if (encoded is null)
            {
                return null;
            }

            var bytes = encoded.ToArray();
            if (bytes.Length == 0 || bytes.Length >= source.Length)
            {
                return null;
            }

            return new OptimizedImage(bytes, final.Width, final.Height);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Includes native library load failures (DllNotFoundException / TypeInitializationException).
            _logger.LogWarning(
                "Image optimization skipped. Extension={Extension}, Bytes={Bytes}, ExceptionType={ExceptionType}",
                extension,
                source.Length,
                exception.GetType().Name);
            return null;
        }
        finally
        {
            resized?.Dispose();
            oriented?.Dispose();
            decoded?.Dispose();
        }
    }

    /// <summary>Applies the EXIF orientation so phone photos are not stored sideways.</summary>
    private static SKBitmap? ApplyOrigin(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft)
        {
            return null;
        }

        var swapsAxes = origin is SKEncodedOrigin.LeftTop
            or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom
            or SKEncodedOrigin.LeftBottom;

        var width = swapsAxes ? source.Height : source.Width;
        var height = swapsAxes ? source.Width : source.Height;

        var target = new SKBitmap(new SKImageInfo(width, height, source.ColorType, source.AlphaType));
        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.Transparent);

        switch (origin)
        {
            case SKEncodedOrigin.TopRight:
                canvas.Translate(width, 0);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.BottomRight:
                canvas.Translate(width, height);
                canvas.RotateDegrees(180);
                break;
            case SKEncodedOrigin.BottomLeft:
                canvas.Translate(0, height);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.LeftTop:
                canvas.RotateDegrees(90);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.RightTop:
                canvas.Translate(width, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom:
                canvas.Translate(width, height);
                canvas.RotateDegrees(90);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.LeftBottom:
                canvas.Translate(0, height);
                canvas.RotateDegrees(270);
                break;
        }

        canvas.DrawBitmap(source, 0, 0);
        canvas.Flush();
        return target;
    }

    private static SKBitmap? ResizeToFit(SKBitmap source, int maxDimension)
    {
        var longestSide = Math.Max(source.Width, source.Height);
        if (longestSide <= maxDimension)
        {
            return null;
        }

        var scale = (double)maxDimension / longestSide;
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));

        return source.Resize(new SKImageInfo(width, height, source.ColorType, source.AlphaType), SKFilterQuality.High);
    }
}
