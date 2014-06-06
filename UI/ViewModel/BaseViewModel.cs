using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace UI.ViewModel
{
  public class BaseViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string name)
    {
      PropertyChangedEventHandler temp = Interlocked.CompareExchange(ref PropertyChanged, null, null);

      if (temp != null)
        temp(this, new PropertyChangedEventArgs(name));
    }
  }
}
