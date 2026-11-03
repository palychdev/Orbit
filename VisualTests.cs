using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

namespace Orbit;
internal static class VisualTests
{
 internal static int Run()
 {
  int checks=0;
  void Check(bool result,string name) {checks++;if(!result)throw new Exception("FAIL: "+name);}
  var app=(App)Application.Current;var original=app.Config.Entries;string theme=app.Config.Theme;
  try {
   foreach(string appearance in new[]{"Dark","Light"}) {
    app.Config.Theme=appearance;ThemeManager.Apply(app,appearance);
    var palette=ThemePalette.For(appearance);
    Check(Contrast(palette.Text,palette.Canvas)>=4.5,"main text contrast "+appearance);
    Check(Contrast(palette.Secondary,palette.Surface)>=4.5,"secondary text contrast "+appearance);
    Check(Contrast(palette.WheelSelectedText,palette.WheelSelected)>=4.5,"selected wheel contrast "+appearance);
    Check(Contrast(palette.Accent,palette.Surface)>=3,"keyboard focus contrast "+appearance);
    Check(Contrast(palette.Danger,palette.Canvas)>=4.5,"validation text contrast "+appearance);
    app.Config.Entries=Settings.Defaults().Entries;foreach(var iconEntry in app.Config.Entries)iconEntry.LoadIcon();
    var window=new MainWindow(app);
    new WindowInteropHelper(window).EnsureHandle();
    Render((FrameworkElement)window.Content,720,480,$"ui-{appearance.ToLowerInvariant()}-apps.png");
    Check(window.AppsList.Items.Count==3,"list contains real applications");
    Check(window.EmptyState.Visibility==Visibility.Collapsed,"nonempty list state");
    var scroll=Descendant<ScrollViewer>(window.AppsList)!;
    Check(scroll.ExtentHeight<=scroll.ViewportHeight,$"short list fits: extent {scroll.ExtentHeight}, viewport {scroll.ViewportHeight}, height {window.AppsList.ActualHeight}");
    Check(app.Config.Entries.FindAll(entry=>File.Exists(entry.Path)).TrueForAll(entry=>entry.Icon is {IsFrozen:true}),"Windows shell supplies frozen real icons for installed applications");
    Render((FrameworkElement)window.Content,720,480,$"ui-{appearance.ToLowerInvariant()}-apps-125.png",1.25);
    window.SettingsTab.IsChecked=true;
    Render((FrameworkElement)window.Content,720,480,$"ui-{appearance.ToLowerInvariant()}-settings.png");
    Check(window.SettingsPage.Visibility==Visibility.Visible&&window.AppsPage.Visibility==Visibility.Collapsed,"settings navigation");
    Check(window.SizeValue.Text.Contains("440"),"size includes value");
    window.AppsTab.IsChecked=true;
    Render((FrameworkElement)window.Content,720,480,$"ui-{appearance.ToLowerInvariant()}-compact.png");
    Check(window.FooterBind.ActualWidth>0,"binding control measured");
    var wheel=new WheelVisual(app.Config){Selected=1};Render(wheel,900,700,$"wheel-{appearance.ToLowerInvariant()}.png");
    app.Config.Entries=new List<Entry>();
    var empty=new MainWindow(app);Render((FrameworkElement)empty.Content,720,480,$"ui-{appearance.ToLowerInvariant()}-empty.png");
    Check(empty.EmptyState.Visibility==Visibility.Visible&&empty.ListSurface.Visibility==Visibility.Collapsed,"empty state replaces list");
    app.Config.Entries=new List<Entry>();
    for(int i=0;i<12;i++)app.Config.Entries.Add(new Entry{Name=i==0?"Очень длинное название приложения с несколькими словами для проверки обрезки":$"Приложение {i+1}",Path=@"C:\Program Files\Example\Application.exe"});
    var dense=new MainWindow(app);Render((FrameworkElement)dense.Content,720,480,$"ui-{appearance.ToLowerInvariant()}-dense.png");
    Check(!dense.AddButton.IsEnabled&&dense.AppsList.Items.Count==12,"capacity and add control");
    Check(dense.AppsList.ActualHeight<=dense.ListArea.ActualHeight+1,"list bounded at minimum window height");
    Check(Descendant<ScrollViewer>(dense.AppsList) is {ScrollableHeight:>0},"dense list remains scrollable");
    Check(dense.DownButton.TransformToAncestor(dense.RootCanvas).Transform(new Point()).Y+dense.DownButton.ActualHeight<dense.FooterBind.TransformToAncestor(dense.RootCanvas).Transform(new Point()).Y,"list actions stay above footer");
    Render(new WheelVisual(app.Config){Selected=10},900,700,$"wheel-{appearance.ToLowerInvariant()}-dense.png");
    var entry=Settings.Defaults().Entries[0];string originalName=entry.Name;
    var editor=new EntryEditor(entry);editor.NameBox.Text="";
    Check(!editor.TryApply()&&entry.Name==originalName,"invalid edit does not mutate entry");
    Render((FrameworkElement)editor.Content,520,440,$"ui-{appearance.ToLowerInvariant()}-validation.png");
    editor.NameBox.Text="Мой блокнот";editor.PathBox.Text=entry.Path;
    Check(editor.TryApply()&&entry.Name=="Мой блокнот","valid edit commits fields");
   }
   string json=System.Text.Json.JsonSerializer.Serialize(new Settings{Theme="Light"});
   Check(System.Text.Json.JsonSerializer.Deserialize<Settings>(json)!.Theme=="Light","appearance roundtrip");
   Check(new Settings().Theme=="Dark","dark default for existing settings");
   Check(System.Text.Json.JsonSerializer.Deserialize<Settings>("{\"Key\":119}")!.Theme=="Dark","old settings without theme remain compatible");
  } finally {app.Config.Entries=original;app.Config.Theme=theme;ThemeManager.Apply(app,theme);}
  return checks;
 }
 private static T? Descendant<T>(DependencyObject parent) where T:DependencyObject
 {
  for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T match)return match;var nested=Descendant<T>(child);if(nested!=null)return nested;}return null;
 }
 private static void Render(FrameworkElement element,int width,int height,string name,double scale=1)
 {
  element.Measure(new Size(width,height));element.Arrange(new Rect(0,0,width,height));element.UpdateLayout();
  var bitmap=new RenderTargetBitmap((int)(width*scale),(int)(height*scale),96*scale,96*scale,PixelFormats.Pbgra32);bitmap.Render(element);
  var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var output=File.Create(Path.Combine(AppContext.BaseDirectory,name));png.Save(output);
 }
 private static double Contrast(Brush foreground,Brush background)
 {
  static double L(Brush brush) {var c=((SolidColorBrush)brush).Color;static double V(byte b){double x=b/255.0;return x<=0.04045?x/12.92:Math.Pow((x+0.055)/1.055,2.4);}return .2126*V(c.R)+.7152*V(c.G)+.0722*V(c.B);}
  double a=L(foreground),b=L(background);return (Math.Max(a,b)+.05)/(Math.Min(a,b)+.05);
 }
}
