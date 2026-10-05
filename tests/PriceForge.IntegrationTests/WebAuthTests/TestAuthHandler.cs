using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PriceForge.IntegrationTests.WebAuthTests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string AuthHeader = "X-Test-Auth";

    /// <summary>
    /// Позволяет тесту задать конкретный userId — нужно там, где эндпоинт берёт пользователя
    /// из принципала и работает с его данными (корзина, заказы, FK на users).
    /// </summary>
    public const string UserIdHeader = "X-Test-UserId";

    public const string RoleHeader = "X-Test-Role";

    public const string DefaultUserId = "a0000000-0000-0000-0000-000000000001";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
                           ILoggerFactory logger,
                           UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Аутентифицируем ТОЛЬКО при наличии маркер-заголовка — иначе запрос анонимный (→ 401).
        // Это сохраняет возможность тестировать и authorized, и unauthorized пути.
        if (!Request.Headers.ContainsKey(AuthHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string userId = Request.Headers.TryGetValue(UserIdHeader, out Microsoft.Extensions.Primitives.StringValues v)
                        && !string.IsNullOrWhiteSpace(v)
            ? v.ToString()
            : DefaultUserId;

        string role = Request.Headers.TryGetValue(RoleHeader, out Microsoft.Extensions.Primitives.StringValues r)
                      && !string.IsNullOrWhiteSpace(r)
            ? r.ToString()
            : "Admin";

        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "Test Admin"),
            new Claim(ClaimTypes.Role, role)
        ];

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
