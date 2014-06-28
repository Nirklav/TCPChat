using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace UI.ViewModel
{
  public abstract class BaseViewModel : 
    INotifyPropertyChanged,
    IDisposable
  {
    private bool disposed = false;

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string name)
    {
      PropertyChangedEventHandler temp = Interlocked.CompareExchange(ref PropertyChanged, null, null);

      if (temp != null)
        temp(this, new PropertyChangedEventArgs(name));
    }

    public virtual void Dispose()
    {
      if (disposed)
        return;

      disposed = true;
    }
  }
}
