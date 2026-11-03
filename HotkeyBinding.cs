namespace Orbit;
internal static class HotkeyBinding
{
 internal static bool IsMouse(int key)=>key is 4 or 5 or 6;
 internal static bool IsAllowed(int key)=>IsMouse(key)||(key>=8&&key<=254&&!InputService.IsModifier(key));
 internal static bool TryMouseMessage(int message,uint data,out int key,out bool down,out bool up)
 {
  key=0;down=up=false;
  if(message is 0x207 or 0x208){key=4;down=message==0x207;up=!down;return true;}
  if(message is not (0x20B or 0x20C))return false;
  uint button=data>>16;if(button is not (1 or 2))return false;
  key=button==1?5:6;down=message==0x20B;up=!down;return true;
 }
}
