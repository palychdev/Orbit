using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using Forms=System.Windows.Forms;

namespace Orbit;
internal sealed class InputService : IDisposable
{
 private readonly Dispatcher dispatcher;
 private readonly Thread thread;
 private readonly Native.HookProc keyboardCallback,mouseCallback;
 private readonly object stateLock=new();
 private readonly HotkeyState state=new();
 private Forms.ApplicationContext? context;
 private Forms.Control? control;
 private IntPtr keyboardHook,mouseHook;
 private volatile bool suspended;
 private volatile int mouseKey;
 public event Action? Pressed;
 public event Action<bool>? Released;
 public bool Suspended {get=>suspended;set=>suspended=value;}
 internal bool MouseHookInstalled=>Interlocked.CompareExchange(ref mouseHook,IntPtr.Zero,IntPtr.Zero)!=IntPtr.Zero;
 public static bool IsModifier(int k)=>k is 16 or 17 or 18 or 91 or 92 or 160 or 161 or 162 or 163 or 164 or 165;
 public static bool Down(int k)=>(Native.GetAsyncKeyState(k)&0x8000)!=0;
 public static int CurrentModifiers()=>(Down(17)?2:0)|(Down(18)?1:0)|(Down(16)?4:0)|(Down(91)||Down(92)?8:0);

 public InputService(Dispatcher dispatcher)
 {
  this.dispatcher=dispatcher;keyboardCallback=OnKey;mouseCallback=OnMouse;
  Exception? error=null;using var ready=new ManualResetEventSlim();
  thread=new Thread(()=>{
   try {
    context=new Forms.ApplicationContext();control=new Forms.Control();_=control.Handle;
    keyboardHook=Native.SetWindowsHookEx(13,keyboardCallback,Native.GetModuleHandle(null),0);
    if(keyboardHook==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());
   }catch(Exception ex){error=ex;}
   ready.Set();
   try{if(error==null)Forms.Application.Run(context!);}
   finally {
    if(mouseHook!=IntPtr.Zero){Native.UnhookWindowsHookEx(mouseHook);mouseHook=IntPtr.Zero;}
    if(keyboardHook!=IntPtr.Zero)Native.UnhookWindowsHookEx(keyboardHook);
    control?.Dispose();context?.Dispose();
   }
  }){IsBackground=true,Name="Orbit input"};
  thread.SetApartmentState(ApartmentState.STA);thread.Start();ready.Wait();if(error!=null)throw error;
 }

 public void Configure(Settings settings)
 {
  if(!HotkeyBinding.IsAllowed(settings.Key))throw new ArgumentOutOfRangeException(nameof(settings.Key));
  int requested=HotkeyBinding.IsMouse(settings.Key)?settings.Key:0;
  // Install a mouse hook only for a mouse binding: keyboard users pay no mouse-move cost.
  control!.Invoke(new Action(()=>{
   if(requested!=0&&mouseHook==IntPtr.Zero){mouseHook=Native.SetWindowsHookEx(14,mouseCallback,Native.GetModuleHandle(null),0);if(mouseHook==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());}
   if(requested==0&&mouseHook!=IntPtr.Zero){if(!Native.UnhookWindowsHookEx(mouseHook))throw new Win32Exception(Marshal.GetLastWin32Error());mouseHook=IntPtr.Zero;}
   mouseKey=requested;
   lock(stateLock)state.Configure(settings.Key,settings.Modifiers,Down(settings.Key));
  }));
 }
 private IntPtr Dispatch(int key,bool down,bool up,IntPtr hook,int code,IntPtr w,IntPtr l)
 {
  HotkeyResult result;lock(stateLock)result=state.Handle(key,down,up,CurrentModifiers(),suspended);
  if((result&HotkeyResult.Open)!=0)dispatcher.BeginInvoke(()=>Pressed?.Invoke());
  if((result&HotkeyResult.Accept)!=0)dispatcher.BeginInvoke(()=>Released?.Invoke(false));
  if((result&HotkeyResult.Cancel)!=0)dispatcher.BeginInvoke(()=>Released?.Invoke(true));
  return (result&HotkeyResult.Suppress)!=0?new IntPtr(1):Native.CallNextHookEx(hook,code,w,l);
 }
 private IntPtr OnKey(int code,IntPtr w,IntPtr l)
 {
  if(code<0)return Native.CallNextHookEx(keyboardHook,code,w,l);
  int message=w.ToInt32();return Dispatch(Marshal.ReadInt32(l),message is 0x100 or 0x104,message is 0x101 or 0x105,keyboardHook,code,w,l);
 }
 private IntPtr OnMouse(int code,IntPtr w,IntPtr l)
 {
  int message=w.ToInt32();
  // Movement, scrolling, LMB and RMB take the allocation-free pass-through path.
  if(code<0||mouseKey==0||message is not (0x207 or 0x208 or 0x20B or 0x20C))return Native.CallNextHookEx(mouseHook,code,w,l);
  if(!HotkeyBinding.TryMouseMessage(message,unchecked((uint)Marshal.ReadInt32(l,8)),out int key,out bool down,out bool up)||key!=mouseKey)return Native.CallNextHookEx(mouseHook,code,w,l);
  return Dispatch(key,down,up,mouseHook,code,w,l);
 }
 public void Dispose(){context?.ExitThread();thread.Join(1500);}
}
