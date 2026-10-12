using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.ComponentModel;

namespace Client.Main.Configuration;

internal static partial class LoginDetailsStore
{
    private static string WindowsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TwinMU", "login.bin");
    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob { public int Length; public IntPtr Data; }
    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(ref DataBlob input, string description, IntPtr entropy, IntPtr reserved, IntPtr prompt, uint flags, out DataBlob output);
    [DllImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(ref DataBlob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, uint flags, out DataBlob output);
    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);

    private static byte[] ProtectWindows(byte[] bytes, bool encrypt)
    {
        var input = new DataBlob { Length = bytes.Length, Data = Marshal.AllocHGlobal(bytes.Length) };
        DataBlob output = default;
        try
        {
            Marshal.Copy(bytes, 0, input.Data, bytes.Length);
            bool ok = encrypt ? CryptProtectData(ref input, "TwinMU login", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output)
                : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output);
            if (!ok) throw new Win32Exception(Marshal.GetLastWin32Error());
            var result = new byte[output.Length];
            Marshal.Copy(output.Data, result, 0, output.Length);
            return result;
        }
        finally
        {
            for (int i = 0; i < input.Length; i++) Marshal.WriteByte(input.Data, i, 0);
            Marshal.FreeHGlobal(input.Data);
            if (output.Data != IntPtr.Zero) LocalFree(output.Data);
        }
    }
    private static (string Username, string Password)? LoadWindows()
    {
        try
        {
            if (!File.Exists(WindowsPath)) return null;
            string text = Encoding.UTF8.GetString(ProtectWindows(File.ReadAllBytes(WindowsPath), false));
            int separator = text.IndexOf('\n');
            return separator > 0 ? (text[..separator], text[(separator + 1)..]) : null;
        }
        catch { try { ClearWindows(); } catch { } return null; }
    }
    private static void SaveWindows(string username, string password)
    {
        byte[] data = ProtectWindows(Encoding.UTF8.GetBytes(username + "\n" + password), true);
        Directory.CreateDirectory(Path.GetDirectoryName(WindowsPath)!);
        File.WriteAllBytes(WindowsPath + ".tmp", data);
        File.Move(WindowsPath + ".tmp", WindowsPath, true);
    }
    private static void ClearWindows()
    {
        File.Delete(WindowsPath);
        File.Delete(WindowsPath + ".tmp");
    }
}
