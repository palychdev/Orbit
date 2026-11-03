using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Automation;
using System.Windows.Media;
using Microsoft.Win32;

namespace Orbit;
public partial class MainWindow : Window
{
 private readonly App app;
 private bool ready,recording;
 private Entry? removed;
 private int removedIndex;
 internal MainWindow(App app)
 {
  this.app=app; InitializeComponent();
  SizeSlider.Value=app.Config.Size; SensitivitySlider.Value=app.Config.Sensitivity; OpacitySlider.Value=app.Config.Opacity;
  try {using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");StartupCheck.IsChecked=key?.GetValue("Orbit") is string;}
  catch {StartupCheck.IsEnabled=false;StartupCheck.ToolTip="Чтение автозапуска ограничено системой.";}
  BindButton.Content=FooterBind.Content=app.Config.BindName; Refresh(); ready=true;UpdateValues();UpdateAppearance();
  SourceInitialized+=(_,_)=>ThemeManager.ApplyTitleBar(this,app.Config.Theme);
  PreviewKeyDown+=RecordKey;
  Deactivated+=(_,_)=>StopRecording();
  Closing+=(_,e)=>{if(!app.Exiting) {e.Cancel=true; StopRecording(); Hide();}};
 }
 private void Refresh(int selected=-1) {
  for(int i=0;i<app.Config.Entries.Count;i++)app.Config.Entries[i].Slot=(i+1).ToString();
  AppsList.ItemsSource=null;AppsList.ItemsSource=app.Config.Entries;AppsList.SelectedIndex=selected;
  CountLabel.Text=app.Config.Entries.Count.ToString();AddButton.IsEnabled=app.Config.Entries.Count<12;
  EmptyState.Visibility=app.Config.Entries.Count==0?Visibility.Visible:Visibility.Collapsed;
  ListSurface.Visibility=app.Config.Entries.Count==0?Visibility.Collapsed:Visibility.Visible;
  Selection_Changed(null!,null!);
 }
 public void Report(string text) {Status.Text=text;Status.ToolTip=text;}
 private bool Save() {try {app.Config.Save(); app.Input?.Configure(app.Config);Status.SetResourceReference(TextBlock.ForegroundProperty,"SecondaryBrush");Report("Сохранено");return true;} catch(Exception ex) {Status.SetResourceReference(TextBlock.ForegroundProperty,"DangerBrush");Report("Не удалось сохранить: "+ex.Message);return false;}}
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
 private void RowEdit_Click(object sender,RoutedEventArgs e) {e.Handled=true;if(sender is Button {Tag:Entry entry}){AppsList.SelectedItem=entry;Edit_Click(sender,e);}}
 private void Remove_Click(object sender,RoutedEventArgs e) {int i=AppsList.SelectedIndex;if(i<0)return;removed=app.Config.Entries[i];removedIndex=i;app.Config.Entries.RemoveAt(i);Refresh(Math.Min(i,app.Config.Entries.Count-1));if(Save())Report("Удалено: "+removed.Name);UndoButton.Visibility=Visibility.Visible;}
 private void Undo_Click(object sender,RoutedEventArgs e) {
  if(removed==null)return;
  if(app.Config.Entries.Count>=12){Report("Нет свободного места в колесе.");return;}
  int index=Math.Min(removedIndex,app.Config.Entries.Count);app.Config.Entries.Insert(index,removed);removed=null;UndoButton.Visibility=Visibility.Collapsed;Refresh(index);Save();
 }
 private void Up_Click(object sender,RoutedEventArgs e) => Reorder(-1);
 private void Down_Click(object sender,RoutedEventArgs e) => Reorder(1);
 private void Reorder(int d) {int i=AppsList.SelectedIndex,j=i+d;if(i<0||j<0||j>=app.Config.Entries.Count)return;var entry=app.Config.Entries[i];app.Config.Entries.RemoveAt(i);app.Config.Entries.Insert(j,entry);Refresh(j);Save();}
 private void Hide_Click(object sender,RoutedEventArgs e) {StopRecording();Hide();}
 private void Bind_Click(object sender,RoutedEventArgs e) {recording=true;app.Input.Suspended=true;BindButton.Content="Нажми клавишу…";BindHint.Text="Ctrl, Alt, Shift + клавиша. Esc — отмена.";BindHint.Visibility=Visibility.Visible;BindButton.Focus();}
 private void StopRecording() {if(!recording)return;recording=false;app.Input.Suspended=false;BindButton.Content=FooterBind.Content=app.Config.BindName;BindHint.Text="";BindHint.Visibility=Visibility.Collapsed;}
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
  if(!ready)return;app.Config.Size=SizeSlider.Value;app.Config.Sensitivity=SensitivitySlider.Value;app.Config.Opacity=OpacitySlider.Value;UpdateValues();Save();
 }
 private void UpdateValues() {SizeValue.Text=$"{app.Config.Size:0} px";SensitivityValue.Text=$"{app.Config.Sensitivity:0.0}×";OpacityValue.Text=$"{app.Config.Opacity:P0}";}
 private void Page_Changed(object sender,RoutedEventArgs e) {
  if(AppsPage==null||SettingsPage==null)return;
  bool settings=SettingsTab.IsChecked==true;AppsPage.Visibility=settings?Visibility.Collapsed:Visibility.Visible;SettingsPage.Visibility=settings?Visibility.Visible:Visibility.Collapsed;
  if(!settings)StopRecording();
 }
 private void FooterBind_Click(object sender,RoutedEventArgs e) {SettingsTab.IsChecked=true;Bind_Click(sender,e);}
 private void Theme_Click(object sender,RoutedEventArgs e) {app.Config.Theme=app.Config.Theme=="Light"?"Dark":"Light";ThemeManager.Apply(app,app.Config.Theme);UpdateAppearance();Save();}
 private void UpdateAppearance() {
  bool light=app.Config.Theme=="Light";
  ThemeButton.ToolTip=light?"Тёмная тема":"Светлая тема";AutomationProperties.SetName(ThemeButton,(string)ThemeButton.ToolTip);
  ThemeGlyph.Data=Geometry.Parse(light?"M20,15 A9,9 0 1 1 9,3 A7,7 0 0 0 20,15":"M12,7 A5,5 0 1 1 11.99,7 M12,0 L12,3 M12,21 L12,24 M0,12 L3,12 M21,12 L24,12 M3.5,3.5 L5.5,5.5 M18.5,18.5 L20.5,20.5 M3.5,20.5 L5.5,18.5 M18.5,5.5 L20.5,3.5");
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
