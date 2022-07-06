﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.WindowsAPICodePack.PropertySystem;
using Microsoft.WindowsAPICodePack.Win32Native.PropertySystem;

// ReSharper disable BuiltInTypeReferenceStyle
// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
#pragma warning disable CA1712 // Do not prefix enum values with type name

namespace ToastNotificationsPlugin;

[SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "<Pending>")]
internal enum STGM : long
{
    STGM_READ = 0x00000000L,
    STGM_WRITE = 0x00000001L,
    STGM_READWRITE = 0x00000002L,
    STGM_SHARE_DENY_NONE = 0x00000040L,
    STGM_SHARE_DENY_READ = 0x00000030L,
    STGM_SHARE_DENY_WRITE = 0x00000020L,
    STGM_SHARE_EXCLUSIVE = 0x00000010L,
    STGM_PRIORITY = 0x00040000L,
    STGM_CREATE = 0x00001000L,
    STGM_CONVERT = 0x00020000L,
    STGM_TRANSACTED = 0x00010000L,
    STGM_NOSCRATCH = 0x00100000L,
    STGM_NOSNAPSHOT = 0x00200000L,
    STGM_SIMPLE = 0x08000000L,
    STGM_DIRECT_SWMR = 0x00400000L,
    STGM_DELETEONRELEASE = 0x04000000L
}

internal static class ShellIidGuid
{
    internal const string CShellLink = "00021401-0000-0000-C000-000000000046";
    internal const string IPersistFile = "0000010b-0000-0000-C000-000000000046";
    internal const string IPropertyStore = "886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99";
    internal const string IShellLinkW = "000214F9-0000-0000-C000-000000000046";
}

[ComImport]
[Guid(ShellIidGuid.IShellLinkW)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellLinkW
{
    UInt32 GetPath(
        [Out] [MarshalAs(UnmanagedType.LPWStr)]
        StringBuilder pszFile,
        int cchMaxPath,
        IntPtr pfd,
        uint fFlags);

    UInt32 GetIDList(out IntPtr ppidl);
    UInt32 SetIDList(IntPtr pidl);

    UInt32 GetDescription(
        [Out] [MarshalAs(UnmanagedType.LPWStr)]
        StringBuilder pszFile,
        int cchMaxName);

    UInt32 SetDescription(
        [MarshalAs(UnmanagedType.LPWStr)] string pszName);

    UInt32 GetWorkingDirectory(
        [Out] [MarshalAs(UnmanagedType.LPWStr)]
        StringBuilder pszDir,
        int cchMaxPath
    );

    UInt32 SetWorkingDirectory(
        [MarshalAs(UnmanagedType.LPWStr)] string pszDir);

    UInt32 GetArguments(
        [Out] [MarshalAs(UnmanagedType.LPWStr)]
        StringBuilder pszArgs,
        int cchMaxPath);

    UInt32 SetArguments(
        [MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

    UInt32 GetHotKey(out short wHotKey);
    UInt32 SetHotKey(short wHotKey);
    UInt32 GetShowCmd(out uint iShowCmd);
    UInt32 SetShowCmd(uint iShowCmd);

    UInt32 GetIconLocation(
        [Out] [MarshalAs(UnmanagedType.LPWStr)]
        out StringBuilder pszIconPath,
        int cchIconPath,
        out int iIcon);

    UInt32 SetIconLocation(
        [MarshalAs(UnmanagedType.LPWStr)] string pszIconPath,
        int iIcon);

    UInt32 SetRelativePath(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPathRel,
        uint dwReserved);

    UInt32 Resolve(IntPtr hwnd, uint fFlags);

    UInt32 SetPath(
        [MarshalAs(UnmanagedType.LPWStr)] string? pszFile);
}

[ComImport]
[Guid(ShellIidGuid.IPersistFile)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPersistFile
{
    UInt32 GetCurFile(
        [Out] [MarshalAs(UnmanagedType.LPWStr)]
        StringBuilder pszFile
    );

    UInt32 IsDirty();

    UInt32 Load(
        [MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
        [MarshalAs(UnmanagedType.U4)] STGM dwMode);

    UInt32 Save(
        [MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
        bool fRemember);

    UInt32 SaveCompleted(
        [MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
}

[ComImport]
[Guid(ShellIidGuid.IPropertyStore)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPropertyStore
{
    UInt32 GetCount([Out] out uint propertyCount);
    UInt32 GetAt([In] uint propertyIndex, out PropertyKey key);
    UInt32 GetValue([In] ref PropertyKey key, [Out] PropVariant pv);
    UInt32 SetValue([In] ref PropertyKey key, [In] PropVariant pv);
    UInt32 Commit();
}

[ComImport]
[Guid(ShellIidGuid.CShellLink)]
[ClassInterface(ClassInterfaceType.None)]
internal class CShellLink
{
}

[ExcludeFromCodeCoverage]
public static class ErrorHelper
{
    public static void VerifySucceeded(uint hresult)
    {
        if (hresult > 1)
            throw new ExternalException("Failed with HRESULT: " + hresult.ToString("X"), unchecked((int)hresult));
    }
}