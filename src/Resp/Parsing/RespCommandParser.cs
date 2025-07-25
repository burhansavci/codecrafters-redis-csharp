using System.Text;
using codecrafters_redis.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis.Resp.Parsing;

public class RespCommandParser(IServiceProvider serviceProvider)
{
    public List<RespCommand> GetRespCommands(string request)
    {
        try
        {
            var commands = new List<RespCommand>();
            var requests = request.Split(RespObject.CRLF, StringSplitOptions.RemoveEmptyEntries);

            for (var index = 0; index < requests.Length; index++)
            {
                var requestPart = requests[index];

                if (!requestPart.StartsWith(DataType.Array))
                    continue;

                // Skip malformed array indicators (like standalone "*")
                if (requestPart.Length < 2)
                {
                    Console.WriteLine($"{index}");
                    Console.WriteLine(request.Replace("\r", "\\r").Replace("\n", "\\n"));
                    Console.WriteLine($"Skipping malformed array indicator: '{requestPart.Replace("\r", "\\r").Replace("\n", "\\n")}'");
                    continue;
                }

                var (array, arrayItemsLength) = ParseArrayRequest(requests, index);
                var (commandName, args) = ExtractCommandAndArgs(array);

                commands.Add(new RespCommand(commandName, args, array));
                index += arrayItemsLength;
            }

            return commands;
        }
        catch (Exception)
        {
            Console.WriteLine(request.Replace("\r", "\\r").Replace("\n", "\\n"));
            throw;
        }
    }

    private static (Array Array, int ArrayItemsLength) ParseArrayRequest(string[] requests, int startIndex)
    {
        var requestPart = requests[startIndex];
        var arrayItemsLength = int.Parse(requestPart[1..]) * 2; //2 = '$<length>\r\n<data>\r\n

        var request = new StringBuilder();
        request.Append(requestPart);
        request.Append(RespObject.CRLF);

        var endIndex = startIndex + 1 + arrayItemsLength;
        if (endIndex > requests.Length)
            throw new IndexOutOfRangeException($"{endIndex} is out of range. Request length: {requests.Length}");

        var arrayItems = requests[(startIndex + 1)..endIndex];
        request.Append(string.Join(RespObject.CRLF, arrayItems));
        request.Append(RespObject.CRLF);

        var array = Array.Parse(request.ToString());
        return (array, arrayItemsLength);
    }

    private (string CommandName, RespObject[] Args) ExtractCommandAndArgs(Array array)
    {
        if (array.Items.Length == 0)
            throw new ArgumentException("Empty command array");

        var commandMessage = (BulkString)array.Items[0];
        var commandName = commandMessage.Data ?? string.Empty;
        var skipCount = 1;

        // Handle commands that might have sub-commands
        if (array.Items.Length > 1)
        {
            var subCommand = ((BulkString)array.Items[1]).Data;
            var compositeCommandName = $"{commandName} {subCommand}";

            if (serviceProvider.GetKeyedService<ICommand>(compositeCommandName) != null)
            {
                commandName = compositeCommandName;
                skipCount = 2;
            }
        }

        var args = array.Items.Skip(skipCount).ToArray();
        return (commandName, args);
    }
}