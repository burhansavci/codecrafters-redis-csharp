namespace codecrafters_redis.Rdb;

public class RdbReader : IDisposable
{
    private readonly BinaryReader? _reader;
    private readonly Dictionary<string, Record> _data = new();

    public RdbReader(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);

        if (File.Exists(path))
            _reader = new BinaryReader(File.OpenRead(path));
    }

    public Dictionary<string, Record> Read()
    {
        if (_reader is null)
            return _data;

        ReadCore();

        return _data;
    }

    private void ReadCore()
    {
        Header();
        FileSection section;
        do
        {
            section = (FileSection)_reader!.ReadByte();

            switch (section)
            {
                case FileSection.Metadata:
                    _reader.ReadStringEncoded(); // metadataAttributeName
                    _reader.ReadStringEncoded(); // metadataAttributeValue
                    break;
                case FileSection.DatabaseSubSectionStart:
                    _reader.ReadSizeEncoded();
                    break;
                case FileSection.HashTableSize:
                    _reader.ReadSizeEncoded(); // hashTableSize
                    _reader.ReadSizeEncoded(); // expiryHashTableSize
                    break;
                case FileSection.ExpireTimeInMilliseconds:
                    var expireTimeInMilliseconds = _reader.ReadInt64(); // Unix (8-byte unsigned integer)
                    var expireAtInMilliseconds = DateTimeOffset.FromUnixTimeMilliseconds(expireTimeInMilliseconds).DateTime;
                    ReadKeyValue((ValueType)_reader.ReadByte(), expireAtInMilliseconds);
                    break;
                case FileSection.ExpireTimeInSeconds:
                    var expireTimeInSeconds = _reader.ReadInt32(); // Unix (4-byte unsigned integer)
                    var expireAtInSeconds = DateTimeOffset.FromUnixTimeSeconds(expireTimeInSeconds).DateTime;
                    ReadKeyValue((ValueType)_reader.ReadByte(), expireAtInSeconds);
                    break;
                case FileSection.EndOfFile:
                    _reader.ReadBytes(8); // CRC64 checksum
                    break;
                default:
                    ReadKeyValue((ValueType)section);
                    break;
            }
        } while (section != FileSection.EndOfFile);
    }

    private void Header()
    {
        _reader!.ReadBytes(5); //REDIS
        _reader.ReadBytes(4); //0011
    }

    private void ReadKeyValue(ValueType valueType, DateTime? expireAt = null)
    {
        string key = _reader!.ReadStringEncoded();

        switch (valueType)
        {
            case ValueType.String:
                var value = _reader!.ReadStringEncoded();
                _data.Add(key, new Record(value, expireAt));
                break;
            default:
                throw new NotSupportedException($"Value type {valueType} is not supported.");
        }
    }

    public void Dispose()
    {
        _reader?.Dispose();
    }
}