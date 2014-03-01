using Engine.Concrete.Helpers;
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

        MainWindow window = new MainWindow();
        MainWindowViewModel viewModel = new MainWindowViewModel(window);
        window.DataContext = viewModel;
        window.Show();
      }
    }
}
