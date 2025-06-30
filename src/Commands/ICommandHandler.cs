namespace codecrafters_redis.Commands;

public interface ICommandHandler
{
    object Handle(object command);
}

public interface ICommandHandler<in TCommand, TResult> : ICommandHandler where TCommand : ICommand<TResult>
{
    TResult Handle(TCommand command);
    
    object ICommandHandler.Handle(object command) => Handle((TCommand)command)!;
}