using System.Text.Json;

namespace RaceDay.API.Services;

public class SessionUser
{
    public int UserID { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public interface ISessionService
{
    void SetUser(HttpContext ctx, SessionUser user);
    SessionUser? GetUser(HttpContext ctx);
    void Clear(HttpContext ctx);
}

public class SessionService : ISessionService
{
    private const string Key = "RaceDayUser";

    public void SetUser(HttpContext ctx, SessionUser user) =>
        ctx.Session.SetString(Key, JsonSerializer.Serialize(user));

    public SessionUser? GetUser(HttpContext ctx)
    {
        var json = ctx.Session.GetString(Key);
        return string.IsNullOrEmpty(json)
            ? null
            : JsonSerializer.Deserialize<SessionUser>(json);
    }

    public void Clear(HttpContext ctx) => ctx.Session.Remove(Key);
}
