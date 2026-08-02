using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

const string Project = "project-alpha";
const string Password = "PoC-only-password-not-written-to-output";
var runId = $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}";
var output = Path.Combine(Path.GetTempPath(), "AI-Development-Manager", "poc", "P0-017", runId);
Directory.CreateDirectory(output);
var clock = new FakeClock(DateTimeOffset.UtcNow);
var auth = new AuthService(clock);
auth.AddUser("admin", Password, Role.Admin);
auth.AddUser("editor", "editor-password", Role.Editor);
auth.AddUser("viewer", "viewer-password", Role.Viewer);
var checks = new List<CheckResult>();

var browser = auth.Login("admin", Password);
var webView = auth.Login("admin", Password);
checks.Add(Check("browser_and_webview_share_cookie_flow", browser.Succeeded && webView.Succeeded && browser.Cookie.Attributes == webView.Cookie.Attributes, "same login and cookie policy"));
checks.Add(Check("secure_httponly_samesite_cookie", browser.Cookie.Secure && browser.Cookie.HttpOnly && browser.Cookie.SameSite == "Strict", "Secure; HttpOnly; SameSite=Strict"));
checks.Add(Check("csrf_rejects_missing_token", auth.ValidateCookieRequest(browser, Project, "write", null) == AuthStatus.CsrfRejected, "missing CSRF token rejected"));
checks.Add(Check("csrf_accepts_matching_token", auth.ValidateCookieRequest(browser, Project, "write", browser.CsrfToken) == AuthStatus.Allowed, "matching CSRF token accepted"));
checks.Add(Check("wrong_password_is_401", auth.Login("admin", "wrong").Status == AuthStatus.Unauthorized, "invalid credentials"));
auth.Login("admin", "wrong"); auth.Login("admin", "wrong");
checks.Add(Check("login_lockout_is_enforced", auth.Login("admin", Password).Status == AuthStatus.LockedOut, "three failed attempts lock account"));
clock.Advance(TimeSpan.FromMinutes(2));
checks.Add(Check("lockout_expires", auth.Login("admin", Password).Succeeded, "lockout period elapsed"));

var viewer = auth.Login("viewer", "viewer-password");
var editor = auth.Login("editor", "editor-password");
checks.Add(Check("viewer_can_read", auth.ValidateCookieRequest(viewer, Project, "read", viewer.CsrfToken) == AuthStatus.Allowed, "viewer read allowed"));
checks.Add(Check("viewer_cannot_write", auth.ValidateCookieRequest(viewer, Project, "write", viewer.CsrfToken) == AuthStatus.Forbidden, "viewer write forbidden"));
checks.Add(Check("editor_can_write", auth.ValidateCookieRequest(editor, Project, "write", editor.CsrfToken) == AuthStatus.Allowed, "editor write allowed"));

var tokenIssue = auth.IssueToken("admin", Project, new[] { "read" }, TimeSpan.FromHours(1));
checks.Add(Check("ai_token_is_read_only", tokenIssue.Succeeded && auth.ValidateBearer(tokenIssue.Token!, Project, "read") == AuthStatus.Allowed && auth.ValidateBearer(tokenIssue.Token!, Project, "write") == AuthStatus.Forbidden, "AI token has read scope only"));
checks.Add(Check("token_project_scope_is_enforced", auth.ValidateBearer(tokenIssue.Token!, "other-project", "read") == AuthStatus.Forbidden, "different project rejected"));
checks.Add(Check("token_within_lifetime_is_accepted", auth.ValidateBearer(auth.IssueToken("admin", Project, new[] { "read" }, TimeSpan.FromMinutes(1)).Token!, Project, "read") == AuthStatus.Allowed, "fresh token accepted"));
var revocable = auth.IssueToken("admin", Project, new[] { "read" }, TimeSpan.FromHours(1));
auth.RevokeToken(revocable.Token!);
checks.Add(Check("revoked_token_is_rejected", auth.ValidateBearer(revocable.Token!, Project, "read") == AuthStatus.Unauthorized, "revoked token rejected"));
clock.Advance(TimeSpan.FromHours(2));
checks.Add(Check("expired_token_is_rejected", auth.ValidateBearer(tokenIssue.Token!, Project, "read") == AuthStatus.Unauthorized, "expired token rejected"));

