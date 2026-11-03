using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Orbit;
internal static class ShellIcons
{
 [StructLayout(LayoutKind.Sequential)] private struct IconSize {public int Width,Height;}
 [ComImport,Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]private interface IImageFactory{[PreserveSig]int GetImage(IconSize size,int flags,out IntPtr bitmap);}
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)]private static extern int SHCreateItemInKnownFolder(ref Guid folder,uint flags,string name,ref Guid iid,out IImageFactory factory);
 [DllImport("gdi32.dll")]private static extern bool DeleteObject(IntPtr bitmap);
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] private struct FileInfo {public IntPtr Icon;public int Index;public uint Attributes;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string DisplayName;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=80)]public string TypeName;}
 [ComImport,Guid("46EB5926-582E-4017-9FDF-E8998DAA0950"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] private interface IImageList {
  [PreserveSig]int Add(IntPtr bitmap,IntPtr mask,out int index);
  [PreserveSig]int ReplaceIcon(int index,IntPtr icon,out int replaced);
  [PreserveSig]int SetOverlayImage(int image,int overlay);
  [PreserveSig]int Replace(int index,IntPtr bitmap,IntPtr mask);
  [PreserveSig]int AddMasked(IntPtr bitmap,int mask,out int index);
  [PreserveSig]int Draw(IntPtr parameters);
  [PreserveSig]int Remove(int index);
  [PreserveSig]int GetIcon(int index,int flags,out IntPtr icon);
 }
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)] private static extern IntPtr SHGetFileInfo(string path,uint attributes,out FileInfo info,uint size,uint flags);
 [DllImport("shell32.dll")] private static extern int SHGetImageList(int size,ref Guid iid,out IImageList list);
 [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);
 internal static ImageSource? Load(string path)
 {
  if(!File.Exists(path))return null;
  string windows=Environment.GetFolderPath(Environment.SpecialFolder.Windows);
  string? appId=Path.GetFullPath(path).Equals(Path.Combine(windows,"System32","notepad.exe"),StringComparison.OrdinalIgnoreCase)?"Microsoft.WindowsNotepad_8wekyb3d8bbwe!App":Path.GetFullPath(path).Equals(Path.Combine(windows,"System32","calc.exe"),StringComparison.OrdinalIgnoreCase)?"Microsoft.WindowsCalculator_8wekyb3d8bbwe!App":null;
  if(appId!=null){var packaged=PackageIcon(appId);if(packaged!=null)return packaged;}
  IImageList? list=null;IntPtr icon=IntPtr.Zero;
  try {
   if(SHGetFileInfo(path,0,out var info,(uint)Marshal.SizeOf<FileInfo>(),0x4000)==IntPtr.Zero)return null;
   var iid=typeof(IImageList).GUID;if(SHGetImageList(4,ref iid,out list)!=0||list.GetIcon(info.Index,1,out icon)!=0||icon==IntPtr.Zero)return null;
   var image=Imaging.CreateBitmapSourceFromHIcon(icon,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions());image.Freeze();return image;
  }catch{return null;}
  finally{if(icon!=IntPtr.Zero)DestroyIcon(icon);if(list!=null)Marshal.FinalReleaseComObject(list);}
 }
 private static ImageSource? PackageIcon(string id)
 {
  IImageFactory? factory=null;IntPtr bitmap=IntPtr.Zero;
  try {
   var folder=new Guid("1e87508d-89c2-42f0-8a7e-645a0f50ca58");var iid=typeof(IImageFactory).GUID;if(SHCreateItemInKnownFolder(ref folder,0,id,ref iid,out factory)!=0)return null;
   if(factory.GetImage(new IconSize{Width=96,Height=96},5,out bitmap)!=0||bitmap==IntPtr.Zero)return null;
   var image=Imaging.CreateBitmapSourceFromHBitmap(bitmap,IntPtr.Zero,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions());image.Freeze();return image;
  }catch{return null;}
  finally{if(bitmap!=IntPtr.Zero)DeleteObject(bitmap);if(factory!=null)Marshal.FinalReleaseComObject(factory);}
 }
}
