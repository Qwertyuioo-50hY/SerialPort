using Microsoft.Data.Sqlite;

namespace Tomography.Data.Storage;

public sealed record LocalAccount(long Id, string Username, string PasswordHash, bool IsEnabled);

public sealed class AccountStore(string connectionString)
{
    private SqliteConnection Open()
    {
        var options = new SqliteConnectionStringBuilder(connectionString);
        if (options.Mode != SqliteOpenMode.Memory && options.DataSource != ":memory:")
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.DataSource))!);
        var connection = new SqliteConnection(connectionString);
        try
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                PRAGMA foreign_keys=ON;
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY, Username TEXT NOT NULL,
                    NormalizedUsername TEXT NOT NULL UNIQUE, PasswordHash TEXT NOT NULL,
                    CreatedAt INTEGER NOT NULL, LastLoginAt INTEGER, IsEnabled INTEGER NOT NULL DEFAULT 1);
                CREATE TABLE IF NOT EXISTS LoginTokens (
                    Id INTEGER PRIMARY KEY, UserId INTEGER NOT NULL REFERENCES Users(Id),
                    TokenHash TEXT NOT NULL UNIQUE, CreatedAt INTEGER NOT NULL,
                    ExpiresAt INTEGER NOT NULL, RevokedAt INTEGER);
                """;
            command.ExecuteNonQuery();
            return connection;
        }
        catch { connection.Dispose(); throw; }
    }

    public bool Create(string username, string normalizedUsername, string passwordHash)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Users (Username, NormalizedUsername, PasswordHash, CreatedAt)
            VALUES ($name, $normalized, $password, $now) ON CONFLICT(NormalizedUsername) DO NOTHING;
            """;
        command.Parameters.AddWithValue("$name", username);
        command.Parameters.AddWithValue("$normalized", normalizedUsername);
        command.Parameters.AddWithValue("$password", passwordHash);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        return command.ExecuteNonQuery() == 1;
    }

    public LocalAccount? Find(string normalizedUsername)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Username, PasswordHash, IsEnabled FROM Users WHERE NormalizedUsername=$name";
        command.Parameters.AddWithValue("$name", normalizedUsername);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadAccount(reader) : null;
    }

    private static LocalAccount ReadAccount(SqliteDataReader reader) =>
        new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3));

    public void RecordLogin(long userId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Users SET LastLoginAt=$now WHERE Id=$id AND IsEnabled=1";
        command.Parameters.AddWithValue("$id", userId);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("账号已停用。");
    }

    public void AddToken(long userId, string hash, DateTimeOffset expires)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO LoginTokens (UserId, TokenHash, CreatedAt, ExpiresAt)
            SELECT Id, $hash, $now, $expires FROM Users WHERE Id=$id AND IsEnabled=1;
            """;
        command.Parameters.AddWithValue("$id", userId);
        command.Parameters.AddWithValue("$hash", hash);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        command.Parameters.AddWithValue("$expires", expires.ToUnixTimeSeconds());
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("账号已停用。");
    }

    public LocalAccount? FindToken(string hash)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT u.Id, u.Username, u.PasswordHash, u.IsEnabled FROM Users u
            JOIN LoginTokens t ON t.UserId=u.Id WHERE t.TokenHash=$hash
            AND t.RevokedAt IS NULL AND t.ExpiresAt>$now AND u.IsEnabled=1;
            """;
        command.Parameters.AddWithValue("$hash", hash);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadAccount(reader) : null;
    }

    public void RevokeToken(string hash)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE LoginTokens SET RevokedAt=$now WHERE TokenHash=$hash AND RevokedAt IS NULL";
        command.Parameters.AddWithValue("$hash", hash);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        command.ExecuteNonQuery();
    }
}
