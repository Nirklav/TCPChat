using Engine.Model.Client;
using Engine.Model.Common;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace UI.ViewModel
{
  public abstract class BaseViewModel : 
    INotifyPropertyChanged,
    IDisposable
  {
    private bool _disposed;
    protected Dispatcher Dispatcher;
    protected IClientEvents Events;

    public event PropertyChangedEventHandler PropertyChanged;

    public BaseViewModel(BaseViewModel parent, bool initializeNotifier)
    {
      if (parent != null)
        Dispatcher = parent.Dispatcher;

      if (initializeNotifier)
      {
        Events = NotifierGenerator.MakeEvents<IClientEvents>();
        ClientModel.Notifier.Add(Events);
      }
    }

    protected virtual void DisposeManagedResources()
    {
      if (Events != null)
        ClientModel.Notifier.Remove(Events);
    }

    public void Dispose()
    {
      if (_disposed)
        return;
      _disposed = true;
      DisposeManagedResources();
    }

    protected virtual void OnPropertyChanged(string name)
    {
      var temp = Interlocked.CompareExchange(ref PropertyChanged, null, null);
      if (temp != null)
        temp(this, new PropertyChangedEventArgs(name));
    }

    protected void SetValue<T>(T value, string propertyName, Action<T> setter)
    {
      setter(value);
      OnPropertyChanged(propertyName);
    }

    protected EventHandler<TArgs> CreateSubscriber<TArgs>(Action<TArgs> method)
      where TArgs : EventArgs
    {
      Action<TArgs> safeMethod = a =>
      {
        try
        {
          method(a);
        }
        catch (Exception e)
        {
          ClientModel.Logger.Write(e);
        }
      };

      // For subscribing on events object event, it can not unsubscribe.
      // Because events object creatred for each viewmodel, and removed with viewmodel.
      return (sender, eventArgs) => Dispatcher.BeginInvoke(safeMethod, eventArgs);
    }
  }
}
