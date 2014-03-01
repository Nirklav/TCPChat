using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace UI.Infrastructure
{
  public class Command : ICommand
  {
    #region fields
    readonly Action<object> execute;
    readonly Func<object, bool> canExecute;
    #endregion

    #region constructors
    public Command(Action<object> execute, Func<object, bool> canExecute = null)
    {
      if (execute == null)
        throw new ArgumentNullException("execute");

      this.execute = execute;
      this.canExecute = canExecute;
    }
    #endregion

    #region ICommand
    public bool CanExecute(object parameter)
    {
      return canExecute == null ? true : canExecute(parameter);
    }

    public event EventHandler CanExecuteChanged
    {
      add { CommandManager.RequerySuggested += value; }
      remove { CommandManager.RequerySuggested -= value; }
    }

    public void Execute(object parameter)
    {
      execute(parameter);
    }
    #endregion
  }
}
