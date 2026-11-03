using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Orbit;
internal static class Launcher
{
 private static readonly Dictionary<string,string> Aliases=new(StringComparer.OrdinalIgnoreCase) { ["calc"]="CalculatorApp", ["notepad"]="Notepad" };
 public static IntPtr Find(Entry entry)
 {
  string target=Path.GetFullPath(entry.Path); string name=Path.GetFileNameWithoutExtension(target);
  IntPtr found=IntPtr.Zero;
  Native.EnumWindows((h,_)=>{
   if(!Native.IsWindowVisible(h) || Native.GetWindow(h,4)!=IntPtr.Zero || (Native.GetWindowLong(h,-20)&0x80)!=0) return true;
   if(Native.DwmGetWindowAttribute(h,14,out int cloaked,4)==0 && cloaked!=0) return true;
   Native.GetWindowThreadProcessId(h,out uint pid);
   if(pid==Environment.ProcessId) return true;
   try {
    using var process=Process.GetProcessById((int)pid);
    bool match=string.Equals(process.MainModule?.FileName,target,StringComparison.OrdinalIgnoreCase);
    if(!match && Aliases.TryGetValue(name,out string? alias)) match=string.Equals(process.ProcessName,alias,StringComparison.OrdinalIgnoreCase);
    if(match && name.Equals("explorer",StringComparison.OrdinalIgnoreCase)) {var c=new StringBuilder(256); Native.GetClassName(h,c,c.Capacity); match=c.ToString() is "CabinetWClass" or "ExploreWClass";}
    if(match) {found=h;return false;}
   } catch { }
   return true;
  },IntPtr.Zero);
  return found;
 }
 public static string Open(Entry entry)
 {
  IntPtr window=Find(entry);
  if(window!=IntPtr.Zero) {
   if(Native.IsIconic(window)) Native.ShowWindowAsync(window,9);
   if(!Native.SetForegroundWindow(window)) return "Windows ограничила переключение. Нажми значок приложения на панели задач.";
   return "Переключено: "+entry.Name;
  }
  if(!File.Exists(entry.Path)) throw new FileNotFoundException("Файл программы не найден. Обнови путь в настройках.",entry.Path);
  Process.Start(new ProcessStartInfo(entry.Path,entry.Arguments) {UseShellExecute=true,WorkingDirectory=string.IsNullOrWhiteSpace(entry.WorkingDirectory)?Path.GetDirectoryName(entry.Path)!:entry.WorkingDirectory});
  return "Запущено: "+entry.Name;
 }
}
