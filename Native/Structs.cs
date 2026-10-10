using System;
using System.Runtime.InteropServices;

namespace StageDeck.Native;

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct SIZE
{
    public int CX;
    public int CY;
}

[StructLayout(LayoutKind.Sequential)]
public struct DWM_THUMBNAIL_PROPERTIES
{
    public uint dwFlags;
    public RECT rcDestination;
    public RECT rcSource;
    public byte opacity;
    public int fVisible;
    public int fSourceClientAreaOnly;
}

public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
    int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
