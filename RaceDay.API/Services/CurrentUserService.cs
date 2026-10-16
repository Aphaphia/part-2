using System.Security.Claims;

namespace RaceDay.API.Services;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Role { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    public int? UserId
    {
        get
        {
            var id = _accessor.HttpContext?.User?
                .FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var v) ? v : null;
        }
    }

    public string? Role =>
        _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
}