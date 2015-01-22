using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Engine.Plugins
{
  public abstract class Plugin<TModel> : CrossDomainObject
    where TModel : CrossDomainObject
  {
    public static TModel Model { get; private set; }

    private Thread processThread;

    public virtual CrossDomainObject NotifierContext { get { return null; } }

    public void Initialize(TModel model)
    {
      Model = model;
      processThread = new Thread(ProcessThreadHandler);
      processThread.IsBackground = true;
      processThread.Start();

      Initialize();
    }

    private void ProcessThreadHandler()
    {
      while (true)
      {
        Thread.Sleep(TimeSpan.FromMinutes(1));

        Model.Process();
        OnProcess();
      }
    }

    public abstract string Name { get; }
    protected abstract void Initialize();
    protected virtual void OnProcess() { }
  }
}
