using System.Text;

namespace codecrafters_redis.RESP;

public record Array(params RespObject[] Items) : RespObject(DataType.Array)
{
    public int Length => Items.Length;
    
    public static implicit operator string(Array array) => array.ToString();

    public static Array Parse(string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);

        var parts = data.Split(CRLF, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            throw new FormatException("Invalid RESP array format: no data");

        var arrayMetaData = parts[0];

        if (arrayMetaData.Length < 2)
            throw new FormatException("Invalid RESP array format: incomplete header");

        var firstByte = arrayMetaData[0];

        if (firstByte != DataType.Array)
            throw new FormatException($"Expected array data type '{DataType.Array}', but got '{firstByte}'");

        if (!int.TryParse(arrayMetaData[1..], out var arrayLength))
            throw new FormatException("Invalid array length format");

        if (arrayLength < 0)
            throw new FormatException("Array length cannot be negative");

        if (arrayLength == 0)
            return new Array();

        parts = parts.Skip(1).ToArray();
        var items = new RespObject[arrayLength];
        var itemIndex = 0;
        var partIndex = 0;

        while (itemIndex < arrayLength && partIndex < parts.Length)
        {
            if (partIndex >= parts.Length)
                throw new FormatException($"Insufficient data for array item {itemIndex}");

            var typeHeader = parts[partIndex++];
            Console.WriteLine(typeHeader);
            if (string.IsNullOrEmpty(typeHeader))
                throw new FormatException($"Empty type header for array item {itemIndex}");

            var itemData = parts[partIndex++];
            Console.WriteLine(itemData);
            if (string.IsNullOrEmpty(itemData))
                throw new FormatException($"Empty data for array item {itemIndex}");

            var dataType = typeHeader[0];

            items[itemIndex] = dataType switch
            {
                DataType.BulkString => new BulkString(itemData),
                DataType.SimpleString => new SimpleString(itemData),
                _ => throw new FormatException($"Unsupported data type '{dataType}' at element {itemIndex}")
            };
            itemIndex++;
        }

        if (itemIndex != arrayLength)
            throw new FormatException($"Expected {arrayLength} elements, but parsed {itemIndex}");

        return new Array(items);
    }

    public override string ToString()
    {
        var header = $"{FirstByte}{Length}{CRLF}";

        StringBuilder sb = new();

        foreach (var respObject in Items)
            if (respObject is BulkString bulkString)
                sb.Append(bulkString);
            else
                throw new NotImplementedException();

        return header + sb;
    }
}