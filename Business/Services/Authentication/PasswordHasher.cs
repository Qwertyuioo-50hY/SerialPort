using System.Security.Cryptography;

namespace Tomography.Business.Services.Authentication;

public static class PasswordHasher
{
    private const int Iterations = 600_000;
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encoded)
    {
        var fields = encoded.Split('$');
        if (fields.Length != 4 || fields[0] != "PBKDF2-SHA256" ||
            !int.TryParse(fields[1], out var iterations) || iterations is < 100_000 or > 2_000_000) return false;
        try
        {
            var salt = Convert.FromBase64String(fields[2]);
            var expected = Convert.FromBase64String(fields[3]);
            if (salt.Length != 16 || expected.Length != 32) return false;
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException) { return false; }
    }
}
