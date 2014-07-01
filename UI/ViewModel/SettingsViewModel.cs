using System;
using System.Collections.Generic;
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
    #endregion

    #region properties

    #endregion

    #region commands
    public ICommand CloseSettingsCommand { get; private set; }
    #endregion

    #region constructors
    public SettingsViewModel(SettingsView view)
    {
      window = view;
      CloseSettingsCommand = new Command(CloseSettings);
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
