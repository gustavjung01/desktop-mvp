using System.IO;
using SkiaSharp;

namespace CongTy.Desktop.Products;

public static class ProductImageProcessor
{
    public const int MaxEdge = 1600;
    public const long MaxInputBytes = 20L * 1024 * 1024;
    public const int WebpQuality = 82;

    public static async Task<byte[]> ToWebpAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Chưa chọn ảnh.", nameof(filePath));

        var info = new FileInfo(filePath);
        if (!info.Exists) throw new FileNotFoundException("Không tìm thấy ảnh đã chọn.", filePath);
        if (info.Length is < 1 or > MaxInputBytes) throw new InvalidOperationException("Ảnh gốc tối đa 20 MB.");

        var extension = info.Extension.ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp")
            throw new InvalidOperationException("Chọn ảnh JPG, PNG hoặc WebP.");

        await using var stream = File.OpenRead(filePath);
        using var input = SKBitmap.Decode(stream) ?? throw new InvalidOperationException("Không đọc được ảnh.");
        cancellationToken.ThrowIfCancellationRequested();

        var longest = Math.Max(input.Width, input.Height);
        var width = input.Width;
        var height = input.Height;
        if (longest > MaxEdge)
        {
            var ratio = (double)MaxEdge / longest;
            width = Math.Max(1, (int)Math.Round(input.Width * ratio));
            height = Math.Max(1, (int)Math.Round(input.Height * ratio));
        }

        using var resized = width == input.Width && height == input.Height
            ? null
            : input.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
              ?? throw new InvalidOperationException("Không thể đổi kích thước ảnh.");
        var bitmap = resized ?? input;
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, WebpQuality)
            ?? throw new InvalidOperationException("Không thể chuyển ảnh sang WebP.");
        var bytes = encoded.ToArray();
        if (bytes.Length is < 1 or > 5 * 1024 * 1024)
            throw new InvalidOperationException("Ảnh WebP sau xử lý vượt quá 5 MB.");
        return bytes;
    }
}
