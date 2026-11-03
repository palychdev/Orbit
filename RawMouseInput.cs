using System;
using System.Runtime.InteropServices;

namespace Orbit;
internal static class RawMouseInput
{
 internal const uint InputSink=0x100;
 internal const uint Remove=1;
 internal const int Message=0xFF;
 internal static Native.RawDevice Device(IntPtr window) => new() {UsagePage=1,Usage=2,Flags=InputSink,Target=window};

 // Bounds checks keep truncated, keyboard and absolute-device packets out of selection.
 internal static bool Decode(IntPtr data,uint size,out int dx,out int dy)
 {
  dx=dy=0;
  int headerSize=Marshal.SizeOf<Native.RawHeader>();
  if(data==IntPtr.Zero || size<headerSize+24)return false;
  var header=Marshal.PtrToStructure<Native.RawHeader>(data);
  if(header.Type!=0 || header.Size<headerSize+24 || header.Size>size)return false;
  var mouse=Marshal.PtrToStructure<Native.RawMouse>(data+headerSize);
  if((mouse.Flags&1)!=0)return false;
  dx=mouse.X;dy=mouse.Y;return dx!=0 || dy!=0;
 }
}
