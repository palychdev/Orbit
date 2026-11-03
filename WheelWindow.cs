using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Orbit;
internal sealed class WheelWindow : Window
{
 private readonly Settings settings;
 private readonly WheelVisual visual;
 private readonly DispatcherTimer timeout=new() {Interval=TimeSpan.FromSeconds(20)};
 private Native.Point saved;
 private Native.Rect savedClip;
 private bool engaged,preview;
 private bool pointerOwned;
 private double x,y;
 private IntPtr handle;
 public event Action<Entry>? Chosen;
 public WheelWindow(Settings settings)
 {
  this.settings=settings;
  WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.NoResize; AllowsTransparency=true; Background=Brushes.Transparent; ShowInTaskbar=false; Topmost=true; Cursor=Cursors.None;
  Content=visual=new WheelVisual(settings);
  SourceInitialized+=(_,_)=>{
   handle=new WindowInteropHelper(this).Handle;
   Native.SetWindowLong(handle,-20,Native.GetWindowLong(handle,-20)|0x80);
   HwndSource.FromHwnd(handle).AddHook(Messages);
  };
  Deactivated+=(_,_)=>{if(engaged) Finish(true);};
  PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape) {Finish(true); e.Handled=true;}};
  timeout.Tick+=(_,_)=>Finish(true);
  Closed+=(_,_)=>RestorePointer();
 }
 public void Begin(bool isPreview=false)
 {
  if(engaged || settings.Entries.Count==0) return;
  Native.GetCursorPos(out saved); Native.GetClipCursor(out savedClip);
  var info=new Native.MonitorInfo {Size=Marshal.SizeOf<Native.MonitorInfo>()};
  Native.GetMonitorInfo(Native.MonitorFromPoint(saved,2),ref info);
  var r=info.Monitor;
  preview=isPreview; x=y=0; visual.Selected=-1; visual.Preview=preview;
  // SetWindowPos receives physical monitor coordinates, unlike WPF's device-independent Left/Top.
  Show(); Native.SetWindowPos(handle,new IntPtr(-1),r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top,0x40); Activate();
  visual.InvalidateVisual();
  var device=new Native.RawDevice {UsagePage=1,Usage=2,Flags=0,Target=handle};
  if(!Native.RegisterRawInputDevices(new[]{device},1,(uint)Marshal.SizeOf<Native.RawDevice>())) {Hide(); throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
  engaged=true;
  pointerOwned=true;
  int cx=(r.Left+r.Right)/2, cy=(r.Top+r.Bottom)/2;
  Native.SetCursorPos(cx,cy);
  var clip=new Native.Rect {Left=cx,Top=cy,Right=cx+1,Bottom=cy+1};
  Native.ClipCursor(ref clip);
  timeout.Interval=TimeSpan.FromSeconds(preview?5:20); timeout.Start();
 }
 private IntPtr Messages(IntPtr hwnd,int message,IntPtr w,IntPtr l,ref bool handled)
 {
  if(message==0xFF && engaged) {
   uint size=0, header=(uint)Marshal.SizeOf<Native.RawHeader>();
   Native.GetRawInputData(l,0x10000003,IntPtr.Zero,ref size,header);
   if(size<header+24 || size>4096) return IntPtr.Zero;
   IntPtr data=Marshal.AllocHGlobal((int)size);
   try {
    if(Native.GetRawInputData(l,0x10000003,data,ref size,header)!=size) return IntPtr.Zero;
    if(Marshal.PtrToStructure<Native.RawHeader>(data).Type!=0) return IntPtr.Zero;
    var mouse=Marshal.PtrToStructure<Native.RawMouse>(data+(int)header);
    if((mouse.Flags&1)==0) Move(mouse.X,mouse.Y);
   } finally {Marshal.FreeHGlobal(data);}
  }
  return IntPtr.Zero;
 }
 internal void Move(int dx,int dy)
 {
  x+=dx*settings.Sensitivity; y+=dy*settings.Sensitivity;
  // Bound the virtual offset: returning toward the center remains easy after a large motion.
  double distance=Math.Sqrt(x*x+y*y), limit=settings.Size*0.42;
  if(distance>limit) {x*=limit/distance; y*=limit/distance;}
  visual.Selected=Selection.Index(x,y,settings.Entries.Count);
  visual.Offset=new Point(x,y); visual.InvalidateVisual();
 }
 public void Finish(bool cancel)
 {
  if(!engaged) return;
  int selected=visual.Selected; bool wasPreview=preview;
  engaged=false; timeout.Stop();
  RestorePointer(); Hide();
  if(!cancel && !wasPreview && selected>=0 && selected<settings.Entries.Count) Chosen?.Invoke(settings.Entries[selected]);
 }
 private void RestorePointer()
 {
  if(!pointerOwned) return;
  pointerOwned=false;
  var remove=new Native.RawDevice {UsagePage=1,Usage=2,Flags=1,Target=IntPtr.Zero};
  Native.RegisterRawInputDevices(new[]{remove},1,(uint)Marshal.SizeOf<Native.RawDevice>());
  Native.UnclipCursor(IntPtr.Zero); Native.SetCursorPos(saved.X,saved.Y); Native.ClipCursor(ref savedClip);
 }
}
internal sealed class WheelVisual : FrameworkElement
{
 private readonly Settings settings;
 public int Selected {get;set;}=-1;
 public bool Preview {get;set;}
 public Point Offset {get;set;}
 public WheelVisual(Settings settings) {this.settings=settings;}
 private static Brush Brush(string value) => (Brush)new BrushConverter().ConvertFromString(value)!;
 private void Text(DrawingContext dc,string text,Point center,double size,Brush brush,double maxWidth=260)
 {
  var ft=new FormattedText(text,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),size,brush,VisualTreeHelper.GetDpi(this).PixelsPerDip) {MaxTextWidth=maxWidth,TextAlignment=TextAlignment.Center,Trimming=TextTrimming.CharacterEllipsis,MaxLineCount=2};
  dc.DrawText(ft,new Point(center.X-maxWidth/2,center.Y));
 }
 protected override void OnRender(DrawingContext dc)
 {
  var center=new Point(ActualWidth/2,ActualHeight/2);
  double radius=Math.Min(settings.Size/2,Math.Min(ActualWidth,ActualHeight)*0.43), inner=radius*0.45;
  dc.DrawRectangle(Brush("#42060A13"),null,new Rect(0,0,ActualWidth,ActualHeight));
  dc.PushOpacity(settings.Opacity);
  int n=settings.Entries.Count;
  for(int i=0;i<n;i++) {
   double angle=i*360.0/n-90, half=180.0/n;
   var geometry=Sector(center,inner,radius,angle-half+1.1,angle+half-1.1);
   bool selected=i==Selected;
   dc.DrawGeometry(selected?Brush("#35B7A0"):Brush("#E51A2434"),new Pen(selected?Brush("#9FF5DE"):Brush("#3C5068"),selected?2:1),geometry);
   double rad=angle*Math.PI/180; var p=new Point(center.X+Math.Cos(rad)*radius*0.72,center.Y+Math.Sin(rad)*radius*0.72);
   var entry=settings.Entries[i];
   if(entry.Icon!=null) dc.DrawImage(entry.Icon,new Rect(p.X-20,p.Y-28,40,40));
   else Text(dc,entry.Name.Length>0?entry.Name[..1]:"?",new Point(p.X,p.Y-32),30,Brushes.White,80);
   Text(dc,entry.Name,new Point(p.X,p.Y+18),12,Brushes.White,Math.Max(65,Math.Min(140,2*Math.PI*radius*.72/n-8)));
  }
  dc.DrawEllipse(Brush("#F0101826"),new Pen(Brush("#40536A"),1),center,inner-5,inner-5);
  Text(dc,Selected<0?"ORBIT":settings.Entries[Selected].Name,new Point(center.X,center.Y-22),Selected<0?20:16,Brushes.White,inner*1.75);
  Text(dc,Selected<0?"Двинь мышь": "Отпусти бинд",new Point(center.X,center.Y+12),11,Brush("#A6B8CD"),inner*1.8);
  if(Selected>=0) {
   double rad=(Selected*360.0/n-90)*Math.PI/180;
   dc.DrawEllipse(Brush("#8BF5D9"),null,new Point(center.X+Math.Cos(rad)*(inner-17),center.Y+Math.Sin(rad)*(inner-17)),4,4);
  }
  dc.Pop();
  Text(dc,Preview?"ПРЕДПРОСМОТР · 5 СЕКУНД · ESC — ЗАКРЫТЬ":settings.BindName+"  ·  ESC — ОТМЕНА",new Point(center.X,center.Y+radius+24),12,Brush("#D2DEEF"),500);
 }
 internal static StreamGeometry Sector(Point c,double inner,double outer,double start,double end)
 {
  Point P(double r,double angle)=>new(c.X+r*Math.Cos(angle*Math.PI/180),c.Y+r*Math.Sin(angle*Math.PI/180));
  var g=new StreamGeometry(); using(var ctx=g.Open()) {
   ctx.BeginFigure(P(outer,start),true,true);
   ctx.ArcTo(P(outer,end),new Size(outer,outer),0,end-start>180,SweepDirection.Clockwise,true,false);
   ctx.LineTo(P(inner,end),true,false);
   ctx.ArcTo(P(inner,start),new Size(inner,inner),0,end-start>180,SweepDirection.Counterclockwise,true,false);
  } g.Freeze(); return g;
 }
}
