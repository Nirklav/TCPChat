using System;
using System.Security;
using System.Threading;

namespace Engine.Model
{
  public abstract class ModelGuard<TModel> : MarshalByRefObject, IDisposable
    where TModel : class
  {
    #region consts
#if DEBUG
    private const int TimeOut = -1;
#else
    private const int TimeOut = 10000;
#endif
    #endregion

    #region fields
    [ThreadStatic] protected static ModelGuard<TModel> _current;
    protected TModel _model;
    #endregion

    #region initialization
    [SecurityCritical]
    protected ModelGuard(TModel initialModel)
    {
      if (!Monitor.TryEnter(initialModel, TimeOut))
        throw new InvalidOperationException("model lock timeout");

      _model = initialModel;

      if (_current == null)
        _current = this;
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_current == this)
        _current = null;

      _model = default(TModel);

      Monitor.Exit(_model);
    }
    #endregion
  }
}
