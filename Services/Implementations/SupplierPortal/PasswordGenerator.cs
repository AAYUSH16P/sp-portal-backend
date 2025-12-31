using System.Security.Cryptography;
using System.Text;

public static class PasswordGenerator
{
    public static string Generate(int length = 10)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@#$!";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var result = new StringBuilder(length);

        foreach (var b in bytes)
            result.Append(chars[b % chars.Length]);

        return result.ToString();
    }
}