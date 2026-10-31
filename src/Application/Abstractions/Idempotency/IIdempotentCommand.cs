namespace Application.Abstractions.Idempotency;

public interface IIdempotentCommand
{
    string IdempotencyKey { get; }
    string UserId { get; }
}