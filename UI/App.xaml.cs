using Engine.Helpers;
using System;
using System.Windows;
using UI.View;
using UI.ViewModel;

namespace UI
{
  public partial class App : Application
  {
    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

      var window = new MainWindow();
      var viewModel = new MainViewModel(window);
      window.DataContext = viewModel;
      window.Show();
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      var error = e.ExceptionObject as Exception;
      if (error == null)
        return;

      var logger = new Logger(AppDomain.CurrentDomain.BaseDirectory + "/UnhandledError.log");
      logger.Write(error);
    }
  }
}
