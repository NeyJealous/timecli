using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TimeClickers.PortCore;

/// <summary>
/// Compatibility implementation for the Time Clickers 1.4.5 legacy save format.
/// This mirrors the old format for migration/import only and is not intended
/// as a cryptographic design for new saves.
/// </summary>
public static class SaveCrypto
{
    public const string KeyText = "l5FUP7guJYYz7vBFuFTNuLqZmS2dD5j0";
    public const int IvBytes = 32;
    public const int KeySizeBits = 256;
    public const int BlockSizeBits = 256;

    public static string EncryptString(string plaintext)
    {
#pragma warning disable SYSLIB0022
        using var rijndael = new RijndaelManaged
        {
            Mode = CipherMode.CBC,
            Padding = PaddingMode.Zeros,
            KeySize = KeySizeBits,
            BlockSize = BlockSizeBits,
            Key = Encoding.UTF8.GetBytes(KeyText)
        };
        rijndael.GenerateIV();

        byte[] ciphertext;
        using (var ms = new MemoryStream())
        {
            using (var cs = new CryptoStream(ms, rijndael.CreateEncryptor(), CryptoStreamMode.Write))
            {
                byte[] plain = Encoding.ASCII.GetBytes(plaintext);
                cs.Write(plain, 0, plain.Length);
                cs.FlushFinalBlock();
            }
            ciphertext = ms.ToArray();
        }

        var packed = new byte[IvBytes + ciphertext.Length];
        rijndael.IV.CopyTo(packed, 0);
        ciphertext.CopyTo(packed, IvBytes);
#pragma warning restore SYSLIB0022

        return Convert.ToBase64String(AddNoise(packed));
    }

    public static string DecryptString(string encoded)
    {
        byte[] packed = RemoveNoise(Convert.FromBase64String(encoded));
        var ciphertext = new byte[packed.Length - IvBytes];
        Buffer.BlockCopy(packed, IvBytes, ciphertext, 0, ciphertext.Length);

        var iv = new byte[IvBytes];
        Buffer.BlockCopy(packed, 0, iv, 0, IvBytes);

#pragma warning disable SYSLIB0022
        using var rijndael = new RijndaelManaged
        {
            Mode = CipherMode.CBC,
            Padding = PaddingMode.Zeros,
            KeySize = KeySizeBits,
            BlockSize = BlockSizeBits,
            Key = Encoding.UTF8.GetBytes(KeyText),
            IV = iv
        };

        using var ms = new MemoryStream(ciphertext);
        using var cs = new CryptoStream(ms, rijndael.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadLine() ?? string.Empty;
#pragma warning restore SYSLIB0022
    }

    public static byte[] AddNoise(byte[] input)
    {
        string noise = Md5Sum(KeyText);
        var output = new byte[input.Length];

        for (int i = 0, j = 0; i < input.Length; i++, j++)
        {
            if (j >= noise.Length) j = 0;
            output[i] = (byte)((input[i] + noise[j]) % 256);
        }

        return output;
    }

    public static byte[] RemoveNoise(byte[] input)
    {
        string noise = Md5Sum(KeyText);
        var output = new byte[input.Length];

        for (int i = 0, j = 0; i < input.Length; i++, j++)
        {
            if (j >= noise.Length) j = 0;
            int value = input[i] - noise[j];
            if (value < 0) value += 256;
            output[i] = (byte)value;
        }

        return output;
    }

    private static string Md5Sum(string input)
    {
        using var utf8 = new UTF8Encoding();
#pragma warning disable SYSLIB0021
        using var md5 = new MD5CryptoServiceProvider();
#pragma warning restore SYSLIB0021
        byte[] hash = md5.ComputeHash(utf8.GetBytes(input));

        var sb = new StringBuilder();
        foreach (byte b in hash)
            sb.Append(b.ToString("x").PadLeft(2, '0'));

        return sb.ToString().PadLeft(32, '0');
    }
}
