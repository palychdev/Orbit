using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Orbit;
// Opt-in integration checks. Uses real HWNDs/registrations/cursor APIs, but feeds
// explicit test packets to the production decoder: no synthetic system keystrokes.
internal static class NativeIntegrationTests
{
 internal static void RegistrationOnly()
 {
  int checks=0;
  var parameters=new HwndSourceParameters("Orbit raw input registration test") {ParentWindow=new IntPtr(-3),Width=1,Height=1};
  using var source=new HwndSource(parameters);
  uint deviceSize=(uint)Marshal.SizeOf<Native.RawDevice>();
  try {
   for(int i=0;i<100;i++) {
    var device=RawMouseInput.Device(source.Handle);
    if(!Native.RegisterRawInputDevices(new[]{device},1,deviceSize))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());checks++;
    var registered=Devices().Single(d=>d.UsagePage==1&&d.Usage==2);
    if(registered.Flags!=RawMouseInput.InputSink || registered.Target!=source.Handle)throw new Exception("Background sink registration mismatch");checks++;
    var remove=new Native.RawDevice {UsagePage=1,Usage=2,Flags=RawMouseInput.Remove};
    if(!Native.RegisterRawInputDevices(new[]{remove},1,deviceSize))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());checks++;
    if(Devices().Any(d=>d.UsagePage==1&&d.Usage==2))throw new Exception("Stale mouse registration");checks++;
   }
  } finally {
   Native.RegisterRawInputDevices(new[]{new Native.RawDevice {UsagePage=1,Usage=2,Flags=RawMouseInput.Remove}},1,deviceSize);
  }
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"native-test.txt"),$"PASS: {checks} Windows API registration checks. 100 register/unregister cycles on a hidden message-only HWND, INPUTSINK flag and target verified via GetRegisteredRawInputDevices.\nDoes not move the cursor or verify actual physical mouse delivery, Start, browser fullscreen or game rendering.");
 }
 internal static void Run()
 {
  int checks=0,chosen=0;
  void Check(bool condition,string name) {checks++;if(!condition)throw new Exception("FAIL: "+name);}
  var config=Settings.Defaults();config.Entries.Add(new Entry {Name="Fourth"});
  var wheel=new WheelWindow(config,attemptActivation:false);
  wheel.Chosen+=_=>chosen++;
  int header=Marshal.SizeOf<Native.RawHeader>();uint size=(uint)(header+24);
  IntPtr packet=Marshal.AllocHGlobal((int)size);
  try {
   Marshal.StructureToPtr(new Native.RawHeader {Type=0,Size=size},packet,false);
   Marshal.StructureToPtr(new Native.RawMouse {X=100,Y=0},packet+header,false);
   for(int cycle=0;cycle<40;cycle++) {
    Native.GetCursorPos(out var position);Native.GetClipCursor(out var clip);
    var foreground=Native.GetForegroundWindow();
    wheel.Begin();
    IntPtr hwnd=new WindowInteropHelper(wheel).Handle;
    Check(Native.GetForegroundWindow()!=hwnd,"selection does not require foreground");
    var device=Devices().Single(d=>d.UsagePage==1&&d.Usage==2);
    Check(device.Flags==RawMouseInput.InputSink&&device.Target==hwnd,"OS registered background mouse sink");
    Native.GetWindowRect(hwnd,out var rect);
    var monitor=new Native.MonitorInfo {Size=Marshal.SizeOf<Native.MonitorInfo>()};
    Native.GetMonitorInfo(Native.MonitorFromPoint(position,2),ref monitor);
    Check(Same(rect,monitor.Monitor),"overlay covers physical monitor bounds");
    wheel.ProcessRawPacket(packet,size);Check(wheel.Selected==1,"production decoder selects without activation/click");
    bool cancel=cycle%2==0;int before=chosen;
    wheel.Finish(cancel);
    Check(chosen==before+(cancel?0:1),"accept/cancel route");
    Native.GetClipCursor(out var afterClip);Native.GetCursorPos(out var afterPosition);
    Check(Same(clip,afterClip),"original clipping restored");
    Check(position.X==afterPosition.X&&position.Y==afterPosition.Y,"original cursor restored");
    Check(!Devices().Any(d=>d.UsagePage==1&&d.Usage==2),"raw input unregistered on close");
    Check(!wheel.IsVisible,"overlay hidden");
    Check(foreground==Native.GetForegroundWindow(),"background foreground preserved");
   }
  } finally {wheel.Finish(true);wheel.Close();Marshal.FreeHGlobal(packet);}
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"native-test.txt"),$"PASS: {checks} native integration checks, 40 background open/close cycles. Real HWND, OS INPUTSINK registration, physical screen bounds, decoder-to-selection route, cursor position/clip restoration, input unregister and accept/cancel.\nMovement packets are supplied by the test; actual physical mouse/game rendering is not simulated.");
 }
 private static bool Same(Native.Rect a,Native.Rect b)=>a.Left==b.Left&&a.Top==b.Top&&a.Right==b.Right&&a.Bottom==b.Bottom;
 private static Native.RawDevice[] Devices()
 {
  uint count=0,size=(uint)Marshal.SizeOf<Native.RawDevice>();
  if(Native.GetRegisteredRawInputDevices(null,ref count,size)==uint.MaxValue)throw new InvalidOperationException("GetRegisteredRawInputDevices failed");
  if(count==0)return Array.Empty<Native.RawDevice>();
  var devices=new Native.RawDevice[count];
  if(Native.GetRegisteredRawInputDevices(devices,ref count,size)==uint.MaxValue)throw new InvalidOperationException("GetRegisteredRawInputDevices failed");
  return devices;
 }
}
