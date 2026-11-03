using System;
using System.Collections.Generic;

namespace Orbit;
[Flags]
internal enum HotkeyResult { None=0,Suppress=1,Open=2,Accept=4,Cancel=8 }

// Runs only on the hook thread (or under InputService's short configuration lock).
internal sealed class HotkeyState
{
 private int key=0x77,modifiers;
 private bool active,ignoreUntilUp;
 private readonly HashSet<int> suppressed=new();
 internal void Configure(int key,int modifiers,bool held) {this.key=key;this.modifiers=modifiers;ignoreUntilUp=held;}
 internal HotkeyResult Handle(int vk,bool down,bool up,int currentModifiers,bool suspended)
 {
  if(suppressed.Contains(vk)) {
   if(up) {
    suppressed.Remove(vk);
    if(vk==key && active) {active=false;return HotkeyResult.Suppress|HotkeyResult.Accept;}
   }
   return HotkeyResult.Suppress;
  }
  if(ignoreUntilUp && vk==key) {if(up)ignoreUntilUp=false;return HotkeyResult.None;}
  if(active && down && vk==27) {active=false;suppressed.Add(vk);return HotkeyResult.Suppress|HotkeyResult.Cancel;}
  int modifierBit=vk is 17 or 162 or 163?2:vk is 18 or 164 or 165?1:vk is 16 or 160 or 161?4:0;
  if(active && up && (modifiers&modifierBit)!=0) {active=false;return HotkeyResult.Accept;}
  // Let Alt+Tab escape the overlay; don't trap application switching behind a held bind.
  if(active && down && vk==9 && (currentModifiers&1)!=0) {active=false;return HotkeyResult.Cancel;}
  if(!suspended && down && vk==key && currentModifiers==modifiers) {
   active=true;suppressed.Add(vk);return HotkeyResult.Suppress|HotkeyResult.Open;
  }
  return HotkeyResult.None;
 }
}
