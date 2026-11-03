using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Orbit;
internal static class Native
{
 [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X,Y; }
 [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left,Top,Right,Bottom; }
 [StructLayout(LayoutKind.Sequential)] internal struct MonitorInfo { public int Size; public Rect Monitor,Work; public uint Flags; }
 [StructLayout(LayoutKind.Sequential)] internal struct RawDevice { public ushort UsagePage,Usage; public uint Flags; public IntPtr Target; }
 [StructLayout(LayoutKind.Sequential)] internal struct RawHeader { public uint Type,Size; public IntPtr Device,WParam; }
 [StructLayout(LayoutKind.Explicit,Size=24)] internal struct RawMouse { [FieldOffset(0)] public ushort Flags; [FieldOffset(4)] public uint Buttons; [FieldOffset(12)] public int X; [FieldOffset(16)] public int Y; }
 internal delegate IntPtr HookProc(int code,IntPtr w,IntPtr l);
 internal delegate bool EnumProc(IntPtr window,IntPtr l);
 [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point p);
 [DllImport("user32.dll")] internal static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll", EntryPoint="ClipCursor")] internal static extern bool ClipCursor(ref Rect rect);
 [DllImport("user32.dll", EntryPoint="ClipCursor")] internal static extern bool UnclipCursor(IntPtr rect);
 [DllImport("user32.dll")] internal static extern bool GetClipCursor(out Rect rect);
 [DllImport("user32.dll")] internal static extern IntPtr MonitorFromPoint(Point p,uint flags);
 [DllImport("user32.dll", CharSet=CharSet.Auto)] internal static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
 [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int height,uint flags);
 [DllImport("user32.dll",SetLastError=true)] internal static extern bool RegisterRawInputDevices(RawDevice[] devices,uint count,uint size);
 [DllImport("user32.dll",SetLastError=true)] internal static extern uint GetRegisteredRawInputDevices([Out] RawDevice[]? devices,ref uint count,uint size);
 [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr window,out Rect rect);
 [DllImport("user32.dll")] internal static extern uint GetRawInputData(IntPtr input,uint command,IntPtr data,ref uint size,uint headerSize);
 [DllImport("user32.dll",SetLastError=true)] internal static extern IntPtr SetWindowsHookEx(int id,HookProc proc,IntPtr module,uint thread);
 [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
 [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr w,IntPtr l);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string? name);
 [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumProc proc,IntPtr l);
 [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] internal static extern IntPtr GetWindow(IntPtr h,uint command);
 [DllImport("user32.dll")] internal static extern int GetWindowLong(IntPtr h,int index);
 [DllImport("user32.dll")] internal static extern int SetWindowLong(IntPtr h,int index,int value);
 [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int GetWindowText(IntPtr h,StringBuilder text,int count);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int GetClassName(IntPtr h,StringBuilder text,int count);
 [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr h);
 [DllImport("user32.dll")] internal static extern bool ShowWindowAsync(IntPtr h,int command);
 [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("dwmapi.dll")] internal static extern int DwmGetWindowAttribute(IntPtr h,int attribute,out int value,int size);
 [DllImport("dwmapi.dll")] internal static extern int DwmSetWindowAttribute(IntPtr h,int attribute,ref int value,int size);
}
