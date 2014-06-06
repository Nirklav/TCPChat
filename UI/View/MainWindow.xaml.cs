using Engine;
using Engine.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Xml.Linq;
using UI.Dialogs;
using UI.Infrastructure;
using UI.ViewModel;

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

      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    #region utilit methods
    [DllImport("user32.dll")]
    private static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

    public void Alert()
    {
      if ((IsActive && !(WindowState == WindowState.Minimized)) || !Settings.Current.Alerts)
        return;

      WindowInteropHelper h = new WindowInteropHelper(this);
      FlashWindow(h.Handle, true);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      Exception error = e.ExceptionObject as Exception;

      if (error == null)
        return;

      Logger logger = new Logger("UnhandledError.log");
      logger.Write(error);
    }
    #endregion
  }
}