using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MQTTnet.Protocol;
using MQTTRestApi.Data;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Exceptions;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Domain.Services;
using MQTTRestApi.Services;

namespace MqttRestApi.Tests;

/// <summary>
/// Expanded, data-driven unit regression matrix based on the 313-case QA plan.
/// Some assertions are intentionally stricter than the current implementation:
/// failures can reveal missing validation or security controls.
/// </summary>
public class TestPlan313UnitTests
{
    private const string ExistingPassword = "Correct!123";

    private static (AuthService Service, AppDbContext Db, Moq.Mock<ILoggerService> Logger) CreateAuth()
    {
        var db = TestHelpers.CreateDb();
        var logger = new Moq.Mock<ILoggerService>();
        return (new AuthService(db, logger.Object), db, logger);
    }

    private static async Task<User> SeedUser(AppDbContext db, string username = "qa-user", string password = ExistingPassword,
        UserTypes role = UserTypes.User)
    {
        var user = new User(username, BCrypt.Net.BCrypt.HashPassword(password), role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static IEnumerable<object[]> RegistrationCases()
    {
        var cases = new List<object[]>();
        for (var i = 0; i < 8; i++)
            cases.Add(new object[] { $"qa_valid_{i}", $"GoodPass!{i}x", (int)UserTypes.User, false, false });

        cases.AddRange(new[]
        {
            new object[] { "", "GoodPass!1x", (int)UserTypes.User, true, false },
            new object[] { "   ", "GoodPass!1x", (int)UserTypes.User, true, false },
            new object[] { new string('u', 5000), "GoodPass!1x", (int)UserTypes.User, true, false },
            new object[] { "null-pass", "", (int)UserTypes.User, true, false },
            new object[] { "space-pass", "     ", (int)UserTypes.User, true, false },
            new object[] { "short-pass", "x", (int)UserTypes.User, true, false },
            new object[] { "unicode-pass", "žaba🔐!Pass9", (int)UserTypes.User, false, false },
            new object[] { "sql-' OR 1=1 --", "GoodPass!1x", (int)UserTypes.User, false, false },
            new object[] { "<script>alert(1)</script>", "GoodPass!1x", (int)UserTypes.User, false, false },
            new object[] { "name.with+symbols@example.test", "GoodPass!1x", (int)UserTypes.User, false, false },
            new object[] { "admin-escalation", "GoodPass!1x", (int)UserTypes.Admin, true, false },
            new object[] { "invalid-role", "GoodPass!1x", 999, true, false },
            new object[] { "duplicate", "GoodPass!1x", (int)UserTypes.User, true, true },
            new object[] { "duplicate-case", "GoodPass!1x", (int)UserTypes.User, true, true },
            new object[] { "nul-char-\0", "GoodPass!1x", (int)UserTypes.User, true, false },
            new object[] { "tab\tname", "GoodPass!1x", (int)UserTypes.User, true, false },
            new object[] { "line\nname", "GoodPass!1x", (int)UserTypes.User, true, false },
            new object[] { "emoji-😀", "Pass😀!123", (int)UserTypes.User, false, false },
            new object[] { "mixed_CASE_123", "Aa1!Aa1!Aa1!", (int)UserTypes.User, false, false },
            new object[] { "a", "GoodPass!1x", (int)UserTypes.User, false, false },
            new object[] { "qa-user", ExistingPassword, (int)UserTypes.User, true, true },
            new object[] { "null-role", "GoodPass!1x", -1, true, false },
            new object[] { "very-long-pass", new string('P', 100), (int)UserTypes.User, true, false },
            new object[] { "quotes-\"'`", "GoodPass!1x", (int)UserTypes.User, false, false },
            new object[] { "slash\\backslash", "GoodPass!1x", (int)UserTypes.User, false, false },
            new object[] { "accent-é-ø-漢字", "GoodPass!1x", (int)UserTypes.User, false, false },
            new object[] { "colon:semi;pipe|", "GoodPass!1x", (int)UserTypes.User, false, false }
        });
        return cases;
    }

    [Theory]
    [MemberData(nameof(RegistrationCases))]
    public async Task Register_InputBoundaryMatrix(string username, string password, int roleValue, bool expectReject, bool duplicate)
    {
        var (service, db, _) = CreateAuth();
        if (duplicate)
            await SeedUser(db, username, ExistingPassword);

        if (expectReject)
        {
            await Assert.ThrowsAsync<BadRequestException>(() => service.Register(username, password, (UserTypes)roleValue));
            return;
        }

        var result = await service.Register(username, password, (UserTypes)roleValue);
        var saved = await db.Users.SingleAsync(u => u.Id == result.Id);
        Assert.Equal(username, saved.Username);
        Assert.NotEqual(password, saved.Password);
        Assert.True(BCrypt.Net.BCrypt.Verify(password, saved.Password));
    }

    public static IEnumerable<object[]> LoginCases()
    {
        yield return new object[] { "known-user", ExistingPassword, true };
        var bad = new (string Username, string Password)[]
        {
            ("known-user", "wrong"), ("missing", ExistingPassword), ("", ""), (" ", " "),
            ("KNOWN-USER", ExistingPassword), ("known-user", ""), ("known-user", " "),
            ("known-user", "Correct!124"), ("known-user", "correct!123"), ("known-user", "CORRECT!123"),
            ("unknown' OR 1=1 --", "anything"), ("<script>", "anything"), ("😀", "😀"),
            ("known-user ", ExistingPassword), (" known-user", ExistingPassword),
            (new string('u', 1000), "anything"), ("known-user", new string('x', 1000)),
            ("nul\0user", "password"), ("line\nuser", "password"), ("tab\tuser", "password"),
            ("žaba", "šifra"), ("admin", "admin"), ("root", "toor"), ("null", "null")
        };
        foreach (var row in bad) yield return new object[] { row.Username, row.Password, false };
    }

    [Theory]
    [MemberData(nameof(LoginCases))]
    public async Task Login_CredentialMatrix(string username, string password, bool expectSuccess)
    {
        var (service, db, _) = CreateAuth();
        await SeedUser(db, "known-user", ExistingPassword, UserTypes.User);
        if (expectSuccess)
        {
            var user = await service.Login(username, password);
            Assert.Equal("known-user", user.Username);
        }
        else
        {
            await Assert.ThrowsAsync<BadRequestException>(() => service.Login(username, password));
        }
    }

    public static IEnumerable<object[]> PasswordChangeCases()
    {
        var cases = new List<object[]>();
        var validNewPasswords = new[]
        {
            "NewPass!123", "another_1234", "ééééPass!9", "🔐SafePass9!", "spaces are allowed?1",
            "punctuation!@#$%^&*()", "MixedCase123!", "1234567890Aa!", "xYz!987654", "with.dots-123!",
            "tab\tpass123!", "line\npass123!", "zero\0charPass!", "a".PadRight(20, 'b'), "final-pass-99!"
        };
        foreach (var pass in validNewPasswords) cases.Add(new object[] { ExistingPassword, pass, true });
        var wrongOldPasswords = new[] { "wrong", "", " ", "correct!123", "Correct!124", "admin", "null", "😀", "password", "Correct!123 " };
        foreach (var old in wrongOldPasswords) cases.Add(new object[] { old, "NewPass!123", false });
        var weakNewPasswords = new[] { "", " ", "x", "123", "pasword", "short", "  00000      ", "aaaaa      ", "456345" };
        foreach (var pass in weakNewPasswords) cases.Add(new object[] { ExistingPassword, pass, false });
        return cases;
    }

    [Theory]
    [MemberData(nameof(PasswordChangeCases))]
    public async Task ChangePassword_BoundaryMatrix(string oldPassword, string newPassword, bool expectSuccess)
    {
        using var db = TestHelpers.CreateDb();
        var service = new UsersService(db);
        var user = await SeedUser(db, "password-user", ExistingPassword);
        var dto = new ChangePasswordDto { OldPassword = oldPassword, NewPassword = newPassword };

        if (!expectSuccess)
        {
            await Assert.ThrowsAsync<BadRequestException>(() => service.ChangePassword(user.Id, dto));
            return;
        }

        await service.ChangePassword(user.Id, dto);
        var updated = await db.Users.FindAsync(user.Id);
        Assert.NotNull(updated);
        Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, updated!.Password));
    }

