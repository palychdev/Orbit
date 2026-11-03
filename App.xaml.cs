using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Forms=System.Windows.Forms;

namespace Orbit;
public partial class App : Application
{
 internal Settings Config {get;private set;}=null!;
 internal InputService Input {get;private set;}=null!;
 internal bool Exiting {get;private set;}
 private MainWindow? settingsWindow;
 private WheelWindow? wheel;
 private Forms.NotifyIcon? tray;
 private Mutex? mutex;
 protected override void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);
  void Log(string message) {try {File.AppendAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"startup.log"),DateTime.Now+" "+message+Environment.NewLine);}catch{}}
  Log("Starting " + string.Join(" ",e.Args));
  if(e.Args.Contains("--native-registration-test")) {try {NativeIntegrationTests.RegistrationOnly();Shutdown(0);}catch(Exception ex){File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"native-test.txt"),ex.ToString());Shutdown(1);}return;}
  if(e.Args.Contains("--native-test")) {try {Config=Settings.Defaults();NativeIntegrationTests.Run();Shutdown(0);}catch(Exception ex){File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"native-test.txt"),ex.ToString());Shutdown(1);}return;}
  if(e.Args.Contains("--self-test")) {try {Config=Settings.Defaults();SelfTest.Run();Shutdown(0);}catch(Exception ex){File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"self-test.txt"),ex.ToString());Shutdown(1);}return;}
  mutex=new Mutex(true,@"Local\Orbit.RadialLauncher",out bool created);
  if(!created) {Log("Already running");MessageBox.Show("Orbit уже работает. Открой настройки через значок в трее.","Orbit");Shutdown();return;}
  try {
   try {Config=Settings.Load();} catch(Exception ex) {Config=Settings.Defaults();MessageBox.Show("Не удалось прочитать настройки. Загружены стандартные.\n"+ex.Message,"Orbit");}
   foreach(var entry in Config.Entries)entry.LoadIcon();
   Log("Installing hotkey");Input=new InputService(Dispatcher);Input.Configure(Config);Log("Creating settings");
   settingsWindow=new MainWindow(this);MainWindow=settingsWindow;
   wheel=new WheelWindow(Config);
   Input.Pressed+=()=>BeginWheel();Input.Released+=cancel=>wheel.Finish(cancel);
   wheel.Chosen+=entry=>{try {settingsWindow.Report(Launcher.Open(entry));}catch(Exception ex){Notify("Не удалось открыть "+entry.Name,ex.Message);}};
   tray=new Forms.NotifyIcon {Icon=System.Drawing.SystemIcons.Application,Text="Orbit · "+Config.BindName,Visible=true};
   var menu=new Forms.ContextMenuStrip();menu.Items.Add("Настройки",null,(_,_)=>ShowSettings());menu.Items.Add("Выход",null,(_,_)=>ExitApp());tray.ContextMenuStrip=menu;tray.DoubleClick+=(_,_)=>ShowSettings();
   if(!e.Args.Contains("--tray"))settingsWindow.Show();Log("Ready");
  } catch(Exception ex) {Log(ex.ToString());MessageBox.Show(ex.Message,"Orbit: ошибка запуска");Shutdown(1);}
  DispatcherUnhandledException+=(_,args)=>{wheel?.Finish(true);Notify("Ошибка Orbit",args.Exception.Message);args.Handled=true;};
  SessionEnding+=(_,_)=>wheel?.Finish(true);
 }
 internal void BeginWheel(bool preview=false)
 {
  try {if(Config.Entries.Count==0) {Notify("Колесо пустое","Добавь программы в настройках.");return;}wheel?.Begin(preview);}
  catch(Exception ex) {wheel?.Finish(true);Notify("Не удалось открыть колесо",ex.Message);}
 }
 private void ShowSettings() {wheel?.Finish(true);settingsWindow?.Show();if(settingsWindow!=null){settingsWindow.WindowState=WindowState.Normal;settingsWindow.Activate();}}
 private void Notify(string title,string text) {settingsWindow?.Report(text);tray?.ShowBalloonTip(4000,title,text,Forms.ToolTipIcon.Info);}
 private void ExitApp() {Exiting=true;wheel?.Finish(true);Shutdown();}
 protected override void OnExit(ExitEventArgs e)
 {
  Exiting=true;wheel?.Finish(true);Input?.Dispose();if(tray!=null){tray.Visible=false;tray.Dispose();}mutex?.Dispose();base.OnExit(e);
 }
}
