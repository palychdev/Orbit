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
 private readonly Native.HookProc callback;
 private Forms.ApplicationContext? context;
 private IntPtr hook;
 private volatile bool suspended;
 private readonly object stateLock=new();
 private readonly HotkeyState state=new();
 public event Action? Pressed;
 public event Action<bool>? Released;
 public bool Suspended { get=>suspended; set {suspended=value;} }
 public static bool IsModifier(int k) => k is 16 or 17 or 18 or 91 or 92 or 160 or 161 or 162 or 163 or 164 or 165;
 public static bool Down(int k) => (Native.GetAsyncKeyState(k)&0x8000)!=0;
 public static int CurrentModifiers() => (Down(17)?2:0)|(Down(18)?1:0)|(Down(16)?4:0)|(Down(91)||Down(92)?8:0);
 public void Configure(Settings settings) {lock(stateLock)state.Configure(settings.Key,settings.Modifiers,Down(settings.Key));}
 public InputService(Dispatcher dispatcher)
 {
  this.dispatcher=dispatcher; callback=OnKey;
  Exception? error=null;
  using var ready=new ManualResetEventSlim();
  thread=new Thread(()=>{
   context=new Forms.ApplicationContext();
   hook=Native.SetWindowsHookEx(13,callback,Native.GetModuleHandle(null),0);
   if(hook==IntPtr.Zero) error=new Win32Exception(Marshal.GetLastWin32Error());
   ready.Set();
   if(hook!=IntPtr.Zero) Forms.Application.Run(context);
   if(hook!=IntPtr.Zero) Native.UnhookWindowsHookEx(hook);
  }) {IsBackground=true,Name="Orbit hotkey"};
  thread.SetApartmentState(ApartmentState.STA); thread.Start(); ready.Wait();
  if(error!=null) throw error;
 }
 private IntPtr OnKey(int code,IntPtr w,IntPtr l)
 {
  if(code<0) return Native.CallNextHookEx(hook,code,w,l);
  int vk=Marshal.ReadInt32(l); bool down=w.ToInt32() is 0x100 or 0x104; bool up=w.ToInt32() is 0x101 or 0x105;
  HotkeyResult result;lock(stateLock)result=state.Handle(vk,down,up,CurrentModifiers(),suspended);
  if((result&HotkeyResult.Open)!=0)dispatcher.BeginInvoke(()=>Pressed?.Invoke());
  if((result&HotkeyResult.Accept)!=0)dispatcher.BeginInvoke(()=>Released?.Invoke(false));
  if((result&HotkeyResult.Cancel)!=0)dispatcher.BeginInvoke(()=>Released?.Invoke(true));
  return (result&HotkeyResult.Suppress)!=0?new IntPtr(1):Native.CallNextHookEx(hook,code,w,l);
 }
 public void Dispose()
 {
  // ApplicationContext.ExitThread is thread-safe and posts WM_QUIT to its own thread.
  context?.ExitThread(); thread.Join(1500);
 }
}
