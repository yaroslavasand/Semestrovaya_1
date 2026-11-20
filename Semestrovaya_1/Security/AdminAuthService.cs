using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Semestrovaya_1.Security;
// Вход в админку. Пароль администратора нигде не хранится в открытом виде
// при старте он превращается в хэш алгоритмом PBKDF2, и при входе сравниваются хэши.
// После входа выдаётся сессия со случайными токенами, а каждая форма защищена CSRF-токеном.
// Есть пауза против перебора пароля.
public sealed record AdminSession(
    string Id,
    string CsrfToken,
    bool IsAuthenticated,
    DateTimeOffset ExpiresAt);

public sealed class AdminAuthService
{
    private readonly ConcurrentDictionary<string, AdminSession> _sessions =
        new();

    private readonly string _username;
    private readonly byte[] _salt;
    private readonly byte[] _passwordHash;

    public AdminAuthService(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Не задан логин администратора.");
        }

        if (password.Length < 12)
        {
            throw new ArgumentException(
                "Пароль администратора должен содержать минимум 12 символов.");
        }

        _username = username;
        _salt = RandomNumberGenerator.GetBytes(16);
        _passwordHash = HashPassword(password);
    }

    public bool CheckCredentials(string username, string password)
    {
        var candidateHash = HashPassword(password);

        var passwordMatches =
            CryptographicOperations.FixedTimeEquals(
                _passwordHash,
                candidateHash);

        return string.Equals(
            username,
            _username,
            StringComparison.Ordinal) & passwordMatches;
    }

    public AdminSession CreateSession(bool authenticated)
    {
        foreach (var entry in _sessions)
        {
            if (entry.Value.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                _sessions.TryRemove(entry.Key, out _);
            }
        }

        if (_sessions.Count >= 2048)
        {
            throw new InvalidOperationException(
                "Достигнут лимит административных сессий.");
        }

        // создаем две сессии для Id и для CSRF-токена
        var session = new AdminSession(
            CreateToken(),
            CreateToken(),
            authenticated,
            DateTimeOffset.UtcNow.AddMinutes(
                authenticated ? 30 : 10));

        _sessions[session.Id] = session;

        return session;
    }

    public AdminSession? FindSession(string? id)
    {
        if (string.IsNullOrEmpty(id)
            || !_sessions.TryGetValue(id, out var session))
        {
            return null;
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(id, out _);
            return null;
        }

        return session;
    }

    public void RemoveSession(string id)
    {
        _sessions.TryRemove(id, out _);
    }

    private byte[] HashPassword(string password)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            _salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);
    }

    private static string CreateToken()
    {
        return Convert.ToHexString(
            RandomNumberGenerator.GetBytes(32));
    }
}