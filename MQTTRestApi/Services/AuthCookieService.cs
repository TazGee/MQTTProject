namespace MQTTRestApi.Services;

public class AuthCookieService
{
    public const string Name = "access_token";

    public static void Set(HttpResponse response, string token, IConfiguration config)
    {
        var minutes = int.Parse(config["Jwt:ExpiryMinutes"] ?? "15");

        response.Cookies.Append(Name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = response.HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(minutes)
        });
    }

    public static void Clear(HttpResponse response) => response.Cookies.Delete(Name);
}