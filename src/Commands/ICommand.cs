using System.Net.Sockets;
using codecrafters_redis.Resp;

namespace codecrafters_redis.Commands;

public interface ICommand
{
    Task Handle(Socket connection, RespObject[] args);
}