    public static IEnumerable<object[]> RoleChangeCases()
    {
        var cases = new List<object[]>();
        for (var i = 0; i < 12; i++)
            cases.Add(new object[] { "target", i % 2, false });
        for (var i = 0; i < 8; i++)
            cases.Add(new object[] { "self", i % 2, true });
        for (var i = 0; i < 6; i++)
            cases.Add(new object[] { "missing", i % 2, true });
        cases.Add(new object[] { "target", 999, true });
        cases.Add(new object[] { "target", -1, true });
        cases.Add(new object[] { "target", 2, true });
        return cases;
    }

    [Theory]
    [MemberData(nameof(RoleChangeCases))]
    public async Task ChangeRole_TargetAndRoleMatrix(string targetMode, int roleValue, bool expectReject)
    {
        using var db = TestHelpers.CreateDb();
        var service = new UsersService(db);
        var admin = await SeedUser(db, "role-admin", ExistingPassword, UserTypes.Admin);
        var target = await SeedUser(db, "role-target", ExistingPassword, UserTypes.User);
        var targetId = targetMode switch { "self" => admin.Id, "missing" => 999999, _ => target.Id };
        var role = (UserTypes)roleValue;

        if (expectReject)
        {
            if (targetMode == "self" || targetMode == "missing")
                await Assert.ThrowsAsync<NotFoundException>(() => service.ChangeRole(admin.Id, targetId, role));
            else
                await Assert.ThrowsAsync<BadRequestException>(() => service.ChangeRole(admin.Id, targetId, role));
            return;
        }

        await service.ChangeRole(admin.Id, targetId, role);
        Assert.Equal(role, (await db.Users.FindAsync(target.Id))!.Role);
    }

