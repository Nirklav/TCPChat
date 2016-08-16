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
    private bool disposed;
    protected Dispatcher Dispatcher;
    protected IClientNotifierContext NotifierContext;

    public event PropertyChangedEventHandler PropertyChanged;

    public BaseViewModel(BaseViewModel parent, bool initializeNotifier)
    {
      if (parent != null)
        Dispatcher = parent.Dispatcher;

      if (initializeNotifier)
      {
        NotifierContext = NotifierGenerator.MakeContext<IClientNotifierContext>();
        ClientModel.Notifier.Add(NotifierContext);
      }
    }

    protected virtual void DisposeManagedResources()
    {
      if (NotifierContext != null)
        ClientModel.Notifier.Remove(NotifierContext);
    }

    public void Dispose()
    {
      if (disposed)
        return;
      disposed = true;
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
      // For subscribing on NotifierContext event, it can not unsubscribe.
      // Because notifier context creatred for each viewmodel, and removed with viewmodel.
      return (sender, eventArgs) => Dispatcher.BeginInvoke(method, eventArgs);
    }
  }
}
