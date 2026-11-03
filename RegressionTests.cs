using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Orbit;
internal static class RegressionTests
{
 internal static int Run()
 {
  int count=0;
  void Check(bool condition,string name) {count++;if(!condition)throw new Exception("FAIL: "+name);}
  var config=Settings.Defaults();
  config.Entries.Add(new Entry {Name="Fourth",Path="fourth.exe"});
  var host=new FakeHost();var session=new WheelSession(config,host);
  session.Move(100,0);Check(session.Selected==-1,"hidden session ignores movement");
  for(int cycle=0;cycle<1000;cycle++) {
   bool preview=cycle%5==0,cancel=cycle%7==0;
   Check(session.Begin(preview),"reopen without click");
   Check(session.Selected==-1,"new session clears selection");
   Check(!session.Begin(false),"key repeat does not restart");
   session.Move(3,3);Check(session.Selected==-1,"micro-movement cancels");
   session.Move(120,-3);Check(session.Selected==1,"background motion selects right");
   var chosen=session.Finish(cancel);
   Check(ReferenceEquals(chosen,!preview&&!cancel?config.Entries[1]:null),"cancel/preview/accept routing");
   Check(!session.Active&&!host.Input&&!host.Pointer&&!host.Visible,"all resources released");
   Check(session.Finish(false)==null,"repeated finish never launches twice");
  }
  Check(session.Begin(false),"snapshot session starts");session.Move(0,-100);
  var original=config.Entries[0];config.Entries.RemoveAt(0);
  Check(ReferenceEquals(session.Finish(false),original),"selected entry snapshot survives list edit");
  foreach(string failure in new[]{"register","show","capture","release","unregister","hide"}) {
   host=new FakeHost {Fail=failure};session=new WheelSession(config,host);
   bool threw=false;
   try {session.Begin(false);session.Move(100,0);session.Finish(false);}catch(InvalidOperationException){threw=true;}catch(AggregateException){threw=true;}
   Check(threw,"failure reported: "+failure);
   Check(!session.Active,"failure clears active flag: "+failure);
   Check(host.Cleanup.SequenceEqual(new[]{"release","unregister","hide"}),"all cleanup attempted: "+failure);
   host.Fail="";host.Cleanup.Clear();
   Check(session.Begin(false),"recovery after failure: "+failure);session.Move(100,0);
   Check(session.Finish(false)!=null,"selection works after failure: "+failure);
  }
  var empty=new WheelSession(new Settings(),new FakeHost());Check(!empty.Begin(false),"empty wheel does not acquire input");
  for(int n=1;n<=12;n++)for(int degree=0;degree<360;degree++) {
   double a=degree*Math.PI/180;
   Check(Selection.Index(Math.Cos(a)*200,Math.Sin(a)*200,n)>=0,"full angular coverage");
  }

  var keys=new HotkeyState();keys.Configure(0x77,0,false);
  HotkeyResult Send(int key,bool down,int mods=0,bool suspended=false)=>keys.Handle(key,down,!down,mods,suspended);
  Check(Send(0x77,true)==(HotkeyResult.Open|HotkeyResult.Suppress),"bind opens");
  for(int i=0;i<100;i++)Check(Send(0x77,true)==HotkeyResult.Suppress,"autorepeat suppressed");
  Check(Send(0x77,false)==(HotkeyResult.Accept|HotkeyResult.Suppress),"release accepts once");
  Check(Send(0x77,false)==HotkeyResult.None,"duplicate release ignored");
  Send(0x77,true);Check(Send(27,true)==(HotkeyResult.Cancel|HotkeyResult.Suppress),"escape cancels");
  Check(Send(27,false)==HotkeyResult.Suppress,"escape release swallowed with press");
  Check(Send(0x77,true)==HotkeyResult.Suppress,"held bind after escape cannot reopen");Send(0x77,false);
  Check((Send(0x77,true)&HotkeyResult.Open)!=0,"reopen after escape without click");Send(0x77,false);
  Check(Send(0x77,true,0,true)==HotkeyResult.None,"bind recording suspends hook");
  Check(Send(0x77,true,2)==HotkeyResult.None,"wrong modifiers ignored");
  Check(Send(0x77,true,8)==HotkeyResult.None,"Windows shortcuts not hijacked");
  keys.Configure(0x20,2,false);
  Check((Send(0x20,true,2)&HotkeyResult.Open)!=0,"Ctrl+Space opens");
  Check(Send(161,false,2)==HotkeyResult.None,"unrelated modifier release keeps wheel open");
  Check(Send(162,false)==HotkeyResult.Accept,"required modifier release accepts");
  Check(Send(0x20,false)==HotkeyResult.Suppress,"main release after modifier never accepts twice");
  keys.Configure(0x77,0,true);
  Check(Send(0x77,true)==HotkeyResult.None,"held recorded key cannot open");Send(0x77,false);
  Check((Send(0x77,true)&HotkeyResult.Open)!=0,"recorded key opens after release");
  Check(Send(9,true,1)==HotkeyResult.Cancel,"Alt+Tab cancels without blocking switch");Send(0x77,false);
  Check((Send(0x77,true)&HotkeyResult.Open)!=0,"reopen after Alt+Tab");Send(0x77,false);
  for(int i=0;i<1000;i++) {
   Check((Send(0x77,true)&HotkeyResult.Open)!=0,"rapid press");
   Check((Send(0x77,false)&HotkeyResult.Accept)!=0,"rapid release");
  }

  int headerSize=Marshal.SizeOf<Native.RawHeader>();uint packetSize=(uint)(headerSize+24);
  IntPtr packet=Marshal.AllocHGlobal((int)packetSize);
  try {
   void Write(uint type,uint size,ushort flags,int dx,int dy) {
    Marshal.StructureToPtr(new Native.RawHeader {Type=type,Size=size},packet,false);
    Marshal.StructureToPtr(new Native.RawMouse {Flags=flags,X=dx,Y=dy},packet+headerSize,false);
   }
   Write(0,packetSize,0,-42,71);
   Check(RawMouseInput.Decode(packet,packetSize,out int dx,out int dy)&&dx==-42&&dy==71,"signed relative raw input");
   Check(!RawMouseInput.Decode(IntPtr.Zero,packetSize,out _,out _),"null packet rejected");
   for(uint size=0;size<packetSize;size++)Check(!RawMouseInput.Decode(packet,size,out _,out _),"truncated packet rejected");
   Write(1,packetSize,0,20,30);Check(!RawMouseInput.Decode(packet,packetSize,out _,out _),"keyboard packet rejected");
   Write(0,packetSize,1,20,30);Check(!RawMouseInput.Decode(packet,packetSize,out _,out _),"absolute device ignored");
   Write(0,packetSize,0,0,0);Check(!RawMouseInput.Decode(packet,packetSize,out _,out _),"button-only packet not movement");
   Write(0,packetSize+1,0,20,30);Check(!RawMouseInput.Decode(packet,packetSize,out _,out _),"oversized header rejected");
   var random=new Random(12345);
   for(int i=0;i<1000;i++) {
    int px=random.Next(-5000,5001),py=random.Next(-5000,5001);Write(0,packetSize,0,px,py);
    Check(RawMouseInput.Decode(packet,packetSize,out dx,out dy)==(px!=0||py!=0)&&dx==px&&dy==py,"relative packet decoding");
   }
  } finally {Marshal.FreeHGlobal(packet);}
  var device=RawMouseInput.Device(new IntPtr(123));
  Check(device.Flags==0x100&&device.Target==new IntPtr(123)&&device.UsagePage==1&&device.Usage==2,"INPUTSINK registration independent of foreground");
  Check(Marshal.SizeOf<Native.RawMouse>()==24,"native RAWMOUSE layout");
  return count;
 }

 private sealed class FakeHost : IWheelHost
 {
  internal string Fail="";
  internal bool Input,Pointer,Visible;
  internal readonly List<string> Cleanup=new();
  private void MaybeFail(string stage) {if(Fail==stage)throw new InvalidOperationException(stage);}
  public void RegisterInput() {Input=true;MaybeFail("register");}
  public void ShowOverlay() {Visible=true;MaybeFail("show");}
  public void CapturePointer() {Pointer=true;MaybeFail("capture");}
  public void ReleasePointer() {Cleanup.Add("release");Pointer=false;MaybeFail("release");}
  public void UnregisterInput() {Cleanup.Add("unregister");Input=false;MaybeFail("unregister");}
  public void HideOverlay() {Cleanup.Add("hide");Visible=false;MaybeFail("hide");}
 }
}
