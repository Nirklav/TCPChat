using Engine.Model.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Engine.Plugins.Client
{
  public class ClientWrapper : CrossDomainObject
  {
    public void SendMessage(ushort id, object messageContent) { ClientModel.Client.SendMessage(id, messageContent); }

    public string Id
    {
      get { return ClientModel.Client.Id; }
      set { ClientModel.Client.Id = value; }
    }

    public IPEndPoint RemotePoint { get { return ClientModel.Client.RemotePoint; } }
    public IPEndPoint LocalPoint { get { return ClientModel.Client.LocalPoint; } }
  }
}
