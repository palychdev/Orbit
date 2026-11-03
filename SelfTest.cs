using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Orbit;
internal static class SelfTest
{
 public static void Run()
 {
  int checks=0;
  void Check(bool result,string name) {checks++;if(!result)throw new Exception("FAIL: "+name);}
  Check(Selection.Index(0,0,4)==-1,"center cancels");Check(Selection.Index(23,0,4)==-1,"dead zone");
  Check(Selection.Index(0,-100,4)==0,"up");Check(Selection.Index(100,0,4)==1,"right");Check(Selection.Index(0,100,4)==2,"down");Check(Selection.Index(-100,0,4)==3,"left");
  for(int n=1;n<=12;n++)for(int i=0;i<n;i++) {double a=(i*360.0/n-90)*Math.PI/180;Check(Selection.Index(100*Math.Cos(a),100*Math.Sin(a),n)==i,$"{n} sectors, index {i}");}
  Check(Selection.Index(100,0,0)==-1,"empty");
  var config=Settings.Defaults();var roundtrip=JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(config))!;
  Check(roundtrip.Entries.Count==3 && roundtrip.Key==0x77,"settings roundtrip");
  foreach(var entry in config.Entries)entry.LoadIcon();
  var visual=new WheelVisual(config) {Selected=1};visual.Measure(new Size(900,700));visual.Arrange(new Rect(0,0,900,700));visual.UpdateLayout();
  var bitmap=new RenderTargetBitmap(900,700,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);
  var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var output=File.Create(System.IO.Path.Combine(AppContext.BaseDirectory,"wheel-preview.png")))png.Save(output);
  checks+=VisualTests.Run();
  checks+=RegressionTests.Run();
  File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"self-test.txt"),$"PASS: {checks} checks. Sector directions, raw packet decoding, background-input configuration, hotkey sequences, repeated open/close, failure cleanup and recovery, settings roundtrip, WPF render.\nReal Start menu, F11 and game scenarios require interactive verification.");
 }
}
