using System.Net.Sockets;
using System.Text;
using codecrafters_redis.RESP;

namespace codecrafters_redis.Commands;

public class EchoCommand : ICommand
{
    public const string Name = "ECHO";
    
    public async Task Handle(Socket connection, RespObject[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentOutOfRangeException.ThrowIfZero(args.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(args.Length, 1);

        if (args[0] is not BulkString message)
            throw new FormatException("Invalid message format. Expected bulk string.");
        
        await connection.SendAsync(Encoding.UTF8.GetBytes(message));
    }
}