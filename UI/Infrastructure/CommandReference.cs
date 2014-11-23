using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace UI.Infrastructure
{
  public class CommandReference :
      Freezable,
      ICommand
  {
    #region dependencyProperties
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
      "Command",
      typeof(ICommand),
      typeof(CommandReference),
      new PropertyMetadata(OnCommandChanged));

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
      "CommandParameter",
      typeof(object),
      typeof(CommandReference));

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      CommandReference commandReference = d as CommandReference;
      ICommand oldCommand = e.OldValue as ICommand;
      ICommand newCommand = e.NewValue as ICommand;

      if (oldCommand != null)
        oldCommand.CanExecuteChanged -= commandReference.CanExecuteChanged;

      if (newCommand != null)
        newCommand.CanExecuteChanged += commandReference.CanExecuteChanged;
    }
    #endregion

    #region properties
    public Command Command
    {
      get { return (Command)GetValue(CommandProperty); }
      set { SetValue(CommandProperty, value); }
    }

    public object CommandParameter
    {
      get { return GetValue(CommandParameterProperty); }
      set { SetValue(CommandParameterProperty, value); }
    }
    #endregion

    #region ICommand
    public bool CanExecute(object parameter)
    {
      if (Command != null)
        return Command.CanExecute(parameter);

      return false;
    }

    public void Execute(object parameter)
    {
      if (parameter != null)
      {
        Command.Execute(parameter);
        return;
      }

      Command.Execute(CommandParameter);
    }

    public event EventHandler CanExecuteChanged;
    #endregion

    #region Freesable
    protected override Freezable CreateInstanceCore()
    {
      throw new NotImplementedException();
    }
    #endregion
  }
}
