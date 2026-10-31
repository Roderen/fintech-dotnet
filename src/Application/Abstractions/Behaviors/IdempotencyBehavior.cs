using Application.Abstractions.Idempotency;
using MediatR;
using RedLockNet;

namespace Application.Abstractions.Behaviors;

public class IdempotencyBehavior<TRequest, TResponse>(IDistributedLockFactory lockFactory) 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (request is not IIdempotentCommand idempotentCommand)
        {
            return await next();
        }

        string resource = $"lock:user_{idempotentCommand.UserId}:{idempotentCommand.IdempotencyKey}";
        
        await using var redLock = await lockFactory.CreateLockAsync(
            resource, 
            expiryTime: TimeSpan.FromSeconds(30),
            waitTime: TimeSpan.FromSeconds(10),
            retryTime: TimeSpan.FromSeconds(1),
            cancellationToken: cancellationToken
        );
        
        if (!redLock.IsAcquired)
        {
            throw new InvalidOperationException("Transaction is already being processed. Please wait.");
        }
        
        return await next();
    }
}