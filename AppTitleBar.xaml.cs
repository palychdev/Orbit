using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Orbit;
public partial class AppTitleBar : UserControl
{
 public event RoutedEventHandler? ThemeRequested;
 public AppTitleBar(){InitializeComponent();}
 internal void SetTheme(bool light){ThemeGlyph.Kind=light?IconKind.Moon:IconKind.Sun;ThemeButton.ToolTip=light?"Тёмная тема":"Светлая тема";AutomationProperties.SetName(ThemeButton,(string)ThemeButton.ToolTip);}
 private void Theme_Click(object sender,RoutedEventArgs e)=>ThemeRequested?.Invoke(this,e);
 private void Minimize_Click(object sender,RoutedEventArgs e){if(Window.GetWindow(this) is Window window)window.WindowState=WindowState.Minimized;}
 private void Close_Click(object sender,RoutedEventArgs e)=>Window.GetWindow(this)?.Close();
 private void About_Click(object sender,RoutedEventArgs e)
 {
  var owner=Window.GetWindow(this);var panel=new StackPanel{Margin=new Thickness(24)};panel.Children.Add(new TextBlock{Text="Orbit",FontSize=24,FontWeight=FontWeights.SemiBold});panel.Children.Add(new TextBlock{Text=typeof(App).Assembly.GetName().Version!.ToString(3),Margin=new Thickness(0,4,0,20)});
  var credit=new TextBlock();credit.Inlines.Add(new Run("Разработчик · "));var link=new Hyperlink(new Run("palychdev")){NavigateUri=new Uri("https://github.com/palychdev")};link.SetResourceReference(Hyperlink.ForegroundProperty,"AccentBrush");link.RequestNavigate+=(_,args)=>{Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri){UseShellExecute=true});args.Handled=true;};credit.Inlines.Add(link);panel.Children.Add(credit);
  var dialog=new Window{Title="Об Orbit",Width=320,Height=190,ResizeMode=ResizeMode.NoResize,Owner=owner,WindowStartupLocation=WindowStartupLocation.CenterOwner,Content=panel,Style=(Style)FindResource(typeof(Window)),ShowInTaskbar=false};DialogChrome.Attach(dialog);dialog.ShowDialog();
 }
}
