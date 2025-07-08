using System.Buffers.Binary;
using System.Text;

namespace codecrafters_redis.Rdb;

public static class BinaryReaderExtensions
{
    public static string ReadStringEncoded(this BinaryReader reader)
    {
        var (size, isStringEncoded) = reader.ReadSizeEncodedCore();

        if (!isStringEncoded)
            return Encoding.UTF8.GetString(reader.ReadBytes(size));

        switch (size)
        {
            case 0b00000000: // 8-bit integer
                return reader.ReadByte().ToString();
            case 0b00000001: // 16-bit integer (little-endian)
                return reader.ReadInt16().ToString();
            case 0b00000010: // 32-bit integer (little-endian)
                return reader.ReadInt32().ToString();
            case 0b00000011: // LZF-compressed strings
            default:
                throw new NotSupportedException("LZF-compressed strings are not supported.");
        }
    }

    public static int ReadSizeEncoded(this BinaryReader reader)
    {
        var (size, isStringEncoded) = reader.ReadSizeEncodedCore();

        if (isStringEncoded)
            throw new FormatException("Invalid size encoding. Expected size encoded value (not string encoded).");

        return size;
    }

    private static (int size, bool isStringEncoded) ReadSizeEncodedCore(this BinaryReader reader)
    {
        var firstByte = reader.ReadByte();

        var firstTwoBits = (firstByte >> 6) & 0b11; // Extract first 2 bits

        switch (firstTwoBits)
        {
            case 0b00:
            {
                return (firstByte & 0b00111111, false);
            }
            case 0b01:
            {
                var sizeFirstByte = (firstByte & 0b00111111) << 8;
                var sizeSecondByte = reader.ReadByte();
                return (sizeFirstByte | sizeSecondByte, false);
            }
            case 0b10:
                // Skip the remaining 6 bits and read next 4 bytes in big-endian
                var bytes = reader.ReadBytes(4);
                return (BinaryPrimitives.ReadInt32BigEndian(bytes), false);
            default: // 0b11: The remaining 6 bits specify a type of string encoding.
                return (firstByte & 0b00111111, true);
        }
    }
}