using Engine.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using UI.Infrastructure;
using UI.View;
using UI.ViewModel;

namespace UI
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
      protected override void OnStartup(StartupEventArgs e)
      {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        MainWindow window = new MainWindow();
        MainViewModel viewModel = new MainViewModel(window);
        window.DataContext = viewModel;
        window.Show();
      }

      private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
      {
        Exception error = e.ExceptionObject as Exception;

        if (error == null)
          return;

        Logger logger = new Logger(AppDomain.CurrentDomain.BaseDirectory + "/UnhandledError.log");
        logger.Write(error);
      }
    }
}
