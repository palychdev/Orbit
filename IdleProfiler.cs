using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;

namespace Orbit;
// Explicit developer mode only. Normal operation has no profiling timers.
internal static class IdleProfiler
{
 internal static void Start(App app,bool mouse)
 {
  var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};
  TimeSpan cpu=default;long allocated=0;var clock=new Stopwatch();bool sampling=false;
  timer.Tick+=(_,_)=>{
   using var process=Process.GetCurrentProcess();
   if(!sampling){cpu=process.TotalProcessorTime;allocated=GC.GetTotalAllocatedBytes();clock.Start();sampling=true;timer.Interval=TimeSpan.FromSeconds(8);return;}
   timer.Stop();clock.Stop();process.Refresh();double used=(process.TotalProcessorTime-cpu).TotalMilliseconds;
   var report=new {Mode=mouse?"Mouse4":"F8",Seconds=clock.Elapsed.TotalSeconds,CpuOneCorePercent=used/clock.Elapsed.TotalMilliseconds*100,CpuMachinePercent=used/clock.Elapsed.TotalMilliseconds*100/Environment.ProcessorCount,WorkingSetMiB=process.WorkingSet64/1048576.0,PrivateMiB=process.PrivateMemorySize64/1048576.0,AllocatedKiB=(GC.GetTotalAllocatedBytes()-allocated)/1024.0,MouseHook=app.Input.MouseHookInstalled,Context="Fresh normal app in tray, 3 default applications, 2s warmup then 8s idle; this machine only"};
   File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"idle-"+(mouse?"mouse":"keyboard")+".json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));app.Shutdown();
  };timer.Start();
 }
}
