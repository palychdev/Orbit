using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Text.Json.Serialization;

namespace Orbit;
public sealed class Entry
{
 public string Name { get; set; } = "";
 public string Path { get; set; } = "";
 public string Arguments { get; set; } = "";
 public string WorkingDirectory { get; set; } = "";
 [JsonIgnore] public ImageSource? Icon { get; set; }
 [JsonIgnore] public string Slot { get; set; } = "";
 [JsonIgnore] public string FileName => System.IO.Path.GetFileName(Path);
 public void LoadIcon()
 {
  Icon=ShellIcons.Load(Path);
 }
}
public sealed class Settings
{
 public int Key { get; set; } = 0x77;
 public int Modifiers { get; set; }
 public double Size { get; set; } = 440;
 public double Sensitivity { get; set; } = 1;
 public string Theme { get; set; } = "Dark";
 public List<Entry> Entries { get; set; } = new();
 [JsonIgnore] public string BindName => BindingName(Key, Modifiers);
 public static string BindingName(int key,int mods) => ((mods&2)!=0?"Ctrl + ":"")+((mods&1)!=0?"Alt + ":"")+((mods&4)!=0?"Shift + ":"")+(key switch{4=>"Средняя кнопка",5=>"Mouse 4",6=>"Mouse 5",_=>System.Windows.Input.KeyInterop.KeyFromVirtualKey(key).ToString()});
 public static readonly string DataFile = System.IO.Path.Combine(AppContext.BaseDirectory,"Data","settings.json");
 public static Settings Load()
 {
  if(!File.Exists(DataFile)) return Defaults();
  var s=JsonSerializer.Deserialize<Settings>(File.ReadAllText(DataFile)) ?? throw new InvalidDataException("Пустой файл настроек.");
  s.Size=Math.Clamp(s.Size,300,640); s.Sensitivity=Math.Clamp(s.Sensitivity,0.4,2.5);
  if(!HotkeyBinding.IsAllowed(s.Key)) {s.Key=0x77; s.Modifiers=0;}
  s.Modifiers &=7; s.Entries ??= new(); s.Entries.RemoveAll(e=>e==null || string.IsNullOrWhiteSpace(e.Path));
  if(s.Theme!="Light")s.Theme="Dark";
  if(s.Entries.Count>12) s.Entries.RemoveRange(12,s.Entries.Count-12);
  return s;
 }
 public static Settings Defaults() => new() { Entries=new() {
  new() { Name="Блокнот", Path=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","notepad.exe") },
  new() { Name="Проводник", Path=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe") },
  new() { Name="Калькулятор", Path=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","calc.exe") }
 }};
 public void Save()
 {
  Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DataFile)!);
  File.WriteAllText(DataFile+".tmp",JsonSerializer.Serialize(this,new JsonSerializerOptions {WriteIndented=true}));
  File.Move(DataFile+".tmp",DataFile,true);
 }
}
public static class Selection
{
 public static int Index(double x,double y,int count,double deadZone=24)
 {
  if(count<=0 || x*x+y*y<deadZone*deadZone) return -1;
  double angle=(Math.Atan2(y,x)*180/Math.PI+90+360)%360;
  return (int)Math.Floor((angle+180.0/count)%360/(360.0/count));
 }
}
