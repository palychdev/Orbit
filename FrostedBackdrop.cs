using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace Orbit;
// Capture only the small region behind the wheel, once per opening. The cached
// blur is static while selecting, and is released on close. No idle capture loop.
internal sealed class FrostedBackdrop : Border
{
 private readonly Image image=new(){Stretch=Stretch.Fill,IsHitTestVisible=false,Effect=new BlurEffect{Radius=18,KernelType=KernelType.Gaussian,RenderingBias=RenderingBias.Performance}};
 internal bool HasFrame=>image.Source!=null;
 [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr handle);
 internal FrostedBackdrop(){HorizontalAlignment=HorizontalAlignment.Center;VerticalAlignment=VerticalAlignment.Center;IsHitTestVisible=false;Child=image;}
 internal void Capture(Native.Rect monitor,Settings settings,double scale)
 {
  Release();
  if(SystemParameters.HighContrast)return;
  double diameter=Math.Min(settings.Size,Math.Min(monitor.Right-monitor.Left,monitor.Bottom-monitor.Top)/scale*.86);
  int pixels=(int)Math.Ceiling((diameter+64)*scale);
  int x=(monitor.Left+monitor.Right-pixels)/2,y=(monitor.Top+monitor.Bottom-pixels)/2;
  IntPtr bitmap=IntPtr.Zero;
  try {
   using var capture=new System.Drawing.Bitmap(pixels,pixels,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
   using(var graphics=System.Drawing.Graphics.FromImage(capture))graphics.CopyFromScreen(x,y,0,0,new System.Drawing.Size(pixels,pixels),System.Drawing.CopyPixelOperation.SourceCopy);
   bitmap=capture.GetHbitmap();var source=Imaging.CreateBitmapSourceFromHBitmap(bitmap,IntPtr.Zero,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions());source.Freeze();
   SetFrame(source,diameter,scale);
  }catch{Release();} // A protected desktop or exclusive renderer can refuse capture.
  finally{if(bitmap!=IntPtr.Zero)DeleteObject(bitmap);}
 }
 internal void SetFrame(BitmapSource source,double diameter,double scale) {
  Width=source.PixelWidth/scale;Height=source.PixelHeight/scale;Clip=new EllipseGeometry(new Point(Width/2,Height/2),diameter/2,diameter/2);
  image.Source=source;image.CacheMode=new BitmapCache{SnapsToDevicePixels=true};
 }
 internal void Release(){image.Source=null;image.CacheMode=null;Clip=null;}
}
