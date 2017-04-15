using Engine.Helpers;
using Engine.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Engine.Model.Common
{
  [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
  public class NotifierAttribute : Attribute
  {
    public Type Events { get; private set; }
    public Type BaseNotifier { get; set; }

    public NotifierAttribute(Type events)
    {
      Events = events;
    }
  }

  public interface INotifier
  {
    void Add(object events);
    bool Remove(object events);
  }

  public abstract class Notifier : 
    CrossDomainObject, 
    INotifier
  {
    private readonly object _syncObject = new object();
    private List<object> _events = new List<object>();

    public virtual IEnumerable<object> GetEvents()
    {
      lock (_syncObject)
        return _events;
    }

    public void Add(object events)
    {
      lock (_syncObject)
      {
        _events = new List<object>(_events); // Creates new, old value returned outside
        _events.Add(events);
      }
    }

    public bool Remove(object events)
    {
      lock (_syncObject)
      {
        _events = new List<object>(_events); // Creates new, old value returned outside
        return _events.Remove(events);
      }
    }
  }

  public abstract class NotifierEvents : CrossDomainObject
  {
    protected void Invoke<TArgs>(EventHandler<TArgs> handler, object sender, TArgs args, Action<Exception> callback)
      where TArgs : EventArgs
    {
      if (handler != null)
        handler.BeginDispatch(sender, args, callback);
    }

    protected void Add<TArgs>(ref EventHandler<TArgs> value, EventHandler<TArgs> added)
      where TArgs : EventArgs
    {
      while (true)
      {
        var startValue = value;
        var result = (EventHandler<TArgs>)Delegate.Combine(startValue, added);
        var endValue = Interlocked.CompareExchange(ref value, result, startValue);
        
        if (ReferenceEquals(startValue, endValue))
          break;
      }
    }

    protected void Remove<TArgs>(ref EventHandler<TArgs> value, EventHandler<TArgs> removed)
      where TArgs : EventArgs
    {
      while (true)
      {
        var startValue = value;
        var result = (EventHandler<TArgs>)Delegate.Remove(startValue, removed);
        var endValue = Interlocked.CompareExchange(ref value, result, startValue);

        if (ReferenceEquals(startValue, endValue))
          break;
      }
    }
  }
}
