using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;

namespace Orbit;
internal static class DialogChrome
{
 internal static void Attach(Window window)
 {
  var body=(UIElement)window.Content;window.Content=null;window.WindowStyle=WindowStyle.None;
  WindowChrome.SetWindowChrome(window,new WindowChrome{CaptionHeight=32,ResizeBorderThickness=new Thickness(0),GlassFrameThickness=new Thickness(0),CornerRadius=new CornerRadius(12),UseAeroCaptionButtons=false});
  var root=new Grid();root.SetResourceReference(Panel.BackgroundProperty,"CanvasBrush");root.RowDefinitions.Add(new RowDefinition{Height=new GridLength(32)});root.RowDefinitions.Add(new RowDefinition());
  var caption=new Grid();var title=new TextBlock{Text=window.Title,Margin=new Thickness(16,0,48,0),VerticalAlignment=VerticalAlignment.Center};caption.Children.Add(title);
  var close=new Button{Style=(Style)window.FindResource("IconButton"),Width=36,Height=32,Margin=new Thickness(0,0,8,0),HorizontalAlignment=HorizontalAlignment.Right,Content=new FilledIcon{Kind=IconKind.Close,Width=11,Height=11},ToolTip="Закрыть"};
  System.Windows.Automation.AutomationProperties.SetName(close,"Закрыть");WindowChrome.SetIsHitTestVisibleInChrome(close,true);close.Click+=(_,_)=>window.Close();caption.Children.Add(close);root.Children.Add(caption);Grid.SetRow(body,1);root.Children.Add(body);window.Content=root;
  window.SourceInitialized+=(_,_)=>ThemeManager.ApplyTitleBar(window,((App)Application.Current).Config.Theme);
 }
}
