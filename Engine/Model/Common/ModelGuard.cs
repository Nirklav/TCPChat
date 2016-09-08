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
    private static object _syncObject = new object();
    protected TModel _model;
    #endregion

    #region initialization
    [SecurityCritical]
    protected ModelGuard(TModel initialModel)
    {
      if (!Monitor.TryEnter(_syncObject, TimeOut))
        throw new InvalidOperationException("model lock timeout");

      _model = initialModel;
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      _model = default(TModel);

      Monitor.Exit(_syncObject);
    }
    #endregion
  }
}
