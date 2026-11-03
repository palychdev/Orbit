using System;
using System.Collections.Generic;
using System.Linq;

namespace Orbit;

// The input session deliberately has no foreground/focus dependency. Shell menus and
// fullscreen applications may reject activation while still delivering INPUTSINK data.
internal interface IWheelHost
{
 void RegisterInput();
 void ShowOverlay();
 void CapturePointer();
 void ReleasePointer();
 void UnregisterInput();
 void HideOverlay();
}

internal sealed class WheelSession
{
 private readonly Settings settings;
 private readonly IWheelHost host;
 private Entry[] entries=Array.Empty<Entry>();
 private bool preview;
 private double x,y;
 internal bool Active {get;private set;}
 internal int Selected {get;private set;}=-1;

 internal WheelSession(Settings settings,IWheelHost host) {this.settings=settings;this.host=host;}

 internal bool Begin(bool preview)
 {
  if(Active || settings.Entries.Count==0) return false;
  entries=settings.Entries.ToArray();this.preview=preview;x=y=0;Selected=-1;Active=true;
  try {host.RegisterInput();host.ShowOverlay();host.CapturePointer();return true;}
  catch {try {Finish(true);} catch { /* Preserve the original startup error. */ } throw;}
 }

 internal void Move(int dx,int dy)
 {
  if(!Active)return;
  x+=dx*settings.Sensitivity;y+=dy*settings.Sensitivity;
  double distance=Math.Sqrt(x*x+y*y),limit=settings.Size*0.42;
  if(distance>limit) {x*=limit/distance;y*=limit/distance;}
  Selected=Selection.Index(x,y,entries.Length);
 }

 internal Entry? Finish(bool cancel)
 {
  if(!Active)return null;
  Active=false;
  Entry? chosen=!cancel && !preview && Selected>=0?entries[Selected]:null;
  Selected=-1;entries=Array.Empty<Entry>();x=y=0;
  // Every cleanup step runs even if an earlier one fails. No launch after failed cleanup.
  var errors=new List<Exception>();
  foreach(Action cleanup in new Action[]{host.ReleasePointer,host.UnregisterInput,host.HideOverlay})
   try {cleanup();} catch(Exception ex) {errors.Add(ex);}
  if(errors.Count>0)throw new AggregateException("Не удалось полностью закрыть колесо.",errors);
  return chosen;
 }
}
