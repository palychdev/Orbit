using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Orbit;
internal sealed class ThemePalette
{
 internal bool Light {get;init;}
 internal Brush Canvas {get;init;}=null!;
 internal Brush Surface {get;init;}=null!;
 internal Brush Elevated {get;init;}=null!;
 internal Brush Hover {get;init;}=null!;
 internal Brush Text {get;init;}=null!;
 internal Brush Secondary {get;init;}=null!;
 internal Brush Stroke {get;init;}=null!;
 internal Brush Accent {get;init;}=null!;
 internal Brush Danger {get;init;}=null!;
 internal Brush Wheel {get;init;}=null!;
 internal Brush WheelSelected {get;init;}=null!;
 internal Brush WheelSelectedText {get;init;}=null!;
 internal Brush Backdrop {get;init;}=null!;
 private static Brush B(string hex) {var b=(SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;b.Freeze();return b;}
 internal static ThemePalette For(string theme)=>theme=="Light"?new() {
  Light=true,Canvas=B("#F5F5F7"),Surface=B("#FFFFFF"),Elevated=B("#E8E8ED"),Hover=B("#EDEDF1"),
  Text=B("#1D1D1F"),Secondary=B("#66666B"),Stroke=B("#DCDCE0"),Accent=B("#776237"),Danger=B("#BC2835"),
  Wheel=B("#F2F2F5"),WheelSelected=B("#242426"),WheelSelectedText=B("#FFFFFF"),Backdrop=B("#24000000")
 }:new() {
  Canvas=B("#161617"),Surface=B("#222224"),Elevated=B("#343437"),Hover=B("#2C2C2F"),
  Text=B("#F5F5F7"),Secondary=B("#A1A1A6"),Stroke=B("#38383A"),Accent=B("#B6A579"),Danger=B("#FF8A91"),
  Wheel=B("#29292C"),WheelSelected=B("#F5F5F7"),WheelSelectedText=B("#1D1D1F"),Backdrop=B("#44000000")
 };
}
internal static class ThemeManager
{
 internal static void Apply(Application app,string theme,bool animate=false)
 {
  var p=ThemePalette.For(theme);
  foreach(var pair in new (string,Brush)[]{("CanvasBrush",p.Canvas),("SurfaceBrush",p.Surface),("ElevatedBrush",p.Elevated),("HoverBrush",p.Hover),("TextBrush",p.Text),("SecondaryBrush",p.Secondary),("StrokeBrush",p.Stroke),("AccentBrush",p.Accent),("DangerBrush",p.Danger)}) {
   var target=((SolidColorBrush)pair.Item2).Color;
   if(animate&&SystemParameters.ClientAreaAnimation&&app.Resources[pair.Item1] is SolidColorBrush previous) {
    Color from=previous.Color;var brush=new SolidColorBrush(target);
    var animation=new ColorAnimation(from,target,TimeSpan.FromMilliseconds(170)){FillBehavior=FillBehavior.Stop,EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};
    animation.Completed+=(_,_)=>{if(!brush.IsFrozen){brush.BeginAnimation(SolidColorBrush.ColorProperty,null);brush.Freeze();}};brush.BeginAnimation(SolidColorBrush.ColorProperty,animation);app.Resources[pair.Item1]=brush;
   } else app.Resources[pair.Item1]=pair.Item2;
  }
  foreach(Window w in app.Windows)ApplyTitleBar(w,theme);
 }
 internal static void ApplyTitleBar(Window w,string theme)
 {
  var h=new WindowInteropHelper(w).Handle;if(h==IntPtr.Zero)return;
  int dark=theme=="Light"?0:1;Native.DwmSetWindowAttribute(h,20,ref dark,4);
  int rounded=2;Native.DwmSetWindowAttribute(h,33,ref rounded,4);
  var p=ThemePalette.For(theme);var c=((SolidColorBrush)p.Canvas).Color;
  int caption=c.R|(c.G<<8)|(c.B<<16);Native.DwmSetWindowAttribute(h,35,ref caption,4);
  var t=((SolidColorBrush)p.Text).Color;int text=t.R|(t.G<<8)|(t.B<<16);Native.DwmSetWindowAttribute(h,36,ref text,4);
 }
}
