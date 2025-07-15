namespace codecrafters_redis.Server;

public class RedisConfiguration
{
    private readonly string[] _args;
    private const int DefaultPort = 6379;
    private const int MinPort = 1;
    private const int MaxPort = 65535;

    public RedisConfiguration(string[] args)
    {
        _args = args;

        Directory = GetArgValue("dir");
        DbFileName = GetArgValue("dbfilename");
        Port = ParsePort(GetArgValue("port"));
        ReplicaOf = GetArgValue("replicaof");

        if (!IsReplica) return;

        var (masterHost, masterPort) = ParseReplicaOfConfig(ReplicaOf!);
        MasterHost = masterHost;
        MasterPort = ParsePort(masterPort);
    }

    public string Directory { get; }
    public string DbFileName { get; }
    public int Port { get; }
    public string? ReplicaOf { get; }
    public string? MasterHost { get; }
    public int? MasterPort { get; }
    public bool IsReplica => !string.IsNullOrWhiteSpace(ReplicaOf);

    private static (string Host, string Port) ParseReplicaOfConfig(string replicaOf)
    {
        if (string.IsNullOrWhiteSpace(replicaOf))
            throw new ArgumentException("Replica configuration cannot be null or empty", nameof(replicaOf));

        var parts = replicaOf.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid replicaof configuration format. Expected format: '<host> <port>'", nameof(replicaOf));

        var host = parts[0];
        var port = parts[1];

        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Master host cannot be empty", nameof(replicaOf));

        if (string.IsNullOrWhiteSpace(port))
            throw new ArgumentException("Master port cannot be empty", nameof(replicaOf));

        return (host, port);
    }

    private string GetArgValue(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
            return string.Empty;

        arg = $"--{arg}";
        var index = Array.IndexOf(_args, arg);

        if (index == -1)
            return string.Empty;

        if (index + 1 >= _args.Length)
            throw new ArgumentException($"Argument '{arg}' is missing its value");

        return _args[index + 1];
    }

    private int ParsePort(string portValue)
    {
        switch (IsReplica)
        {
            case false when string.IsNullOrWhiteSpace(portValue):
                return DefaultPort;
            case true when string.IsNullOrWhiteSpace(portValue):
                throw new ArgumentException("Replica master port configuration cannot be null or empty");
        }

        if (!int.TryParse(portValue, out var port))
            throw new ArgumentException($"Invalid port value: '{portValue}'. Port must be a valid integer.");

        if (port is < MinPort or > MaxPort)
            throw new ArgumentException($"Port value '{port}' is out of range. Port must be between {MinPort} and {MaxPort}.");

        return port;
    }
}