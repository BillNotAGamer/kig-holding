using System.Security.Cryptography;

namespace KIGHolding.Services;

public static class AdminSecurityStampGenerator
{
    public static string Create()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }
}
