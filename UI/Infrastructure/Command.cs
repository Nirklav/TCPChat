using Engine.Model.Client;
using Engine.Model.Server;
using System;
using System.Windows.Input;

namespace UI.Infrastructure
{
  public class Command : ICommand
  {
    #region fields
    private readonly Action<object> _execute;
    private readonly Func<object, bool> _canExecute;
    #endregion

    #region constructors
    public Command(Action<object> execute, Func<object, bool> canExecute = null)
    {
      _execute = execute ?? throw new ArgumentNullException("execute");
      _canExecute = canExecute;
    }
    #endregion

    #region ICommand
    public bool CanExecute(object parameter)
    {
      try
      {
        return _canExecute == null ? true : _canExecute(parameter);
      }
      catch (Exception e)
      {
        LogError(e);
        return true;
      }
    }

    public event EventHandler CanExecuteChanged
    {
      add { CommandManager.RequerySuggested += value; }
      remove { CommandManager.RequerySuggested -= value; }
    }

    public void Execute(object parameter)
    {
      try
      {
        _execute(parameter);
      }
      catch (Exception e)
      {
        LogError(e);
      }
    }

    private void LogError(Exception e)
    {
      if (ClientModel.IsInited)
        ClientModel.Logger.Write(e);
      if (ServerModel.IsInited)
        ServerModel.Logger.Write(e);
    }
    #endregion
  }
}
