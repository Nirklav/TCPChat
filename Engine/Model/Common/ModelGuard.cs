using System;
using System.Security;
using System.Threading;

namespace Engine.Model
{
  public abstract class ModelGuard<TModel> :
    MarshalByRefObject,
    IDisposable
  {
    #region consts
#if DEBUG
    private const int TimeOut = -1;
#else
    private const int TimeOut = 10000;
#endif
    #endregion

    #region fields
    private static object syncObject = new object();
    protected TModel model;
    #endregion

    #region initialization
    [SecurityCritical]
    protected ModelGuard(TModel initialModel)
    {
      if (!Monitor.TryEnter(syncObject, TimeOut))
        throw new InvalidOperationException("model lock timeout");

      model = initialModel;
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      model = default(TModel);

      Monitor.Exit(syncObject);
    }
    #endregion
  }
}
