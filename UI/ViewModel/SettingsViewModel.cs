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
    private string selectedTab;
    #endregion

    #region properties
    public string[] SettingItems { get; private set; }

    public string SelectedTab
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

      SettingItems = new[] { "Основные", "Звук", "Пользовательские" };
    }

    public override void Dispose()
    {
      base.Dispose();


    }
    #endregion

    #region methods
    private void CloseSettings(object obj)
    {
      window.Close();
    }
    #endregion
  }
}
