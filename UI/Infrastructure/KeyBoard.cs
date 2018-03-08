using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UI.Infrastructure
{
  public static class KeyBoard
  {
    public static event Action<Keys> KeyUp;
    public static event Action<Keys> KeyDown;

    private static IntPtr _hookHandle;
    private static IntPtr _moduleHandle;
    private static HookHandler _hookCallback;

    public static void SetHook()
    {
      _moduleHandle = Marshal.GetHINSTANCE(AppDomain.CurrentDomain.GetAssemblies()[0].GetModules()[0]);
      _hookCallback = HookCallback;

      _hookHandle = SetWindowsHookEx(HookType.KerboardLowLevel, _hookCallback, _moduleHandle, 0);
    }

    public static void UnsetHook()
    {
      UnhookWindowsHookEx(_hookHandle);

      _hookHandle = IntPtr.Zero;
      _moduleHandle = IntPtr.Zero;
    }

    private static IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
      if (code == 0)
      {
        var description = (KeyboardDescription)Marshal.PtrToStructure(lParam, typeof(KeyboardDescription));
        var keys = (Keys)description.KeyCode;
        var isDown = wParam.ToInt32() == 0x100 || wParam.ToInt32() == 0x104;

        if (isDown)
        {
          if (KeyDown != null)
            KeyDown(keys);
        }
        else
        {
          if (KeyUp != null)
            KeyUp(keys);
        }
      }

      return CallNextHookEx(_hookHandle, code, wParam, lParam);
    }

    #region WinApi

    private delegate IntPtr HookHandler(int nCode, IntPtr wParam, [In] IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hookHandle, int code, IntPtr wParam, [In] IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(HookType hookType, HookHandler callback, IntPtr moduleHandle, int threadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hookHandle);

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardDescription
    {
      public uint KeyCode;
      public uint ScanCode;
      public KeyboardFlags Flags;
      public uint Time;
      public IntPtr ExtraInfo;
    }

    [Flags]
    private enum KeyboardFlags
    {
      Extended = 0x01,
      Injected = 0x10,
      AltDown = 0x20,
    }

    private enum HookType
    {
      JournalRecord = 0,
      JournalPlayback = 1,
      Keyboard = 2,
      GetMessage = 3,
      CallWndProc = 4,
      CBT = 5,
      SysMsgFilter = 6,
      Mouse = 7,
      Hardware = 8,
      Debug = 9,
      Shell = 10,
      ForegroundIDLE = 11,
      CallWndProcRet = 12,
      KerboardLowLevel = 13,
      MouseLowLevel = 14
    }

    #endregion
  }
}