    public static IEnumerable<object[]> AlarmBoundaryCases()
    {
        var cases = new List<object[]>();
        for (var i = 0; i < 15; i++)
        {
            var lower = i - 20;
            var upper = i + 1;
            cases.Add(new object[] { (int)AlarmTypes.Both, (double)lower, (double)upper, false });
        }
        foreach (var bounds in new[] { (10d, 10d), (30d, 10d), (0d, 0d), (-1d, -1d), (double.NaN, 10d), (10d, double.NaN), (double.PositiveInfinity, double.PositiveInfinity) })
            cases.Add(new object[] { (int)AlarmTypes.Both, bounds.Item1, bounds.Item2, true });
        for (var i = 0; i < 6; i++) cases.Add(new object[] { (int)AlarmTypes.Minimum, (double)(i * 10), (double)(i - 30), false });
        for (var i = 0; i < 6; i++) cases.Add(new object[] { (int)AlarmTypes.Maximum, (double)(i * 10), (double)(i - 30), false });
        cases.Add(new object[] { (int)AlarmTypes.Both, double.NegativeInfinity, 10d, true });
        cases.Add(new object[] { 999, 10d, 30d, true });
        cases.Add(new object[] { -1, 10d, 30d, true });
        cases.Add(new object[] { (int)AlarmTypes.Both, double.MaxValue, double.MinValue, true });
        cases.Add(new object[] { (int)AlarmTypes.Both, double.MinValue, double.MaxValue, false });
        return cases;
    }

    [Theory]
    [MemberData(nameof(AlarmBoundaryCases))]
    public async Task CreateAlarm_LimitsAndEnumMatrix(int typeValue, double lower, double upper, bool expectReject)
    {
        using var db = TestHelpers.CreateDb();
        var logger = new Moq.Mock<ILoggerService>();
        var service = new AlarmService(db, logger.Object);
        var topic = new Topic("qa/alarm", "test");
        db.Topics.Add(topic);
        await db.SaveChangesAsync();

        var action = () => service.CreateAlarm(new CreateAlarmDto(topic.Id, (AlarmTypes)typeValue, lower, upper));
        if (expectReject)
        {
            await Assert.ThrowsAsync<BadRequestException>(action);
            return;
        }

        await action();
        var saved = await db.Alarms.SingleAsync();
        Assert.Equal(typeValue, (int)saved.AlarmType);
        Assert.Equal(lower, saved.LowerLimit);
        Assert.Equal(upper, saved.UpperLimit);
    }

    public static IEnumerable<object[]> TokenCases()
    {
        var names = new[]
        {
            "qa-user", "user with spaces", "žaba", "漢字", "emoji-😀", "quote'\"", "<script>alert(1)</script>",
            "sql-' OR 1=1 --", "under_score", "dot.name", "plus+name", "semi;colon", "slash/name", "back\\slash",
            "", " ", "A", "MixedCase", new string('u', 256), "name-é-ø-ß"
        };
        for (var i = 0; i < names.Length; i++)
            yield return new object[] { (i + 1).ToString(), names[i], i % 2 == 0 ? "User" : "Admin" };
    }

