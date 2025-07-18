using System.Text;

namespace codecrafters_redis.Resp;

public record Array(params RespObject[] Items) : RespObject(DataType.Array)
{
    public int Length => Items.Length;

    public static implicit operator string(Array array) => array.ToString();

    public static Array Parse(string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);

        var parts = data.Split(CRLF, StringSplitOptions.RemoveEmptyEntries);
        ValidateFormat(parts);

        var arrayLength = ParseArrayLength(parts[0]);

        return arrayLength == 0 ? new Array() : new Array(ParseArrayItems(parts.Skip(1).ToArray(), arrayLength));
    }

    private static void ValidateFormat(string[] parts)
    {
        if (parts.Length == 0)
            throw new FormatException("Invalid RESP array format: no data");

        var arrayHeader = parts[0];
        if (arrayHeader.Length < 2)
            throw new FormatException("Invalid RESP array format: incomplete header");

        if (arrayHeader[0] != DataType.Array)
            throw new FormatException($"Expected array data type '{DataType.Array}', but got '{arrayHeader[0]}'");
    }

    private static int ParseArrayLength(string arrayHeader)
    {
        if (!int.TryParse(arrayHeader[1..], out var arrayLength))
            throw new FormatException("Invalid array length format");

        if (arrayLength < 0)
            throw new FormatException("Array length cannot be negative");

        return arrayLength;
    }

    private static RespObject[] ParseArrayItems(string[] dataParts, int itemsLength)
    {
        var items = new RespObject[itemsLength];
        var partIndex = 0;

        for (var itemIndex = 0; itemIndex < itemsLength; itemIndex++)
        {
            if (partIndex + 1 >= dataParts.Length)
                throw new FormatException($"Insufficient data for array item {itemIndex}");

            var typeHeader = dataParts[partIndex++];
            var dataType = typeHeader[0];

            var itemData = dataParts[partIndex++];

            ValidateItemData(typeHeader, itemData, itemIndex);

            items[itemIndex] = dataType switch
            {
                DataType.BulkString => new BulkString(itemData),
                DataType.SimpleString => new SimpleString(itemData),
                _ => throw new FormatException($"Unsupported data type '{dataType}' at element {itemIndex}")
            };
        }

        return items;
    }

    private static void ValidateItemData(string typeHeader, string itemData, int itemIndex)
    {
        if (string.IsNullOrEmpty(typeHeader))
            throw new FormatException($"Empty type header for array item {itemIndex}");

        if (string.IsNullOrEmpty(itemData))
            throw new FormatException($"Empty data for array item {itemIndex}");
    }

    public override string ToString()
    {
        var sb = new StringBuilder($"{FirstByte}{Length}{CRLF}");

        foreach (var respObject in Items)
            sb.Append(respObject);

        return sb.ToString();
    }
}