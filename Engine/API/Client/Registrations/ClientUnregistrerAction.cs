using Engine.Api.Server;
using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.Api.Client.Registrations
{
  [Serializable]
  public class ClientUnregisterAction : IAction
  {
    /// <summary>
    /// Send unregistration request.
    /// </summary>
    [SecuritySafeCritical]
    public ClientUnregisterAction()
    {
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      ClientModel.Client.SendMessage(ServerUnregisterCommand.CommandId);
    }
  }
}
