using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace TypeExtensions;

public static class Bytes {
    public static uint ReadEthUInt16(this ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > 2)
        {
            bytes = bytes.Slice(bytes.Length - 2, 2);
        }

        if (bytes.Length == 2)
        {
            return BinaryPrimitives.ReadUInt16BigEndian(bytes);
        }

        Span<byte> fourBytes = stackalloc byte[2];
        bytes.CopyTo(fourBytes.Slice(2 - bytes.Length));
        return BinaryPrimitives.ReadUInt16BigEndian(fourBytes);
    }

    public static int ReadEthInt16(this ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > 2)
        {
            bytes = bytes.Slice(bytes.Length - 2, 2);
        }

        if (bytes.Length == 2)
        {
            return BinaryPrimitives.ReadInt16BigEndian(bytes);
        }

        Span<byte> fourBytes = stackalloc byte[2];
        bytes.CopyTo(fourBytes.Slice(2 - bytes.Length));
        return BinaryPrimitives.ReadInt16BigEndian(fourBytes);
    }
    private static byte[] FromHexNibble1Table =
    {
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 0, 16,
        32, 48, 64, 80, 96, 112, 128, 144, 255, 255,
        255, 255, 255, 255, 255, 160, 176, 192, 208, 224,
        240, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 160, 176, 192,
        208, 224, 240
    };

    private static byte[] FromHexNibble2Table =
    {
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 0, 1,
        2, 3, 4, 5, 6, 7, 8, 9, 255, 255,
        255, 255, 255, 255, 255, 10, 11, 12, 13, 14,
        15, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 10, 11, 12,
        13, 14, 15
    };

    public static string ToHexString(this Byte[] bytes)
    {
        return Convert.ToHexString(bytes);
    }

    public static byte[] toByteArray(this string hexString)
    {
        try {
            if (hexString is null)
            {
                throw new ArgumentNullException($"{nameof(hexString)}");
            }

            int startIndex = hexString.StartsWith("0x") ? 2 : 0;
            bool odd = hexString.Length % 2 == 1;
            int numberChars = hexString.Length - startIndex + (odd ? 1 : 0);
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                if (odd && i == 0)
                {
                    bytes[0] += FromHexNibble2Table[(byte)hexString[startIndex]];
                }
                else if (odd)
                {
                    bytes[i / 2] += FromHexNibble1Table[(byte)hexString[i + startIndex - 1]];
                    bytes[i / 2] += FromHexNibble2Table[(byte)hexString[i + startIndex]];
                }
                else
                {
                    bytes[i / 2] += FromHexNibble1Table[(byte)hexString[i + startIndex]];
                    bytes[i / 2] += FromHexNibble2Table[(byte)hexString[i + startIndex + 1]];
                }
            }
            return bytes;
        } catch {
            return Encoding.UTF8.GetBytes(hexString);
        }
    }
}