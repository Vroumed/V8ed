using System.Security.Cryptography;
using System.Text;

namespace V8ed.Utils;

public static class StringUtils
{
    public static bool ToBool(this string str)
    {
        str = str.ToLowerInvariant().Trim();
        return str is "true" or "1";
    }

    public static string RandomString(int length, bool caseVariant = false, bool specialchars = false, bool numericals = false)
    {
        string chars = "abcdefghijklmnopqrstuvwxyz";
        if (caseVariant)
            chars += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (specialchars)
            chars += "!@#$%^&*()_+{}[]:;\"'\\|,.<>?/";
        if (numericals)
            chars += "0123456789";

        char[] stringChars = new char[length];
        Random random = new Random();

        for (int i = 0; i < stringChars.Length; i++)
            stringChars[i] = chars[random.Next(chars.Length)];

        return new string(stringChars);
    }

    public static string CalculateMD5Hash(this string input)
    {
        using MD5 md5 = MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        StringBuilder builder = new StringBuilder();
        foreach (byte t in hashBytes)
            builder.Append(t.ToString("x2"));

        return builder.ToString();
    }
}
