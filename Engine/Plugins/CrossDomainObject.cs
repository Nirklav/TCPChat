using System;
using System.Security;

namespace Engine.Plugins
{
  public abstract class CrossDomainObject : MarshalByRefObject
  {
    [SecurityCritical]
    public sealed override object InitializeLifetimeService()
    {
      return null;
    }
  }
}
