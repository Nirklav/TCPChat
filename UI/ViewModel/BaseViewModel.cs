using Engine.Model.Client;
using Engine.Model.Common;
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

    protected IClientNotifierContext NotifierContext;

    public BaseViewModel(bool initializeNotifier)
    {
      if (initializeNotifier)
      {
        NotifierContext = NotifierGenerator.MakeContext<IClientNotifierContext>();
        var notifier = (Notifier)ClientModel.Notifier;
        notifier.Add(NotifierContext);
      }
    }

    public virtual void Dispose()
    {
      if (disposed)
        return;

      disposed = true;

      if (NotifierContext != null)
      {
        var notifier = (Notifier)ClientModel.Notifier;
        notifier.Remove(NotifierContext);
      }
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
