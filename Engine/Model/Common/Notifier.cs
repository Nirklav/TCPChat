using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Engine.Model.Common
{
  public abstract class Notifier<TNotifierContext> : MarshalByRefObject
  {
    private List<TNotifierContext> contexts;

    public Notifier()
    {
      contexts = new List<TNotifierContext>();
    }

    public void Add(TNotifierContext context)
    {
      contexts.Add(context);
    }

    public bool Remove(TNotifierContext context)
    {
      return contexts.Remove(context);
    }

    protected virtual void Notify<TArgs>(Action<TNotifierContext, TArgs> methodInvoker, TArgs args) where TArgs : EventArgs
    {
      foreach (var context in contexts)
        methodInvoker(context, args);
    }
  }
}
