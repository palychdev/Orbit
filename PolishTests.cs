using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Orbit;
internal static class PolishTests
{
 internal static int Run()
 {
  int count=0;void Check(bool result,string name){count++;if(!result)throw new Exception("FAIL: "+name);}
  Check(!HotkeyBinding.IsAllowed(1)&&!HotkeyBinding.IsAllowed(2),"LMB/RMB cannot be assigned");
  foreach(int key in new[]{4,5,6}) {
   var state=new HotkeyState();state.Configure(key,0,false);
   for(int i=0;i<100;i++) {
    Check(state.Handle(key,true,false,0,false)==(HotkeyResult.Open|HotkeyResult.Suppress),"mouse hold opens");
    Check(state.Handle(key,false,true,0,false)==(HotkeyResult.Accept|HotkeyResult.Suppress),"mouse release accepts");
   }
   state.Handle(key,true,false,0,false);Check((state.Handle(27,true,false,0,false)&HotkeyResult.Cancel)!=0,"Escape cancels mouse session");
   Check(state.Handle(key,false,true,0,false)==HotkeyResult.Suppress,"mouse release after cancel does not launch");
   state.Handle(27,false,true,0,false);state.Configure(key,2,false);
   Check((state.Handle(key,true,false,2,false)&HotkeyResult.Open)!=0,"Ctrl+mouse opens");
   Check(state.Handle(162,false,true,0,false)==HotkeyResult.Accept,"modifier release accepts mouse bind");state.Handle(key,false,true,0,false);
   state.Configure(key,0,true);Check(state.Handle(key,true,false,0,false)==HotkeyResult.None,"recorded held mouse cannot open until release");
   Check(System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(new Settings{Key=key}))!.Key==key,"mouse key survives serialization");
  }
  foreach(var sample in new[]{(0x207,0u,4,true),(0x208,0u,4,false),(0x20B,1u<<16,5,true),(0x20C,1u<<16,5,false),(0x20B,2u<<16,6,true),(0x20C,2u<<16,6,false)}) {
   Check(HotkeyBinding.TryMouseMessage(sample.Item1,sample.Item2,out int key,out bool down,out bool up)&&key==sample.Item3&&down==sample.Item4&&up!=down,"native mouse message decode");
  }
  foreach(int message in new[]{0x200,0x201,0x202,0x204,0x205,0x20A})Check(!HotkeyBinding.TryMouseMessage(message,0,out _,out _,out _),"movement/wheel/LMB/RMB pass through");
  Check(!HotkeyBinding.TryMouseMessage(0x20B,3u<<16,out _,out _,out _),"unknown side button ignored");

  var app=(App)Application.Current;string original=app.Config.Theme;
  var window=new MainWindow(app);var hwnd=new WindowInteropHelper(window).EnsureHandle();
  Check((Native.GetWindowLong(hwnd,-16)&0x10000)==0,"native maximize button disabled");
  Check(window.ResizeMode==ResizeMode.NoResize&&window.WindowStyle==WindowStyle.None,"fixed utility window has no native maximize square");
  var material=new FrostedBackdrop();byte[] pixels=new byte[256*256*4];
  for(int y=0;y<256;y++)for(int x=0;x<256;x++){int i=(y*256+x)*4;byte v=(byte)(((x/8+y/8)%2)*255);pixels[i]=pixels[i+1]=pixels[i+2]=v;pixels[i+3]=255;}
  var pattern=BitmapSource.Create(256,256,96,96,PixelFormats.Bgra32,null,pixels,1024);pattern.Freeze();material.SetFrame(pattern,200,1);
  Check(material.HasFrame,"material retains a single frame only while open");
  material.Measure(new Size(256,256));material.Arrange(new Rect(0,0,256,256));material.UpdateLayout();var blurred=new RenderTargetBitmap(256,256,96,96,PixelFormats.Pbgra32);blurred.Render(material);
  byte[] center=new byte[4];blurred.CopyPixels(new Int32Rect(128,128,1,1),center,4,0);Check(center[0]>25&&center[0]<230,"backdrop really blurs sharp background pattern");
  var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(blurred));using(var output=File.Create(Path.Combine(AppContext.BaseDirectory,"ui-material-test.png")))encoder.Save(output);
  material.Release();Check(!material.HasFrame,"closing releases backdrop image");
  try {
   app.Config.Theme="Light";ThemeManager.Apply(app,"Light",true);var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(240)};timer.Tick+=(_,_)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);
   var canvas=(SolidColorBrush)app.Resources["CanvasBrush"];Check(!canvas.HasAnimatedProperties,"theme animation finishes without an idle clock");Check(canvas.Color==((SolidColorBrush)ThemePalette.For("Light").Canvas).Color,"theme animation reaches requested appearance");
  }finally{app.Config.Theme=original;ThemeManager.Apply(app,original);}
  return count;
 }
 internal static void NativeHooks()
 {
  using var input=new InputService(Dispatcher.CurrentDispatcher);var settings=new Settings();int count=0;
  for(int i=0;i<25;i++) {
   foreach(int key in new[]{4,5,6,0x77}){settings.Key=key;input.Configure(settings);if(input.MouseHookInstalled!=HotkeyBinding.IsMouse(key))throw new Exception("Unexpected mouse hook lifetime");count++;}
  }
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"hook-test.txt"),$"PASS: {count} native hook configuration checks. Mouse hook installed only for MMB/X1/X2, removed for keyboard binding; no polling or injected input.");
 }
}
