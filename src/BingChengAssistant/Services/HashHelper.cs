using System.Security.Cryptography;
using System.Text;

namespace BingChengAssistant.Services;

public static class HashHelper
{
    public static string Sha256(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }

    public static bool Verify(string input, string hash) => Sha256(input) == hash;
}
