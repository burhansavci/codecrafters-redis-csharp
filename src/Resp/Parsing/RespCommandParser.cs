using System.Text;
using codecrafters_redis.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace codecrafters_redis.Resp.Parsing;

public class RespCommandParser(IServiceProvider serviceProvider, ILogger<RespCommandParser> logger)
{
    public List<RespCommand> GetRespCommands(string request)
    {
        try
        {
            var commands = new List<RespCommand>();

            // Skip RDB file if present
            var cleanRequest = SkipRdbFile(request);

            var requests = cleanRequest.Split(RespObject.CRLF, StringSplitOptions.RemoveEmptyEntries);

            for (var index = 0; index < requests.Length; index++)
            {
                var requestPart = requests[index];
                
                // Only consider real array headers: '*' followed by digits (e.g., "*3")
                if (!IsArrayHeader(requestPart))
                {
                    logger.LogWarning("Request: {Request} CleanRequest: {CleanRequest}  Index: {Index}", request.Replace("\r", "\\r").Replace("\n", "\\n"), cleanRequest.Replace("\r", "\\r").Replace("\n", "\\n"), index);
                    logger.LogWarning("Skipping malformed array indicator: '{RequestPart}'", requestPart.Replace("\r", "\\r").Replace("\n", "\\n"));
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
            logger.LogError("An error occured during parsing RESP command. Request: {Request}", request.Replace("\r", "\\r").Replace("\n", "\\n"));
            throw;
        }
    }

    private static string SkipRdbFile(string request)
    {
        // Check if request starts with BulkString marker
        if (!request.StartsWith(DataType.BulkString))
            return request;

        var crlfIndex = request.IndexOf(RespObject.CRLF, StringComparison.Ordinal);
        if (crlfIndex == -1)
            return request;

        // Check if this looks like an RDB file (contains "REDIS" after the length marker)
        var contentStart = crlfIndex + RespObject.CRLF.Length;
        if (contentStart + 5 >= request.Length || !request.AsSpan(contentStart, 5).SequenceEqual("REDIS".AsSpan()))
            return request;
        
        // Skip everything up to the first RESP array header ('*') that follows the RDB payload.
        var nextArrayIndex = request.IndexOf(DataType.Array, contentStart);
        return nextArrayIndex == -1 ? string.Empty : request[nextArrayIndex..];
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

    private static bool IsArrayHeader(string s)
    {
        if (string.IsNullOrEmpty(s) || s[0] != DataType.Array || s.Length < 2)
            return false;

        for (int i = 1; i < s.Length; i++)
        {
            if (!char.IsDigit(s[i]))
                return false;
        }

        return true;
    }
}