    [Theory]
    [MemberData(nameof(TokenCases))]
    public void CreateToken_ClaimsAndSignatureMatrix(string userId, string username, string role)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "01234567890123456789012345678901",
            ["Jwt:Issuer"] = "MQTTRestApi.Tests",
            ["Jwt:Audience"] = "MQTTRestApi.Client",
            ["Jwt:ExpiryMinutes"] = "15"
        }).Build();
        var service = new TokenService(config);
        var raw = service.CreateToken(userId, username, role);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(raw);

        Assert.Equal("MQTTRestApi.Tests", token.Issuer);
        Assert.Contains("MQTTRestApi.Client", token.Audiences);
        Assert.Equal(userId, token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(username, token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.True(token.Claims.Any(c => (c.Type == "role" || c.Type == ClaimTypes.Role) && c.Value == role));
        Assert.True(token.ValidTo > DateTime.UtcNow);
    }

    public static IEnumerable<object[]> LoggerCases()
    {
        var messages = new[]
        {
            "normal log", "", " ", "Unicode žđčćš", "emoji 😀🚀", "SQL ' OR 1=1 --", "<script>alert(1)</script>",
            new string('x', 4000), "line1\nline2", "tab\tvalue", "nul\0char", "json {\"x\":true}",
            "topic: a/b/#", "negative value -999", "decimal 1.2345", "emoji 🔥", "quotes \"double\" 'single'", "end"
        };
        var types = Enum.GetValues<LogTypes>();
        for (var i = 0; i < messages.Length; i++)
            yield return new object[] { messages[i], types[i % types.Length] };
    }

    [Theory]
    [MemberData(nameof(LoggerCases))]
    public async Task Logger_PersistsBoundaryMessages(string message, LogTypes type)
    {
        using var db = TestHelpers.CreateDb();
        var service = new LoggerService(db);
        await service.LogMessage(message, type);
        var saved = await db.Logs.SingleAsync();
        Assert.Equal(message, saved.Message);
        Assert.Equal(type, saved.Type);
        Assert.Contains(type.ToString(), saved.ToString());
    }

    public static IEnumerable<object[]> ModelCases()
    {
        var values = new[]
        {
            "", " ", "normal", "Unicode žđčćš", "emoji 😀", "<script>", "' OR 1=1 --", "a/b", "#", "+",
            "line\nvalue", "tab\tvalue", "quote'\"", new string('x', 2000), "underscore_name", "dash-name",
            "dots.name", "semi;colon", "colon:value", "brackets[]{}", "slash\\backslash", "漢字", "éøß", "-1", "0"
        };
        for (var i = 0; i < values.Length; i++)
            yield return new object[] { i % 4, values[i], (double)i - 10, (double)i + 10 };
    }

    [Theory]
    [MemberData(nameof(ModelCases))]
    public void DomainModels_ConstructorAndStringMatrix(int modelKind, string value, double lower, double upper)
    {
        switch (modelKind)
        {
            case 0:
            {
                var user = new User(value, "hash", UserTypes.User);
                Assert.Equal(value, user.Username);
                Assert.Equal("hash", user.Password);
                Assert.Equal(UserTypes.User, user.Role);
                break;
            }
            case 1:
            {
                var topic = new Topic(value, "description");
                Assert.Equal(value, topic.Name);
                Assert.Equal("description", topic.Description);
                break;
            }
            case 2:
            {
                var alarm = new Alarm(7, AlarmTypes.Both, lower, upper);
                Assert.Equal(7, alarm.TopicId);
                Assert.Equal(lower, alarm.LowerLimit);
                Assert.Equal(upper, alarm.UpperLimit);
                break;
            }
            default:
            {
                var message = new MqttMessage("root/" + value, value, MqttQualityOfServiceLevel.AtMostOnce,
                    false, DateTime.UtcNow, ValueAlarmTypes.NONE);
                Assert.Equal(value, message.Payload);
                Assert.Contains("root/" + value, message.ToString());
                Assert.Contains(value, message.ToString());
                break;
            }
        }
    }
}
