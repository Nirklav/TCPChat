using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Plugins.Client
{
  public class ClientModelWrapper : 
    CrossDomainObject,
    IPluginModelWrapper
  {
    public ClientAPIWrapper API { get; private set; }
    public ClientWrapper Client { get; private set; }

    public ClientModelWrapper()
    {
      API = new ClientAPIWrapper();
      Client = new ClientWrapper();
    }
  }
}
