using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Orbit;
public enum IconKind { Aperture,Sun,Moon,Plus,Up,Down,Edit,Trash,Right,App,Close,Minimize,Info }

public sealed class FilledIcon : FrameworkElement
{
 private static readonly Dictionary<IconKind,Geometry> geometries=new();
 public static readonly DependencyProperty KindProperty=DependencyProperty.Register(nameof(Kind),typeof(IconKind),typeof(FilledIcon),new FrameworkPropertyMetadata(IconKind.App,FrameworkPropertyMetadataOptions.AffectsRender));
 public static readonly DependencyProperty ForegroundProperty=TextElement.ForegroundProperty.AddOwner(typeof(FilledIcon),new FrameworkPropertyMetadata(Brushes.Black,FrameworkPropertyMetadataOptions.Inherits|FrameworkPropertyMetadataOptions.AffectsRender));
 public IconKind Kind {get=>(IconKind)GetValue(KindProperty);set=>SetValue(KindProperty,value);}
 public Brush Foreground {get=>(Brush)GetValue(ForegroundProperty);set=>SetValue(ForegroundProperty,value);}
 internal static Geometry GeometryFor(IconKind kind)
 {
  if(!geometries.TryGetValue(kind,out var geometry)) {
   geometry=(Geometry)Application.Current.FindResource("Icon"+kind);if(geometry.CanFreeze)geometry.Freeze();geometries[kind]=geometry;
  }
  return geometry;
 }
 protected override Size MeasureOverride(Size availableSize)=>new(double.IsInfinity(availableSize.Width)?18:Math.Min(18,availableSize.Width),double.IsInfinity(availableSize.Height)?18:Math.Min(18,availableSize.Height));
 protected override void OnRender(DrawingContext dc) {
  double size=Math.Min(ActualWidth,ActualHeight);dc.PushTransform(new TranslateTransform((ActualWidth-size)/2,(ActualHeight-size)/2));dc.PushTransform(new ScaleTransform(size/256,size/256));dc.DrawGeometry(Foreground,null,GeometryFor(Kind));dc.Pop();dc.Pop();
 }
 internal static ImageSource Source(IconKind kind,Color color) {
  var brush=new SolidColorBrush(color);brush.Freeze();var drawing=new GeometryDrawing(brush,null,GeometryFor(kind));drawing.Freeze();var image=new DrawingImage(drawing);image.Freeze();return image;
 }
}
