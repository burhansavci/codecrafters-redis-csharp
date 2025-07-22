using System.Collections.Concurrent;
using System.Net.Sockets;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Server.Transactions;

public class TransactionManager
{
    private readonly ConcurrentDictionary<Socket, ConcurrentQueue<QueuedCommand>> _execWaitingCommands = new();

    public void StartTransaction(Socket socket) => _execWaitingCommands.TryAdd(socket, new ConcurrentQueue<QueuedCommand>());

    public async Task<List<RespObject>?> ExecuteTransaction(Socket socket, ConnectionHandler connectionHandler)
    {
        if (!_execWaitingCommands.TryRemove(socket, out var queue))
            return null;

        var responses = new List<RespObject>();
        while (queue.TryDequeue(out var command))
        {
            var response = await connectionHandler.ExecuteCommand(socket, command.CommandName, command.Request, command.Command, command.Args);
            responses.Add(response);
        }

        return responses;
    }

    public bool IsTransactionInProgress(Socket socket) => _execWaitingCommands.ContainsKey(socket);

    public void EnqueueCommand(Socket socket, QueuedCommand command)
    {
        if (_execWaitingCommands.TryGetValue(socket, out var queue))
            queue.Enqueue(command);
    }
    
    public bool DiscardTransaction(Socket socket) => _execWaitingCommands.TryRemove(socket, out _);
}