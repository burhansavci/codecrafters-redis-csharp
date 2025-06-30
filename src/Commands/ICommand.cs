namespace codecrafters_redis.Commands;

public interface ICommand<out TResult> : IBaseCommand;

public interface IBaseCommand
{
    public static string Name => null!;
}