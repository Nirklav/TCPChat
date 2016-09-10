using Engine.Model.Client;
using System;
using System.Net;
using System.Security;

namespace Engine.Api.Client
{
  [SecurityCritical]
  class ClientConnectToP2PServiceCommand :
    ClientCommand<ClientConnectToP2PServiceCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.ConnectToP2PService;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      var address = ClientModel.Client.RemotePoint.Address;
      var endPoint = new IPEndPoint(address, content.Port);

      ClientModel.Peer.ConnectToService(endPoint);
    }

    [Serializable]
    public class MessageContent
    {
      private int _port;

      public int Port
      {
        get { return _port; }
        set { _port = value; }
      }
    }
  }
}
