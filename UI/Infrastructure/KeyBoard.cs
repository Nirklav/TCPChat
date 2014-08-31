using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace UI.Infrastructure
{
  public static class KeyBoard
  {
    public static event Action<Keys> KeyUp;
    public static event Action<Keys> KeyDown;

    private static IntPtr hookHandle;
    private static IntPtr moduleHandle;
    private static HookHandler hookCallback;

    public static void SetHook()
    {
      moduleHandle = Marshal.GetHINSTANCE(AppDomain.CurrentDomain.GetAssemblies()[0].GetModules()[0]);
      hookCallback = new HookHandler(HookCallback);

      hookHandle = SetWindowsHookEx(HookType.KerboardLowLevel, hookCallback, moduleHandle, 0);
    }

    public static void UnsetHook()
    {
      UnhookWindowsHookEx(hookHandle);

      hookHandle = IntPtr.Zero;
      moduleHandle = IntPtr.Zero;
    }

    private static IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
      if (code == 0)
      {
        KeyboardDescription description = (KeyboardDescription)Marshal.PtrToStructure(lParam, typeof(KeyboardDescription));

        bool isDown = false;
        if (wParam.ToInt32() == 0x100 || wParam.ToInt32() == 0x104)
          isDown = true;

        Keys keys = (Keys)description.KeyCode;

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

      return CallNextHookEx(hookHandle, code, wParam, lParam);
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
