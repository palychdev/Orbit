using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace Orbit;
internal sealed class EntryEditor : Window
{
 public EntryEditor(Entry entry)
 {
  Style=(Style)FindResource(typeof(Window));
  Title="Приложение";Width=540;Height=420;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;ShowInTaskbar=false;
  var stack=new StackPanel {Margin=new Thickness(24)};Content=stack;
  TextBox Field(string label,string value) {stack.Children.Add(new TextBlock {Text=label,Margin=new Thickness(0,0,0,5)});var box=new TextBox {Text=value,Margin=new Thickness(0,0,0,12)};stack.Children.Add(box);return box;}
  var name=Field("Название",entry.Name);var path=Field("Путь к .exe",entry.Path);var args=Field("Аргументы запуска (необязательно)",entry.Arguments);
  var buttons=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(0,10,0,0)};stack.Children.Add(buttons);
  var save=new Button {Content="Сохранить",IsDefault=true};var cancel=new Button {Content="Отмена",IsCancel=true};buttons.Children.Add(save);buttons.Children.Add(cancel);
  save.Click+=(_,_)=>{
   string p=Environment.ExpandEnvironmentVariables(path.Text.Trim().Trim('"'));
   if(string.IsNullOrWhiteSpace(name.Text) || !File.Exists(p) || !p.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)) {MessageBox.Show(this,"Укажи название и существующий .exe файл.","Проверь данные");return;}
   entry.Name=name.Text.Trim();entry.Path=System.IO.Path.GetFullPath(p);entry.Arguments=args.Text;DialogResult=true;
  };
 }
}
internal static class Shortcut
{
 public static Entry Read(string file)
 {
  string path=file,args="",dir="";
  if(file.EndsWith(".lnk",StringComparison.OrdinalIgnoreCase)) {
   object? shell=null,link=null;
   try {shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!);dynamic s=shell!;link=s.CreateShortcut(file);dynamic l=link;path=(string)l.TargetPath;args=(string)l.Arguments;dir=(string)l.WorkingDirectory;}
   finally {if(link!=null)Marshal.FinalReleaseComObject(link);if(shell!=null)Marshal.FinalReleaseComObject(shell);}
  }
  if(!path.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Этот ярлык не ведёт на .exe. Выбери исполняемый файл программы.");
  return new Entry {Name=System.IO.Path.GetFileNameWithoutExtension(file),Path=path,Arguments=args,WorkingDirectory=dir};
 }
}
