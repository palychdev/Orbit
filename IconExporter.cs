using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Orbit;
internal static class IconExporter
{
 internal static void Export(string destination)
 {
  int[] sizes={16,24,32,48,64,128,256};var images=new List<byte[]>();
  foreach(int size in sizes) {
   var visual=new DrawingVisual();using(var dc=visual.RenderOpen()) {
    dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(28,28,30)),null,new Rect(0,0,size,size),size*.2,size*.2);
    dc.PushTransform(new TranslateTransform(size*.16,size*.16));dc.PushTransform(new ScaleTransform(size*.68/256,size*.68/256));dc.DrawGeometry(Brushes.White,null,FilledIcon.GeometryFor(IconKind.Aperture));dc.Pop();dc.Pop();
   }
   var bitmap=new RenderTargetBitmap(size,size,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var memory=new MemoryStream();png.Save(memory);images.Add(memory.ToArray());
  }
  using var output=new BinaryWriter(File.Create(destination));output.Write((ushort)0);output.Write((ushort)1);output.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;
  for(int i=0;i<sizes.Length;i++){output.Write((byte)(sizes[i]==256?0:sizes[i]));output.Write((byte)(sizes[i]==256?0:sizes[i]));output.Write((byte)0);output.Write((byte)0);output.Write((ushort)1);output.Write((ushort)32);output.Write(images[i].Length);output.Write(offset);offset+=images[i].Length;}
  foreach(var image in images)output.Write(image);
 }
}
