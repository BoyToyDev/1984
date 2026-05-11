using System.Security.Cryptography;

namespace ProductivityTracker;

internal sealed class ExitPasswordVerifier
{
    private readonly AppSettings _settings;

    public ExitPasswordVerifier(AppSettings settings)
    {
        _settings = settings;
    }

    public bool IsPasswordRequired => !_settings.AllowExitWithoutPassword
        && !string.IsNullOrWhiteSpace(_settings.ExitPasswordHashBase64)
        && !string.IsNullOrWhiteSpace(_settings.ExitPasswordSaltBase64);

    public bool Verify(string password)
    {
        if (!IsPasswordRequired)
        {
            return true;
        }

        try
        {
            var expected = Convert.FromBase64String(_settings.ExitPasswordHashBase64!);
            var salt = Convert.FromBase64String(_settings.ExitPasswordSaltBase64!);
            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                salt,
                _settings.ExitPasswordIterations,
                HashAlgorithmName.SHA256);

            var actual = deriveBytes.GetBytes(expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}
