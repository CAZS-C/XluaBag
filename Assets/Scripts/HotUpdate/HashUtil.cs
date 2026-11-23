using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// 文件哈希工具（MD5）。
/// 目前包体很小，同步读文件足够；如果以后上大包，需要改成异步分块计算。
/// </summary>
public static class HashUtil
{
    public static string Md5File(string path)
    {
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        using (MD5 md5 = MD5.Create())
        using (FileStream stream = File.OpenRead(path))
        {
            byte[] hash = md5.ComputeHash(stream);
            StringBuilder sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
