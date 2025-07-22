using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.Resp;
using codecrafters_redis.Resp.Parsing;
using codecrafters_redis.Server.Replications;
using codecrafters_redis.Server.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis.Server;

public class ConnectionHandler(IServiceProvider serviceProvider, RespCommandParser commandParser, RedisServer redisServer, ReplicationManager replicationManager, TransactionManager transactionManager)
{
    private const int BufferSize = 4 * 1024;

    public async Task Handle(Socket connection)
    {
        var buffer = new byte[BufferSize];
        try
        {
            while (connection.Connected)
            {
                var read = await connection.ReceiveAsync(buffer);
                if (read <= 0) break;

                var request = Encoding.UTF8.GetString(buffer, 0, read);

                if (redisServer.IsSlave && connection.IsMaster())
                {
                    request = SkipFullResyncResponse(request);
                    if (string.IsNullOrWhiteSpace(request))
                        continue;
                }

                var commands = commandParser.GetRespCommands(request);

                using var scope = serviceProvider.CreateScope();
                foreach (var (commandName, args, requestArray) in commands)
                {
                    var singleRequest = requestArray.ToString();
                    var command = scope.ServiceProvider.GetRequiredKeyedService<ICommand>(commandName.ToUpperInvariant());

                    if (transactionManager.IsTransactionInProgress(connection) && (command is not ExecCommand || command is not DiscardCommand))
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
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            if (redisServer.IsMaster)
                replicationManager.RemoveReplica(connection);

            connection.Dispose();
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
        {
            redisServer.Offset += request.Length;
        }

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
}