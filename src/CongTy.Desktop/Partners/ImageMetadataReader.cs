using System.IO;

namespace CongTy.Desktop.Partners;

public sealed record CustomerImageFile(
    byte[] Bytes,
    string MimeType,
    int Width,
    int Height);

public static class ImageMetadataReader
{
    private const int MaxBytes = 5 * 1024 * 1024;

    public static async Task<CustomerImageFile> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists)
        {
            throw new FileNotFoundException("Không tìm thấy tệp ảnh.", filePath);
        }

        if (info.Length is < 1 or > MaxBytes)
        {
            throw new InvalidOperationException("Ảnh phải có dung lượng từ 1 byte đến 5 MB.");
        }

        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" => BuildJpeg(bytes),
            ".png" => BuildPng(bytes),
            ".webp" => BuildWebp(bytes),
            _ => throw new InvalidOperationException("Chỉ hỗ trợ ảnh JPEG, PNG hoặc WebP.")
        };
    }

    private static CustomerImageFile BuildPng(byte[] bytes)
    {
        if (bytes.Length < 24
            || bytes[0] != 0x89
            || bytes[1] != 0x50
            || bytes[2] != 0x4E
            || bytes[3] != 0x47
            || bytes[4] != 0x0D
            || bytes[5] != 0x0A
            || bytes[6] != 0x1A
            || bytes[7] != 0x0A)
        {
            throw new InvalidOperationException("Tệp PNG không hợp lệ.");
        }

        var width = ReadBigEndianInt32(bytes, 16);
        var height = ReadBigEndianInt32(bytes, 20);
        ValidateDimensions(width, height);
        return new CustomerImageFile(bytes, "image/png", width, height);
    }

    private static CustomerImageFile BuildJpeg(byte[] bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
        {
            throw new InvalidOperationException("Tệp JPEG không hợp lệ.");
        }

        var offset = 2;
        while (offset + 3 < bytes.Length)
        {
            while (offset < bytes.Length && bytes[offset] != 0xFF)
            {
                offset++;
            }

            while (offset < bytes.Length && bytes[offset] == 0xFF)
            {
                offset++;
            }

            if (offset >= bytes.Length)
            {
                break;
            }

            var marker = bytes[offset++];
            if (marker is 0xD8 or 0xD9)
            {
                continue;
            }

            if (offset + 1 >= bytes.Length)
            {
                break;
            }

            var length = (bytes[offset] << 8) | bytes[offset + 1];
            if (length < 2 || offset + length > bytes.Length)
            {
                throw new InvalidOperationException("Tệp JPEG có cấu trúc không hợp lệ.");
            }

            if (IsJpegStartOfFrame(marker))
            {
                if (length < 7)
                {
                    throw new InvalidOperationException("Tệp JPEG thiếu thông tin kích thước.");
                }

                var height = (bytes[offset + 3] << 8) | bytes[offset + 4];
                var width = (bytes[offset + 5] << 8) | bytes[offset + 6];
                ValidateDimensions(width, height);
                return new CustomerImageFile(bytes, "image/jpeg", width, height);
            }

            offset += length;
        }

        throw new InvalidOperationException("Không đọc được kích thước ảnh JPEG.");
    }

    private static CustomerImageFile BuildWebp(byte[] bytes)
    {
        if (bytes.Length < 30
            || !AsciiEquals(bytes, 0, "RIFF")
            || !AsciiEquals(bytes, 8, "WEBP"))
        {
            throw new InvalidOperationException("Tệp WebP không hợp lệ.");
        }

        if (AsciiEquals(bytes, 12, "VP8X"))
        {
            var width = 1 + ReadUInt24LittleEndian(bytes, 24);
            var height = 1 + ReadUInt24LittleEndian(bytes, 27);
            ValidateDimensions(width, height);
            return new CustomerImageFile(bytes, "image/webp", width, height);
        }

        if (AsciiEquals(bytes, 12, "VP8L"))
        {
            if (bytes[20] != 0x2F)
            {
                throw new InvalidOperationException("Tệp WebP lossless không hợp lệ.");
            }

            var width = 1 + bytes[21] + ((bytes[22] & 0x3F) << 8);
            var height = 1 + ((bytes[22] >> 6) | (bytes[23] << 2) | ((bytes[24] & 0x0F) << 10));
            ValidateDimensions(width, height);
            return new CustomerImageFile(bytes, "image/webp", width, height);
        }

        if (AsciiEquals(bytes, 12, "VP8 "))
        {
            if (bytes.Length < 30
                || bytes[23] != 0x9D
                || bytes[24] != 0x01
                || bytes[25] != 0x2A)
            {
                throw new InvalidOperationException("Tệp WebP lossy không hợp lệ.");
            }

            var width = (bytes[26] | (bytes[27] << 8)) & 0x3FFF;
            var height = (bytes[28] | (bytes[29] << 8)) & 0x3FFF;
            ValidateDimensions(width, height);
            return new CustomerImageFile(bytes, "image/webp", width, height);
        }

        throw new InvalidOperationException("Không đọc được kích thước ảnh WebP.");
    }

    private static bool IsJpegStartOfFrame(byte marker) =>
        marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7
            or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF;

    private static int ReadBigEndianInt32(byte[] bytes, int offset) =>
        (bytes[offset] << 24)
        | (bytes[offset + 1] << 16)
        | (bytes[offset + 2] << 8)
        | bytes[offset + 3];

    private static int ReadUInt24LittleEndian(byte[] bytes, int offset) =>
        bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16);

    private static bool AsciiEquals(byte[] bytes, int offset, string value)
    {
        if (offset + value.Length > bytes.Length)
        {
            return false;
        }

        for (var index = 0; index < value.Length; index++)
        {
            if (bytes[offset + index] != value[index])
            {
                return false;
            }
        }

        return true;
    }

    private static void ValidateDimensions(int width, int height)
    {
        if (width is < 1 or > 1_600 || height is < 1 or > 1_600)
        {
            throw new InvalidOperationException("Mỗi cạnh ảnh phải từ 1 đến 1600 px.");
        }
    }
}
