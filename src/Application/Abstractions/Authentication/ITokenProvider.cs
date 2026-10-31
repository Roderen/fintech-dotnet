namespace Application.Abstractions.Authentication;

public interface ITokenProvider
{
    AccessToken CreateToken(Guid userId);
}