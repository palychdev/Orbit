using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace Orbit;
public partial class MainWindow : Window
{
 private readonly App app;
 private bool ready,recording;
 internal MainWindow(App app)
 {
  this.app=app; InitializeComponent();
  SizeSlider.Value=app.Config.Size; SensitivitySlider.Value=app.Config.Sensitivity; OpacitySlider.Value=app.Config.Opacity;
  try {using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");StartupCheck.IsChecked=key?.GetValue("Orbit") is string;}
  catch {StartupCheck.IsEnabled=false;StartupCheck.ToolTip="Чтение автозапуска ограничено системой.";}
  BindButton.Content=app.Config.BindName; Refresh(); ready=true;
  PreviewKeyDown+=RecordKey;
  Deactivated+=(_,_)=>StopRecording();
  Closing+=(_,e)=>{if(!app.Exiting) {e.Cancel=true; StopRecording(); Hide();}};
 }
 private void Refresh(int selected=-1) {AppsList.ItemsSource=null; AppsList.ItemsSource=app.Config.Entries; AppsList.SelectedIndex=selected; Selection_Changed(null!,null!);}
 public void Report(string text) {Status.Text=text;}
 private void Save() {try {app.Config.Save(); app.Input.Configure(app.Config); Report("Сохранено · удерживай "+app.Config.BindName+" для вызова колеса");} catch(Exception ex) {Report("Не удалось сохранить: "+ex.Message);}}
 private void Selection_Changed(object sender,SelectionChangedEventArgs e) {if(EditButton==null)return; int i=AppsList.SelectedIndex; EditButton.IsEnabled=RemoveButton.IsEnabled=i>=0; UpButton.IsEnabled=i>0; DownButton.IsEnabled=i>=0 && i<app.Config.Entries.Count-1;}
 private void Add_Click(object sender,RoutedEventArgs e)
 {
  if(app.Config.Entries.Count>=12) {Report("Максимум 12 секторов. Удали один, чтобы добавить другой.");return;}
  var dlg=new OpenFileDialog {Title="Выбери приложение или ярлык",Filter="Приложения и ярлыки|*.exe;*.lnk",CheckFileExists=true,DereferenceLinks=true};
  if(dlg.ShowDialog(this)!=true) return;
  try {
   var entry=Shortcut.Read(dlg.FileName);
   var editor=new EntryEditor(entry) {Owner=this};
   if(editor.ShowDialog()==true) {entry.LoadIcon(); app.Config.Entries.Add(entry);Refresh(app.Config.Entries.Count-1);Save();}
  } catch(Exception ex) {Report(ex.Message);}
 }
 private void Edit_Click(object sender,RoutedEventArgs e)
 {
  if(AppsList.SelectedItem is not Entry entry)return;
  if(new EntryEditor(entry) {Owner=this}.ShowDialog()==true) {entry.LoadIcon(); int i=AppsList.SelectedIndex;Refresh(i);Save();}
 }
 private void Remove_Click(object sender,RoutedEventArgs e) {int i=AppsList.SelectedIndex;if(i<0)return;app.Config.Entries.RemoveAt(i);Refresh(Math.Min(i,app.Config.Entries.Count-1));Save();}
 private void Up_Click(object sender,RoutedEventArgs e) => Reorder(-1);
 private void Down_Click(object sender,RoutedEventArgs e) => Reorder(1);
 private void Reorder(int d) {int i=AppsList.SelectedIndex,j=i+d;if(i<0||j<0||j>=app.Config.Entries.Count)return;var entry=app.Config.Entries[i];app.Config.Entries.RemoveAt(i);app.Config.Entries.Insert(j,entry);Refresh(j);Save();}
 private void Preview_Click(object sender,RoutedEventArgs e) {StopRecording();app.BeginWheel(true);}
 private void Hide_Click(object sender,RoutedEventArgs e) {StopRecording();Hide();}
 private void Bind_Click(object sender,RoutedEventArgs e) {recording=true;app.Input.Suspended=true;BindButton.Content="Нажми клавишу…";BindHint.Text="Можно с Ctrl / Alt / Shift. Esc — отменить.";BindButton.Focus();}
 private void StopRecording() {if(!recording)return;recording=false;app.Input.Suspended=false;BindButton.Content=app.Config.BindName;BindHint.Text="Нажми, чтобы задать клавишу или сочетание.";}
 private void RecordKey(object sender,KeyEventArgs e)
 {
  if(!recording)return; e.Handled=true;var key=e.Key==Key.System?e.SystemKey:e.Key;
  int vk=KeyInterop.VirtualKeyFromKey(key); if(InputService.IsModifier(vk))return;
  if(key==Key.Escape) {StopRecording();return;}
  int mods=InputService.CurrentModifiers();
  if((mods&8)!=0) {BindHint.Text="Используй сочетание без клавиши Windows.";return;}
  if(key is Key.Tab or Key.Return or Key.Back || (mods==1 && key==Key.F4) || ((mods&3)==3 && key==Key.Delete)) {BindHint.Text="Выбери другую клавишу, например F8 или Ctrl + Space.";return;}
  app.Config.Key=vk;app.Config.Modifiers=mods;StopRecording();Save();
 }
 private void Options_Changed(object sender,RoutedPropertyChangedEventArgs<double> e)
 {
  if(!ready)return;app.Config.Size=SizeSlider.Value;app.Config.Sensitivity=SensitivitySlider.Value;app.Config.Opacity=OpacitySlider.Value;Save();
 }
 private void Startup_Click(object sender,RoutedEventArgs e)
 {
  try {
   using var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
   if(StartupCheck.IsChecked==true) key.SetValue("Orbit","\""+Environment.ProcessPath+"\" --tray");else key.DeleteValue("Orbit",false);
   Report("Автозапуск "+(StartupCheck.IsChecked==true?"включён":"выключен"));
  } catch(Exception ex) {StartupCheck.IsChecked=StartupCheck.IsChecked!=true;Report(ex.Message);}
 }
}
