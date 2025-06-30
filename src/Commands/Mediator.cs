using System.Collections.Concurrent;
using System.Reflection;

namespace codecrafters_redis.Commands;

public interface IMediator
{
    TResult Send<TResult>(ICommand<TResult> command);
}

public class Mediator : IMediator
{
    private readonly ConcurrentDictionary<Type, ICommandHandler> _handlers = new();
    private readonly Dictionary<Type, Type> _handlerTypes = new();

    public Mediator()
    {
        RegisterHandlers();
    }

    private void RegisterHandlers()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))
                .Select(i => new { HandlerType = t, InterfaceType = i }))
            .ToList();

        foreach (var handler in handlerTypes)
        {
            var commandType = handler.InterfaceType.GetGenericArguments()[0];
            _handlerTypes[commandType] = handler.HandlerType;
        }
    }

    public TResult Send<TResult>(ICommand<TResult> command)
    {
        var commandType = command.GetType();
        
        var handler = _handlers.GetOrAdd(commandType, CreateHandler);
        
        return (TResult)handler.Handle(command);
    }

    private ICommandHandler CreateHandler(Type commandType)
    {
        if (!_handlerTypes.TryGetValue(commandType, out var handlerType))
        {
            throw new InvalidOperationException($"No handler registered for command type {commandType.Name}");
        }

        var handlerInstance = Activator.CreateInstance(handlerType);
        if (handlerInstance == null)
        {
            throw new InvalidOperationException($"Could not create instance of handler type {handlerType.Name}");
        }

        return (ICommandHandler)handlerInstance;
    }
}