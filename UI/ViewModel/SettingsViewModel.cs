using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using UI.Infrastructure;
using UI.View;

namespace UI.ViewModel
{
  public class SettingsViewModel : BaseViewModel
  {
    #region fields
    private SettingsView window;
    private SettingsTabViewModel selectedTab;
    #endregion

    #region properties
    public SettingsTabViewModel[] Tabs { get; private set; }

    public SettingsTabViewModel SelectedTab
    {
      get { return selectedTab; }
      set
      {
        selectedTab = value;
        OnPropertyChanged("SelectedTab");
      }
    }
    #endregion

    #region commands
    public ICommand CloseSettingsCommand { get; private set; }
    #endregion

    #region constructors
    public SettingsViewModel(SettingsView view)
    {
      window = view;
      CloseSettingsCommand = new Command(CloseSettings);

      Tabs = new[] 
      {
        new SettingsTabViewModel("Основные"),
        new AudioTabViewModel("Звук"),
        new SettingsTabViewModel("Пользовательские"),
      };
    }
    #endregion

    #region methods
    private void CloseSettings(object obj)
    {
      foreach (var tab in Tabs)
      {
        tab.SaveSettings();
        tab.Dispose();
      }

      window.Close();
    }
    #endregion
  }
}
