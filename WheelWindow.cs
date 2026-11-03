using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Controls;

namespace Orbit;
internal sealed class WheelWindow : Window,IWheelHost
{
 private readonly Settings settings;
 private readonly WheelVisual visual;
 private readonly FrostedBackdrop backdrop=new();
 private readonly DispatcherTimer timeout=new() {Interval=TimeSpan.FromSeconds(20)};
 private Native.Point saved;
 private Native.Rect savedClip;
 private readonly WheelSession session;
 private bool pointerOwned,inputOwned;
 private IntPtr handle;
 private readonly bool attemptActivation;
 public event Action<Entry>? Chosen;
 public WheelWindow(Settings settings,bool attemptActivation=true)
 {
  this.settings=settings;
  this.attemptActivation=attemptActivation;
  session=new WheelSession(settings,this);
  WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.NoResize; AllowsTransparency=true; Background=Brushes.Transparent; ShowInTaskbar=false; Topmost=true; Cursor=Cursors.None;ShowActivated=false;
  visual=new WheelVisual(settings);var layers=new Grid();layers.Children.Add(backdrop);layers.Children.Add(visual);Content=layers;
  TextOptions.SetTextRenderingMode(this,TextRenderingMode.Grayscale);TextOptions.SetTextFormattingMode(this,TextFormattingMode.Ideal);
  SourceInitialized+=(_,_)=>{
   handle=new WindowInteropHelper(this).Handle;
   Native.SetWindowLong(handle,-20,Native.GetWindowLong(handle,-20)|0x80);
   HwndSource.FromHwnd(handle).AddHook(Messages);
  };
  // Activation is best-effort, never a prerequisite for raw mouse input. In particular,
  // do not cancel on Deactivated: Start and fullscreen windows may keep foreground focus.
  PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape) {Finish(true); e.Handled=true;}};
  timeout.Tick+=(_,_)=>Finish(true);
  Closed+=(_,_)=>Finish(true);
 }
 public void Begin(bool isPreview=false)
 {
  if(session.Active)return;
  visual.Selected=-1;visual.Preview=isPreview;
  timeout.Interval=TimeSpan.FromSeconds(isPreview?5:20);
  if(session.Begin(isPreview))timeout.Start();
 }
 void IWheelHost.RegisterInput()
 {
  new WindowInteropHelper(this).EnsureHandle();
  var device=RawMouseInput.Device(handle);
  if(!Native.RegisterRawInputDevices(new[]{device},1,(uint)Marshal.SizeOf<Native.RawDevice>()))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
  inputOwned=true;
 }
 void IWheelHost.ShowOverlay()
 {
  if(!Native.GetCursorPos(out saved) || !Native.GetClipCursor(out savedClip))throw new InvalidOperationException("Не удалось сохранить положение курсора.");
  var info=new Native.MonitorInfo {Size=Marshal.SizeOf<Native.MonitorInfo>()};
  if(!Native.GetMonitorInfo(Native.MonitorFromPoint(saved,2),ref info))throw new InvalidOperationException("Не удалось определить экран.");
  var r=info.Monitor;
  Native.SetWindowPos(handle,new IntPtr(-1),r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top,0x10);
  double scale=Math.Max(96,Native.GetDpiForWindow(handle))/96.0;backdrop.Capture(r,settings,scale);
  // SetWindowPos receives physical monitor coordinates, unlike WPF's device-independent Left/Top.
  Show();
  if(!Native.SetWindowPos(handle,new IntPtr(-1),r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top,0x50))throw new InvalidOperationException("Не удалось показать колесо.");
  if(attemptActivation)Activate(); // Windows may refuse; INPUTSINK still delivers movement.
  visual.InvalidateVisual();
 }
 void IWheelHost.CapturePointer()
 {
  // Re-read after activation: a fullscreen app may have released its own mouse clip.
  if(!Native.GetClipCursor(out savedClip))throw new InvalidOperationException("Не удалось сохранить ограничения курсора.");
  var info=new Native.MonitorInfo {Size=Marshal.SizeOf<Native.MonitorInfo>()};
  if(!Native.GetMonitorInfo(Native.MonitorFromPoint(saved,2),ref info))throw new InvalidOperationException("Не удалось определить экран.");
  var r=info.Monitor;
  pointerOwned=true;
  int cx=(r.Left+r.Right)/2, cy=(r.Top+r.Bottom)/2;
  if(!Native.UnclipCursor(IntPtr.Zero) || !Native.SetCursorPos(cx,cy))throw new InvalidOperationException("Не удалось установить курсор в центре экрана.");
  var clip=new Native.Rect {Left=cx,Top=cy,Right=cx+1,Bottom=cy+1};
  if(!Native.ClipCursor(ref clip))throw new InvalidOperationException("Не удалось ограничить курсор на время выбора.");
 }
 private IntPtr Messages(IntPtr hwnd,int message,IntPtr w,IntPtr l,ref bool handled)
 {
  if(message==RawMouseInput.Message && session.Active) {
   uint size=0, header=(uint)Marshal.SizeOf<Native.RawHeader>();
   Native.GetRawInputData(l,0x10000003,IntPtr.Zero,ref size,header);
   if(size<header+24 || size>4096) return IntPtr.Zero;
   IntPtr data=Marshal.AllocHGlobal((int)size);
   try {
    if(Native.GetRawInputData(l,0x10000003,data,ref size,header)!=size) return IntPtr.Zero;
    ProcessRawPacket(data,size);
   } finally {Marshal.FreeHGlobal(data);}
  }
  return IntPtr.Zero;
 }
 internal void ProcessRawPacket(IntPtr data,uint size)
 {
  if(RawMouseInput.Decode(data,size,out int dx,out int dy))Move(dx,dy);
 }
 internal int Selected=>session.Selected;
 internal void Move(int dx,int dy)
 {
  session.Move(dx,dy);visual.Selected=session.Selected;visual.InvalidateVisual();
 }
 public void Finish(bool cancel)
 {
  timeout.Stop();var entry=session.Finish(cancel);visual.Selected=-1;
  if(entry!=null)Chosen?.Invoke(entry);
 }
 void IWheelHost.ReleasePointer()
 {
  if(!pointerOwned) return;
  pointerOwned=false;
  bool released=Native.UnclipCursor(IntPtr.Zero);
  bool restored=Native.SetCursorPos(saved.X,saved.Y);
  bool clipped=Native.ClipCursor(ref savedClip);
  if(!released || !restored || !clipped)throw new InvalidOperationException("Не удалось восстановить курсор.");
 }
 void IWheelHost.UnregisterInput()
 {
  if(!inputOwned)return;
  var remove=new Native.RawDevice {UsagePage=1,Usage=2,Flags=RawMouseInput.Remove,Target=IntPtr.Zero};
  if(!Native.RegisterRawInputDevices(new[]{remove},1,(uint)Marshal.SizeOf<Native.RawDevice>()))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
  inputOwned=false;
 }
 void IWheelHost.HideOverlay() {try{if(IsVisible)Hide();}finally{backdrop.Release();}}
}
internal sealed class WheelVisual : FrameworkElement
{
 private readonly Settings settings;
 public int Selected {get;set;}=-1;
 public bool Preview {get;set;}
 private ThemePalette? palette;
 private string? paletteTheme;
 private Brush? translucentSurface,translucentWheel;
 public WheelVisual(Settings settings) {this.settings=settings;}
 private void Text(DrawingContext dc,string text,Point center,double size,Brush brush,double maxWidth=260)
 {
  var ft=new FormattedText(text,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),size,brush,VisualTreeHelper.GetDpi(this).PixelsPerDip) {MaxTextWidth=maxWidth,TextAlignment=TextAlignment.Center,Trimming=TextTrimming.CharacterEllipsis,MaxLineCount=2};
  dc.DrawText(ft,new Point(center.X-maxWidth/2,center.Y));
 }
 protected override void OnRender(DrawingContext dc)
 {
  if(palette==null||paletteTheme!=settings.Theme){palette=ThemePalette.For(settings.Theme);paletteTheme=settings.Theme;translucentSurface=palette.Surface.Clone();translucentSurface.Opacity=.94;translucentSurface.Freeze();translucentWheel=palette.Wheel.Clone();translucentWheel.Opacity=.89;translucentWheel.Freeze();}
  var p=palette;
  var center=new Point(ActualWidth/2,ActualHeight/2);
  double radius=Math.Min(settings.Size/2,Math.Min(ActualWidth,ActualHeight)*0.43), inner=radius*0.48;
  dc.DrawRectangle(p.Backdrop,null,new Rect(0,0,ActualWidth,ActualHeight));
  int n=settings.Entries.Count;
  for(int i=0;i<n;i++) {
   double angle=i*360.0/n-90, half=180.0/n;
   var geometry=Sector(center,inner,radius,angle-half+0.8,angle+half-0.8);
   bool selected=i==Selected;
   dc.DrawGeometry(selected?p.WheelSelected:translucentWheel,new Pen(selected?p.Text:p.Stroke,selected?1.5:0.8),geometry);
   double rad=angle*Math.PI/180; var position=new Point(center.X+Math.Cos(rad)*radius*0.74,center.Y+Math.Sin(rad)*radius*0.74);
   var entry=settings.Entries[i];
   double iconSize=n>=9?28:34;double iconTop=n>=9?-iconSize/2:-26;
   if(entry.Icon!=null)dc.DrawImage(entry.Icon,new Rect(position.X-iconSize/2,position.Y+iconTop,iconSize,iconSize));
   else {dc.PushTransform(new TranslateTransform(position.X-iconSize/2,position.Y+iconTop));dc.PushTransform(new ScaleTransform(iconSize/256,iconSize/256));dc.DrawGeometry(selected?p.WheelSelectedText:p.Text,null,FilledIcon.GeometryFor(IconKind.App));dc.Pop();dc.Pop();}
   if(n<9)Text(dc,entry.Name,new Point(position.X,position.Y+16),12,selected?p.WheelSelectedText:p.Text,Math.Max(60,Math.Min(125,2*Math.PI*radius*.74/n-12)));
  }
  dc.DrawEllipse(translucentSurface,new Pen(p.Stroke,0.8),center,inner-5,inner-5);
  bool hasSelection=Selected>=0&&Selected<settings.Entries.Count;
  Text(dc,hasSelection?settings.Entries[Selected].Name:settings.BindName,new Point(center.X,center.Y-22),hasSelection?16:23,p.Text,inner*1.65);
  Text(dc,hasSelection?"Отпусти": "Удерживай",new Point(center.X,center.Y+16),12,p.Secondary,inner*1.7);
  if(hasSelection) {
   double rad=(Selected*360.0/n-90)*Math.PI/180;
   dc.DrawEllipse(p.Text,null,new Point(center.X+Math.Cos(rad)*(inner-17),center.Y+Math.Sin(rad)*(inner-17)),2.5,2.5);
  }
  var hint=new Rect(center.X-53,center.Y+radius+20,106,29);dc.DrawRoundedRectangle(p.Surface,new Pen(p.Stroke,0.7),hint,8,8);
  Text(dc,"Esc   Отмена",new Point(center.X,hint.Y+6),11,p.Secondary,100);
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
