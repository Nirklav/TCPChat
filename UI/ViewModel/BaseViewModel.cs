using Engine.Model.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace UI.ViewModel
{
  public abstract class BaseViewModel : 
    INotifyPropertyChanged,
    IDisposable
  {
    private bool disposed = false;

    public event PropertyChangedEventHandler PropertyChanged;

    protected ClientEventNotifierContext NotifierContext;

    public BaseViewModel(bool initializeNotifier)
    {
      if (initializeNotifier)
        ClientModel.Notifier.Add(NotifierContext = new ClientEventNotifierContext());
    }

    public virtual void Dispose()
    {
      if (disposed)
        return;

      disposed = true;

      if (NotifierContext != null)
        ClientModel.Notifier.Remove(NotifierContext);
    }

    protected virtual void OnPropertyChanged(string name)
    {
      PropertyChangedEventHandler temp = Interlocked.CompareExchange(ref PropertyChanged, null, null);

      if (temp != null)
        temp(this, new PropertyChangedEventArgs(name));
    }

    protected void SetValue<T>(T value, string propertyName, Action<T> setter)
    {
      setter(value);
      OnPropertyChanged(propertyName);
    }
  }
}
