using Engine.Plugins;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Engine.Model.Common
{
  [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
  public class NotifierAttribute : Attribute
  {
    public Type Context { get; private set; }

    public Type BaseNotifier { get; set; }

    public NotifierAttribute(Type context)
    {
      Context = context;
    }
  }

  public abstract class Notifier : MarshalByRefObject
  {
    private readonly List<object> contexts = new List<object>();
    public virtual object[] GetContexts()
    {
      lock (contexts)
        return contexts.ToArray();
    }

    public void Add(object context)
    {
      lock (contexts)
        contexts.Add(context);
    }

    public bool Remove(object context)
    {
      lock (contexts)
        return contexts.Remove(context);
    }
  }

  public abstract class NotifierContext : CrossDomainObject
  {
    protected void Invoke<TArgs>(EventHandler<TArgs> handler, object sender, TArgs args)
      where TArgs : EventArgs
    {
      var e = Interlocked.CompareExchange(ref handler, null, null);
      if (e != null)
        e(sender, args);
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
