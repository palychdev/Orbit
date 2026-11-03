using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace Orbit;
public partial class EntryEditor : Window
{
 private readonly Entry entry;
 public EntryEditor(Entry entry)
 {
  this.entry=entry;InitializeComponent();
  NameBox.Text=entry.Name;PathBox.Text=entry.Path;ArgumentsBox.Text=entry.Arguments;DirectoryBox.Text=entry.WorkingDirectory;
  SourceInitialized+=(_,_)=>ThemeManager.ApplyTitleBar(this,((App)Application.Current).Config.Theme);
  Loaded+=(_,_)=>{NameBox.Focus();NameBox.SelectAll();};
 }
 private void Browse_Click(object sender,RoutedEventArgs e) {
  var dialog=new Microsoft.Win32.OpenFileDialog {Title="Выбери приложение",Filter="Приложения|*.exe",CheckFileExists=true};
  if(dialog.ShowDialog(this)==true)PathBox.Text=dialog.FileName;
 }
 internal bool TryApply()
 {
  string path=Environment.ExpandEnvironmentVariables(PathBox.Text.Trim().Trim('"'));
  string directory=Environment.ExpandEnvironmentVariables(DirectoryBox.Text.Trim().Trim('"'));
  if(string.IsNullOrWhiteSpace(NameBox.Text)){Error("Укажи название.",NameBox);return false;}
  if(!File.Exists(path)||!path.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)){Error("Выбери существующий .exe файл.",PathBox);return false;}
  if(directory.Length>0&&!Directory.Exists(directory)){Advanced.IsExpanded=true;Error("Рабочая папка не найдена.",DirectoryBox);return false;}
  entry.Name=NameBox.Text.Trim();entry.Path=System.IO.Path.GetFullPath(path);entry.Arguments=ArgumentsBox.Text;entry.WorkingDirectory=directory;
  ValidationMessage.Visibility=Visibility.Collapsed;return true;
 }
 private void Error(string text,TextBox field) {ValidationMessage.Text=text;ValidationMessage.Visibility=Visibility.Visible;field.Focus();}
 private void Save_Click(object sender,RoutedEventArgs e) {if(TryApply())DialogResult=true;}
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