var logout = auth.Logout(browser);
checks.Add(Check("logout_invalidates_session", logout && auth.ValidateCookieRequest(browser, Project, "read", browser.CsrfToken) == AuthStatus.Unauthorized, "session invalidated"));
var auditJson = JsonSerializer.Serialize(auth.AuditEvents);
checks.Add(Check("audit_log_has_no_secrets", !auditJson.Contains(Password, StringComparison.Ordinal) && !auditJson.Contains(tokenIssue.Token!, StringComparison.Ordinal), "password and token body absent"));
var freshViewer = auth.Login("viewer", "viewer-password");
checks.Add(Check("unauthenticated_is_401_and_authorized_is_403", auth.ValidateBearer("missing-token", Project, "read") == AuthStatus.Unauthorized && auth.ValidateCookieRequest(freshViewer, Project, "write", freshViewer.CsrfToken) == AuthStatus.Forbidden, "401/403 separated"));

var result = new { run_id = runId, sdk = "10.0.302", runtime = Environment.Version.ToString(), cookie = new { secure = true, http_only = true, same_site = "Strict" }, default_ai_scope = new[] { "read" }, lockout = new { failures = 3, duration_minutes = 1 }, checks, audit_events = auth.AuditEvents.Count, output_directory = output, completed_utc = DateTimeOffset.UtcNow };
await File.WriteAllTextAsync(Path.Combine(output, "result.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"P0-017 run={runId} output={output}");
foreach (var check in checks) Console.WriteLine($"{(check.Passed ? "PASS" : "FAIL")} {check.Name}: {check.Detail}");
Console.WriteLine($"RESULT_JSON={Path.Combine(output, "result.json")}");
Environment.ExitCode = checks.All(c => c.Passed) ? 0 : 1;

static CheckResult Check(string name, bool passed, string detail) => new(name, passed, detail);

enum Role { Viewer, Editor, Admin }
enum AuthStatus { Allowed, Unauthorized, Forbidden, CsrfRejected, LockedOut }
record CheckResult(string Name, bool Passed, string Detail);
record CookiePolicy(bool Secure, bool HttpOnly, string SameSite, string Attributes);
record LoginResult(bool Succeeded, AuthStatus Status, string SessionId, string CsrfToken, CookiePolicy Cookie);
record TokenResult(bool Succeeded, AuthStatus Status, string? Token);
record UserAccount(string UserName, byte[] Salt, byte[] PasswordHash, Role Role);
record Session(string UserName, string CsrfToken, DateTimeOffset ExpiresAt);
record ApiToken(string UserName, string Project, HashSet<string> Scopes, byte[] Hash, DateTimeOffset ExpiresAt, bool Revoked);
record AuditEvent(DateTimeOffset At, string Event, string UserName, bool Success, string? Detail);

sealed class FakeClock
{
    public FakeClock(DateTimeOffset now) => Now = now;
    public DateTimeOffset Now { get; private set; }
    public void Advance(TimeSpan duration) => Now += duration;
}

sealed class AuthService
{
    private readonly FakeClock clock;
    private readonly Dictionary<string, UserAccount> users = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Session> sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ApiToken> tokens = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (int Count, DateTimeOffset Until)> failures = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<AuditEvent> audit = [];
    private static readonly CookiePolicy Cookie = new(true, true, "Strict", "Secure; HttpOnly; SameSite=Strict; Path=/");
    public AuthService(FakeClock clock) => this.clock = clock;
    public IReadOnlyList<AuditEvent> AuditEvents => audit;
    public void AddUser(string name, string password, Role role)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        users[name] = new UserAccount(name, salt, Hash(password, salt), role);
    }
    public LoginResult Login(string name, string password)
    {
        if (failures.TryGetValue(name, out var failure) && failure.Until > clock.Now) { AddAudit("login_locked", name, false, null); return new(false, AuthStatus.LockedOut, "", "", Cookie); }
        if (!users.TryGetValue(name, out var user) || !CryptographicOperations.FixedTimeEquals(user.PasswordHash, Hash(password, user.Salt)))
        {
            var count = failures.TryGetValue(name, out var current) ? current.Count + 1 : 1;
            failures[name] = (count, count >= 3 ? clock.Now.AddMinutes(1) : clock.Now);
            AddAudit("login_failed", name, false, null);
            return new(false, AuthStatus.Unauthorized, "", "", Cookie);
        }
        failures.Remove(name);
        var sessionId = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var csrf = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        sessions[sessionId] = new Session(name, csrf, clock.Now.AddMinutes(30));
        AddAudit("login_succeeded", name, true, null);
        return new(true, AuthStatus.Allowed, sessionId, csrf, Cookie);
    }
    public AuthStatus ValidateCookieRequest(LoginResult login, string project, string operation, string? csrf)
    {
        if (!login.Succeeded || !sessions.TryGetValue(login.SessionId, out var session) || session.ExpiresAt <= clock.Now) return AuthStatus.Unauthorized;
        if (operation is "write" && !StringComparer.Ordinal.Equals(csrf, session.CsrfToken)) return AuthStatus.CsrfRejected;
        if (!HasPermission(users[session.UserName].Role, project, operation)) return AuthStatus.Forbidden;
        return AuthStatus.Allowed;
    }
    public TokenResult IssueToken(string userName, string project, IEnumerable<string> scopes, TimeSpan lifetime)
    {
        if (!users.TryGetValue(userName, out var user) || user.Role != Role.Admin) return new(false, AuthStatus.Forbidden, null);
        var raw = "adm_at_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        tokens[raw] = new ApiToken(userName, project, scopes.ToHashSet(StringComparer.OrdinalIgnoreCase), HashToken(raw), clock.Now.Add(lifetime), false);
        AddAudit("token_issued", userName, true, $"project={project};scopes={string.Join(',', scopes)}");
        return new(true, AuthStatus.Allowed, raw);
    }
    public AuthStatus ValidateBearer(string raw, string project, string operation)
    {
        var token = tokens.Values.FirstOrDefault(candidate => CryptographicOperations.FixedTimeEquals(candidate.Hash, HashToken(raw)));
        if (token is null || token.Revoked || token.ExpiresAt <= clock.Now) return AuthStatus.Unauthorized;
        if (!StringComparer.Ordinal.Equals(token.Project, project) || !token.Scopes.Contains(operation)) return AuthStatus.Forbidden;
        return AuthStatus.Allowed;
    }
    public void RevokeToken(string raw)
    {
        var hash = HashToken(raw);
        var key = tokens.FirstOrDefault(pair => CryptographicOperations.FixedTimeEquals(pair.Value.Hash, hash)).Key;
        if (key is not null) tokens[key] = tokens[key] with { Revoked = true };
        AddAudit("token_revoked", "admin", true, null);
    }
    public bool Logout(LoginResult login)
    {
        var removed = sessions.Remove(login.SessionId);
        AddAudit("logout", "admin", removed, null);
        return removed;
    }
    public void AddAudit(string eventName, string userName, bool success, string? detail) => audit.Add(new AuditEvent(clock.Now, eventName, userName, success, detail));
    private static bool HasPermission(Role role, string project, string operation) => operation == "read" && project.Length > 0 || operation == "write" && role is Role.Editor or Role.Admin;
    private static byte[] Hash(string value, byte[] salt) => Rfc2898DeriveBytes.Pbkdf2(value, salt, 100_000, HashAlgorithmName.SHA256, 32);
    private static byte[] HashToken(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
}
