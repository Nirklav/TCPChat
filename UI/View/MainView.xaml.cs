using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using UI.Infrastructure;

namespace UI.View
{
  /// <summary>
  /// Логика взаимодействия для MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();

      SetHook();    
    }

    protected override void OnClosed(EventArgs e)
    {
      UnsetHook();

      base.OnClosed(e);
    }

    #region alert
    [DllImport("user32.dll")]
    private static extern bool FlashWindow(IntPtr handle, bool invert);

    public void Alert()
    {
      if ((IsActive && !(WindowState == WindowState.Minimized)) || !Settings.Current.Alerts)
        return;

      WindowInteropHelper intertopHelper = new WindowInteropHelper(this);
      FlashWindow(intertopHelper.Handle, true);
    }

    #endregion

    #region hot key

    public new event Action<Keys> KeyUp;
    public new event Action<Keys> KeyDown;

    private IntPtr hookHandle;
    private IntPtr moduleHandle;
    private HookHandler hookCallback;

    private void SetHook()
    {
      moduleHandle = Marshal.GetHINSTANCE(AppDomain.CurrentDomain.GetAssemblies()[0].GetModules()[0]);
      hookCallback = new HookHandler(HookCallback);

      hookHandle = SetWindowsHookEx(HookType.KerboardLowLevel, hookCallback, moduleHandle, 0);
    }

    private void UnsetHook()
    {
      UnhookWindowsHookEx(hookHandle);

      hookHandle = IntPtr.Zero;
      moduleHandle = IntPtr.Zero;
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
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

    public delegate IntPtr HookHandler(int nCode, IntPtr wParam, [In] IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hookHandle, int code, IntPtr wParam, [In] IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowsHookEx(HookType hookType, HookHandler callback, IntPtr moduleHandle, int threadId);

    [DllImport("user32.dll")]
    public static extern bool UnhookWindowsHookEx(IntPtr hookHandle);

    [StructLayout(LayoutKind.Sequential)]
    struct KeyboardDescription
    {
      public uint KeyCode;
      public uint ScanCode;
      public KeyboardFlags Flags;
      public uint Time;
      public IntPtr ExtraInfo;
    }

    [Flags]
    enum KeyboardFlags : int
    {
      Extended = 0x01,
      Injected = 0x10,
      AltDown = 0x20,
    }

    public enum HookType : int
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

    #endregion
  }
}