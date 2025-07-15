using System.Net.Sockets;

namespace codecrafters_redis.Server.Replications;

public class ReplicaState
{
    public Socket Socket { get; set; } = null!;
    public int AcknowledgedOffset { get; set; }
    public int ExpectedOffset { get; set; }
    public bool IsAcknowledged { get; set; }
}