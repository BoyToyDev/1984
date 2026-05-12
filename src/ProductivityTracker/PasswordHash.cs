using System.Security.Cryptography;

namespace ProductivityTracker;

internal sealed record PasswordHash(string HashBase64, string SaltBase64, int Iterations)
{
    public static PasswordHash Create(string password, int iterations)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var deriveBytes = new Rfc2898DeriveBytes(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256);

        var hash = deriveBytes.GetBytes(32);
        return new PasswordHash(
            Convert.ToBase64String(hash),
            Convert.ToBase64String(salt),
            iterations);
    }
}
