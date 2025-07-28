using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.Resp;
using codecrafters_redis.Resp.Parsing;
using codecrafters_redis.Server.Replications;
using codecrafters_redis.Server.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace codecrafters_redis.Server;

public sealed class ConnectionHandler(
    IServiceProvider serviceProvider,
    RespCommandParser commandParser,
    RedisServer redisServer,
    ReplicationManager replicationManager,
    TransactionManager transactionManager,
    ILogger<ConnectionHandler> logger)
{
    private const int BufferSize = 4 * 1024;

    public async Task<bool> Handle(Socket connection)
    {
        try
        {
            var shouldConnectionContinue = await HandleCore(connection);

            if (!shouldConnectionContinue)
                CleanupConnection(connection);

            return shouldConnectionContinue;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling connection from {RemoteEndPoint}", connection.RemoteEndPoint);
            CleanupConnection(connection);
            return false;
        }
    }

    private async Task<bool> HandleCore(Socket connection)
    {
        var buffer = new byte[BufferSize];

        if (!connection.Connected)
            return false;

        var read = await connection.ReceiveAsync(buffer);
        if (read <= 0) return false;

        var request = Encoding.UTF8.GetString(buffer, 0, read);

        if (redisServer.IsSlave && connection.IsMaster())
        {
            request = SkipFullResyncResponse(request);
            if (string.IsNullOrWhiteSpace(request))
                return true;
        }

        var commands = commandParser.GetRespCommands(request);

        await ProcessCommands(connection, commands);
        return true;
    }

    private async Task ProcessCommands(Socket connection, List<RespCommand> commands)
    {
        using var scope = serviceProvider.CreateScope();

        foreach (var (commandName, args, requestArray) in commands)
        {
            var singleRequest = requestArray.ToString();
            var command = scope.ServiceProvider.GetRequiredKeyedService<ICommand>(commandName.ToUpperInvariant());

            if (transactionManager.IsTransactionInProgress(connection) && command is not ExecCommand && command is not DiscardCommand)
            {
                transactionManager.EnqueueCommand(connection, new QueuedCommand(commandName, singleRequest, command, args));
                await connection.SendResp(new SimpleString("QUEUED"));
            }
            else
            {
                var response = await ExecuteCommand(connection, commandName, singleRequest, command, args);
                if (response is not SelfHandled)
                    await connection.SendResp(response);
            }
        }
    }

    internal async Task<RespObject> ExecuteCommand(Socket connection, string commandName, string request, ICommand command, RespObject[] args)
    {
        if (redisServer.IsMaster && IsWriteCommand(commandName))
        {
            redisServer.Offset += request.Length;
            replicationManager.BroadcastToReplicas(request);
        }

        var response = await command.Handle(connection, args);

        if (redisServer.IsSlave && connection.IsMaster()) 
            redisServer.Offset += request.Length;

        return response;
    }

    private static string SkipFullResyncResponse(string response)
    {
        if (!response.StartsWith("+FULLRESYNC", StringComparison.OrdinalIgnoreCase))
            return response;

        var commandStartIndex = response.IndexOf(DataType.Array, StringComparison.Ordinal);
        return commandStartIndex == -1 ? string.Empty : response[commandStartIndex..];
    }

    private static bool IsWriteCommand(string commandName) => commandName.Equals(SetCommand.Name, StringComparison.OrdinalIgnoreCase);

    private void CleanupConnection(Socket connection)
    {
        if (redisServer.IsMaster)
            replicationManager.RemoveReplica(connection);

        try
        {
            if (connection.Connected)
                connection.Shutdown(SocketShutdown.Both);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error shutting down connection");
        }
        finally
        {
            connection.Dispose();
        }
    }
}