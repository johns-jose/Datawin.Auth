namespace DataWin.Auth.Application.Abstractions;

public interface ICommand<TResult>
{
    Guid TenantId { get; }
}

public interface IQuery<TResult>
{
    Guid TenantId { get; }
}

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}
