using System.Security.Cryptography;
using System.Text;

namespace StoryTime.Api.Domain;

public static class IdentifierHashing
{
    public static string HashIdentifier(string rawIdentifier, int byteLength)
    {
        if (string.IsNullOrWhiteSpace(rawIdentifier))
        {
            return "anonymous";
        }

        var hashed = SHA256.HashData(Encoding.UTF8.GetBytes(rawIdentifier));
        var size = Math.Clamp(byteLength, 1, hashed.Length);
        return Convert.ToHexString(hashed[..size]);
    }
}
